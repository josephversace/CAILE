using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using IIM.Shared.Interfaces;
using IIM.Infrastructure.AI.Execution;

namespace IIM.Infrastructure.AI.OnnxRuntime
{
	/// <summary>
	/// Unified ONNX Runtime manager.
	/// Delegates hardware provider selection to IOnnxExecutionProvider.
	/// </summary>
	public class OnnxRuntimeManager : IOnnxRuntimeManager, IDisposable
	{
		private readonly ILogger<OnnxRuntimeManager> _logger;
		private readonly IOnnxExecutionProvider _provider;

		private readonly Dictionary<string, InferenceSession> _sessionCache = new();
		private readonly SemaphoreSlim _sessionLock = new(1, 1);

		private bool _disposed;

		public OnnxRuntimeManager(
			ILogger<OnnxRuntimeManager> logger,
			IOnnxExecutionProvider provider)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		/// <summary>
		/// Creates or retrieves a cached ONNX inference session using the currently selected provider.
		/// </summary>
		public async Task<InferenceSession> CreateSessionAsync(string modelPath)
		{
			if (!File.Exists(modelPath))
				throw new FileNotFoundException($"Model file not found: {modelPath}");

			var cacheKey = $"{modelPath}::{_provider.Name}";

			await _sessionLock.WaitAsync();
			try
			{
				if (_sessionCache.TryGetValue(cacheKey, out var cached))
				{
					_logger.LogDebug("Using cached ONNX session for {Model} ({Provider})", modelPath, _provider.Name);
					return cached;
				}

				_logger.LogInformation("Creating ONNX session for {Model} using provider {Provider}", modelPath, _provider.Name);

				var options = _provider.Configure(new SessionOptions());
				var session = new InferenceSession(modelPath, options);

				_sessionCache[cacheKey] = session;

				_logger.LogInformation("Session created successfully for {Model}", modelPath);
				return session;
			}
			finally
			{
				_sessionLock.Release();
			}
		}

		/// <summary>
		/// Executes inference.
		/// </summary>
		public async Task<IDisposableReadOnlyCollection<DisposableNamedOnnxValue>> RunAsync(
			InferenceSession session,
			IEnumerable<NamedOnnxValue> inputs,
			CancellationToken cancellationToken = default)
		{
			return await Task.Run(() =>
			{
				try
				{
					var inputList = inputs.ToList();
					_logger.LogDebug("Running inference with {Count} inputs", inputList.Count);

					var results = session.Run(inputList);
					return results;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Inference execution failed");
					throw;
				}
			}, cancellationToken);
		}

		public IReadOnlyDictionary<string, NodeMetadata> GetInputMetadata(InferenceSession session)
			=> session.InputMetadata;

		public IReadOnlyDictionary<string, NodeMetadata> GetOutputMetadata(InferenceSession session)
			=> session.OutputMetadata;

		public NamedOnnxValue CreateTensor<T>(string name, T[] data, int[] dims) where T : unmanaged
		{
			var tensor = new DenseTensor<T>(data, dims);
			return NamedOnnxValue.CreateFromTensor(name, tensor);
		}

		// =====================================================================
		// Preprocessing
		// =====================================================================

		public async Task<List<NamedOnnxValue>> PreprocessInputAsync(
			InferenceSession session, object rawInput, ModelType type)
		{
			return await Task.Run(() =>
			{
				return type switch
				{
					ModelType.LLM => PreprocessTextInput(session, rawInput as string),
					ModelType.Whisper => PreprocessAudioInput(session, rawInput),
					ModelType.CLIP => PreprocessImageInput(session, rawInput),
					ModelType.Embedding => PreprocessEmbeddingInput(session, rawInput as string),

					_ => throw new NotSupportedException($"Model type {type} not supported.")
				};
			});
		}

		// TEXT
		private List<NamedOnnxValue> PreprocessTextInput(InferenceSession session, string? text)
		{
			text ??= string.Empty;

			var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select((w, i) => (long)(i + 1))
				.ToArray();

			const int maxLength = 512;

			var padded = new long[maxLength];
			Array.Copy(tokens, padded, Math.Min(tokens.Length, maxLength));

			var mask = padded.Select(t => t > 0 ? 1L : 0L).ToArray();

			return new List<NamedOnnxValue>
			{
				CreateTensor("input_ids", padded, new[] { 1, maxLength }),
				CreateTensor("attention_mask", mask, new[] { 1, maxLength })
			};
		}

