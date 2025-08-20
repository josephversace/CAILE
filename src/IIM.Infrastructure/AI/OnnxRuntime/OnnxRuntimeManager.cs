// ============================================
// File: src/IIM.Infrastructure/AI/OnnxRuntime/OnnxRuntimeManager.cs
// Purpose: ONNX Runtime session management with DirectML support
// Author: IIM Platform Team
// Created: 2024
// ============================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Enums;
using IIM.Infrastructure.AI.DirectML;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace IIM.Infrastructure.AI.OnnxRuntime
{
    /// <summary>
    /// Contract for managing ONNX Runtime inference sessions and execution.
    /// </summary>
    public interface IOnnxRuntimeManager : IDisposable
    {
        /// <summary>
        /// Creates or retrieves a cached ONNX inference session for the specified model and execution provider.
        /// </summary>
        Task<InferenceSession> CreateSessionAsync(string modelPath, ExecutionProvider provider);

        /// <summary>
        /// Runs inference on the given session using provided named inputs.
        /// </summary>
        Task<IDisposableReadOnlyCollection<DisposableNamedOnnxValue>> RunAsync(
            InferenceSession session,
            IEnumerable<NamedOnnxValue> inputs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets input metadata for the given session.
        /// </summary>
        IReadOnlyDictionary<string, NodeMetadata> GetInputMetadata(InferenceSession session);

        /// <summary>
        /// Gets output metadata for the given session.
        /// </summary>
        IReadOnlyDictionary<string, NodeMetadata> GetOutputMetadata(InferenceSession session);

        /// <summary>
        /// Utility to create a NamedOnnxValue from a raw data tensor.
        /// </summary>
        NamedOnnxValue CreateTensor<T>(string name, T[] data, int[] dimensions) where T : unmanaged;

        /// <summary>
        /// Preprocesses raw input data into model-ready NamedOnnxValues according to model type.
        /// </summary>
        Task<List<NamedOnnxValue>> PreprocessInputAsync(
            InferenceSession session,
            object rawInput,
            ModelType modelType);

        /// <summary>
        /// Converts model output to user-facing structure according to model type.
        /// </summary>
        Task<object> PostprocessOutputAsync(
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs,
            ModelType modelType);
    }

    /// <summary>
    /// Available ONNX execution providers.
    /// </summary>
    public enum ExecutionProvider
    {
        CPU,
        DirectML,
        CUDA,
        ROCm
    }

    /// <summary>
    /// Concrete ONNX Runtime manager.
    /// </summary>
    public class OnnxRuntimeManager : IOnnxRuntimeManager
    {
        private readonly ILogger<OnnxRuntimeManager> _logger;
        private readonly IDirectMLDeviceManager _deviceManager;
        private readonly Dictionary<string, InferenceSession> _sessionCache = new();
        private readonly SemaphoreSlim _sessionLock = new(1, 1);
        private bool _disposed;

        /// <summary>
        /// Constructs the manager with required dependencies.
        /// </summary>
        public OnnxRuntimeManager(
            ILogger<OnnxRuntimeManager> logger,
            IDirectMLDeviceManager deviceManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        }

        /// <summary>
        /// Creates or retrieves a cached ONNX inference session for the specified model and provider.
        /// </summary>
        public async Task<InferenceSession> CreateSessionAsync(string modelPath, ExecutionProvider provider)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model file not found: {modelPath}");

            var cacheKey = $"{modelPath}_{provider}";
            await _sessionLock.WaitAsync();
            try
            {
                if (_sessionCache.TryGetValue(cacheKey, out var cachedSession))
                {
                    _logger.LogDebug("Using cached session for {Model}", modelPath);
                    return cachedSession;
                }

                _logger.LogInformation("Creating inference session for {Model} with {Provider}", modelPath, provider);

                var sessionOptions = await CreateSessionOptionsAsync(provider);
                var session = new InferenceSession(modelPath, sessionOptions);

                _sessionCache[cacheKey] = session;

                _logger.LogInformation("Successfully created session for {Model}", modelPath);
                return session;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        /// <summary>
        /// Executes inference on the given session using provided inputs.
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
                    var inputList = inputs.ToList(); // ← Ensure IReadOnlyCollection
                    _logger.LogDebug("Running inference with {InputCount} inputs", inputList.Count);
                    var results = session.Run(inputList); // ← Pass the List
                    _logger.LogDebug("Inference completed successfully");
                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Inference failed");
                    throw;
                }
            }, cancellationToken);
        }


        /// <summary>
        /// Returns the input metadata for the session (name, types, shape, etc).
        /// </summary>
        public IReadOnlyDictionary<string, NodeMetadata> GetInputMetadata(InferenceSession session)
            => session.InputMetadata;

        /// <summary>
        /// Returns the output metadata for the session (name, types, shape, etc).
        /// </summary>
        public IReadOnlyDictionary<string, NodeMetadata> GetOutputMetadata(InferenceSession session)
            => session.OutputMetadata;

        /// <summary>
        /// Helper to create a NamedOnnxValue (tensor input) for a model.
        /// </summary>
        public NamedOnnxValue CreateTensor<T>(string name, T[] data, int[] dimensions) where T : unmanaged
        {
            var tensor = new DenseTensor<T>(data, dimensions);
            return NamedOnnxValue.CreateFromTensor(name, tensor);
        }

        /// <summary>
        /// Converts raw input (e.g., string, image bytes) into a list of NamedOnnxValue, depending on model type.
        /// </summary>
        public async Task<List<NamedOnnxValue>> PreprocessInputAsync(
            InferenceSession session,
            object rawInput,
            ModelType modelType)
        {
            return await Task.Run(() =>
            {
                return modelType switch
                {
                    ModelType.LLM => PreprocessTextInput(session, rawInput as string),
                    ModelType.Whisper => PreprocessAudioInput(session, rawInput),
                    ModelType.CLIP => PreprocessImageInput(session, rawInput),
                    ModelType.Embedding => PreprocessEmbeddingInput(session, rawInput as string),
                    _ => throw new NotSupportedException($"Model type {modelType} preprocessing not implemented"),
                };
            });
        }

        /// <summary>
        /// Converts inference outputs to a user-facing result for a given model type.
        /// </summary>
        public async Task<object> PostprocessOutputAsync(
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs,
            ModelType modelType)
        {
            return await Task.Run(() =>
            {
                return modelType switch
                {
                    ModelType.LLM => PostprocessTextOutput(outputs),
                    ModelType.Whisper => PostprocessAudioOutput(outputs),
                    ModelType.CLIP => PostprocessImageOutput(outputs),
                    ModelType.Embedding => PostprocessEmbeddingOutput(outputs),
                    _ => outputs.ToDictionary(o => o.Name, o => o.Value)
                };
            });
        }

        /// <summary>
        /// Creates ONNX SessionOptions based on the requested provider.
        /// </summary>
        private async Task<SessionOptions> CreateSessionOptionsAsync(ExecutionProvider provider)
        {
            var options = new SessionOptions
            {
                ExecutionMode = ExecutionMode.ORT_PARALLEL,
                InterOpNumThreads = Environment.ProcessorCount,
                IntraOpNumThreads = Environment.ProcessorCount,
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                EnableMemoryPattern = true,
                EnableCpuMemArena = true
            };

            switch (provider)
            {
                case ExecutionProvider.DirectML:
                    try
                    {
                        options.AppendExecutionProvider_DML(0);
                        _logger.LogInformation("Using DirectML execution provider");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "DirectML not available, falling back to CPU");
                        options.AppendExecutionProvider_CPU();
                    }
                    break;

                case ExecutionProvider.CUDA:
                    try
                    {
                        options.AppendExecutionProvider_CUDA(0);
                        _logger.LogInformation("Using CUDA execution provider");
                    }
                    catch
                    {
                        _logger.LogWarning("CUDA not available, falling back to CPU");
                        options.AppendExecutionProvider_CPU();
                    }
                    break;

                case ExecutionProvider.CPU:
                default:
                    options.AppendExecutionProvider_CPU();
                    _logger.LogInformation("Using CPU execution provider");
                    break;
            }

            return options;
        }

        /// <summary>
        /// Converts a plain string input into tokenized tensors for a language model (demo: simple tokenizer).
        /// </summary>
        private List<NamedOnnxValue> PreprocessTextInput(InferenceSession session, string? text)
        {
            text ??= string.Empty;

            // Demo: word index as token. In production, use a real tokenizer.
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select((w, i) => (long)(i + 1))
                .ToArray();

            var maxLength = 512;
            var paddedTokens = new long[maxLength];
            Array.Copy(tokens, paddedTokens, Math.Min(tokens.Length, maxLength));

            var attentionMask = paddedTokens.Select(t => t > 0 ? 1L : 0L).ToArray();

            return new List<NamedOnnxValue>
            {
                CreateTensor("input_ids", paddedTokens, new[] { 1, maxLength }),
                CreateTensor("attention_mask", attentionMask, new[] { 1, maxLength })
            };
        }

        /// <summary>
        /// Converts a raw audio input into a tensor for Whisper (placeholder for demo).
        /// </summary>
        private List<NamedOnnxValue> PreprocessAudioInput(InferenceSession session, object rawInput)
        {
            // TODO: Implement real mel spectrogram extraction
            var melSpectrogram = new float[80 * 3000]; // e.g., 80 bins x 3000 steps

            return new List<NamedOnnxValue>
            {
                CreateTensor("mel", melSpectrogram, new[] { 1, 80, 3000 })
            };
        }

        /// <summary>
        /// Converts a raw image input into a tensor for CLIP (placeholder for demo).
        /// </summary>
        private List<NamedOnnxValue> PreprocessImageInput(InferenceSession session, object rawInput)
        {
            // TODO: Implement real image preprocessing
            var imageSize = 224;
            var channels = 3;
            var imageData = new float[channels * imageSize * imageSize];
            // Normalize to [-1, 1]
            for (int i = 0; i < imageData.Length; i++)
                imageData[i] = (imageData[i] / 255.0f - 0.5f) * 2.0f;

            return new List<NamedOnnxValue>
            {
                CreateTensor("pixel_values", imageData, new[] { 1, channels, imageSize, imageSize })
            };
        }

        /// <summary>
        /// Converts text input to input_ids tensor for embeddings models (simple tokenizer for demo).
        /// </summary>
        private List<NamedOnnxValue> PreprocessEmbeddingInput(InferenceSession session, string? text)
        {
            text ??= string.Empty;
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select((w, i) => (long)(i + 1))
                .Take(512)
                .ToArray();

            var paddedTokens = new long[512];
            Array.Copy(tokens, paddedTokens, tokens.Length);

            return new List<NamedOnnxValue>
            {
                CreateTensor("input_ids", paddedTokens, new[] { 1, 512 })
            };
        }

        /// <summary>
        /// Converts language model logits output to a list of token IDs (demo: greedy decode).
        /// </summary>
        private object PostprocessTextOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
        {
            var logits = outputs.FirstOrDefault(o => o.Name == "logits");
            if (logits != null)
            {
                var tensor = logits.AsTensor<float>();
                var shape = tensor.Dimensions.ToArray();
                var predictions = new List<int>();

                // Simple greedy decode
                for (int i = 0; i < shape[1]; i++)
                {
                    float maxVal = float.MinValue;
                    int maxIdx = 0;
                    for (int j = 0; j < shape[2]; j++)
                    {
                        var val = tensor[0, i, j];
                        if (val > maxVal)
                        {
                            maxVal = val;
                            maxIdx = j;
                        }
                    }
                    predictions.Add(maxIdx);
                }
                // Demo: Return token ids as string
                return string.Join(" ", predictions.Select(p => $"token_{p}"));
            }
            return "No output found";
        }

        /// <summary>
        /// Converts Whisper output to transcription (placeholder for demo).
        /// </summary>
        private object PostprocessAudioOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
        {
            var textOutput = outputs.FirstOrDefault(o => o.Name.Contains("text") || o.Name.Contains("tokens"));
            if (textOutput != null)
                return "Transcribed audio text"; // TODO: decode tokens to text

            return "No transcription found";
        }

        /// <summary>
        /// Converts CLIP model output to embedding array (placeholder for demo).
        /// </summary>
        private object PostprocessImageOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
        {
            var embeddings = outputs.FirstOrDefault(o => o.Name.Contains("embed") || o.Name.Contains("features"));
            if (embeddings != null)
            {
                var tensor = embeddings.AsTensor<float>();
                return new
                {
                    Embeddings = tensor.ToArray(),
                    Dimensions = tensor.Dimensions.ToArray()
                };
            }
            return "No embeddings found";
        }

        /// <summary>
        /// Converts embedding model output to embedding array (placeholder for demo).
        /// </summary>
        private object PostprocessEmbeddingOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
        {
            var embeddings = outputs.FirstOrDefault();
            if (embeddings != null)
            {
                var tensor = embeddings.AsTensor<float>();
                return tensor.ToArray();
            }
            return Array.Empty<float>();
        }

        /// <summary>
        /// Disposes of sessions and internal resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _sessionLock?.Wait();
            try
            {
                foreach (var session in _sessionCache.Values)
                    session?.Dispose();
                _sessionCache.Clear();
            }
            finally
            {
                _sessionLock?.Release();
                _sessionLock?.Dispose();
            }
            _disposed = true;
        }
    }
}