		// AUDIO
		private List<NamedOnnxValue> PreprocessAudioInput(InferenceSession session, object raw)
		{
			var mel = new float[80 * 3000];
			return new List<NamedOnnxValue>
			{
				CreateTensor("mel", mel, new[] { 1, 80, 3000 })
			};
		}

		// IMAGE
		private List<NamedOnnxValue> PreprocessImageInput(InferenceSession session, object raw)
		{
			const int size = 224;
			const int channels = 3;

			var data = new float[size * size * channels];

			for (int i = 0; i < data.Length; i++)
				data[i] = (data[i] / 255f - 0.5f) * 2f;

			return new List<NamedOnnxValue>
			{
				CreateTensor("pixel_values", data, new[] { 1, channels, size, size })
			};
		}

		// EMBEDDINGS
		private List<NamedOnnxValue> PreprocessEmbeddingInput(InferenceSession session, string? text)
		{
			text ??= string.Empty;

			var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select((w, i) => (long)(i + 1))
				.Take(512)
				.ToArray();

			var padded = new long[512];
			Array.Copy(tokens, padded, tokens.Length);

			return new List<NamedOnnxValue>
			{
				CreateTensor("input_ids", padded, new[] { 1, 512 })
			};
		}

		// =====================================================================
		// Postprocessing
		// =====================================================================

		public async Task<object> PostprocessOutputAsync(
			IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs,
			ModelType type)
		{
			return await Task.Run(() =>
			{
				return type switch
				{
					ModelType.LLM => PostprocessTextOutput(outputs),
					ModelType.Whisper => PostprocessAudioOutput(outputs),
					ModelType.CLIP => PostprocessImageOutput(outputs),
					ModelType.Embedding => PostprocessEmbeddingOutput(outputs),

					_ => outputs.ToDictionary(o => o.Name, o => o.Value)
				};
			});
		}

		private object PostprocessTextOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
		{
			var logits = outputs.FirstOrDefault(o => o.Name == "logits");
			if (logits == null)
				return "No output";

			var tensor = logits.AsTensor<float>();
			var dims = tensor.Dimensions.ToArray();

			var preds = new List<int>();

			for (int t = 0; t < dims[1]; t++)
			{
				float max = float.MinValue;
				int maxIdx = 0;

				for (int j = 0; j < dims[2]; j++)
				{
					var v = tensor[0, t, j];
					if (v > max)
					{
						max = v;
						maxIdx = j;
					}
				}
				preds.Add(maxIdx);
			}

			return string.Join(" ", preds.Select(p => $"token_{p}"));
		}

		private object PostprocessAudioOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
		{
			return "Transcribed audio text";
		}

		private object PostprocessImageOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
		{
			var embed = outputs.FirstOrDefault();
			if (embed == null)
				return "No embeddings";

			var tensor = embed.AsTensor<float>();
			return new { Data = tensor.ToArray(), Shape = tensor.Dimensions.ToArray() };
		}

		private object PostprocessEmbeddingOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
		{
			var embed = outputs.FirstOrDefault();
			if (embed == null)
				return Array.Empty<float>();

			return embed.AsTensor<float>().ToArray();
		}

		// =====================================================================
		// Cleanup
		// =====================================================================

		public void Dispose()
		{
			if (_disposed) return;

			_sessionLock.Wait();
			try
			{
				foreach (var s in _sessionCache.Values)
					s?.Dispose();

				_sessionCache.Clear();
			}
			finally
			{
				_sessionLock.Release();
				_sessionLock.Dispose();
			}

			_disposed = true;
		}

		public Task<InferenceSession> CreateSessionAsync(string modelPath, ExecutionProvider provider)
		{
			// Old API is deprecated — ignore the enum and use the current provider.
			return CreateSessionAsync(modelPath);
		}

	}
}
