using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BERTTokenizers;
using BERTTokenizers.Base;
using IIM.Shared.Dtos;
using Microsoft.Extensions.AI;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace IIM.Infrastructure.Embeddings;

public class OnnxEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
	private readonly InferenceSession _session;
	private readonly TokenizerBase _tokenizer;
	private readonly int _maxTokens;
	private readonly int _dimensions;
	private readonly string _pooling;
	private readonly bool _normalize;
	private bool _disposed;

	public EmbeddingGeneratorMetadata Metadata { get; }

	public OnnxEmbeddingGenerator(EmbeddingModelDto config)
	{
		ArgumentNullException.ThrowIfNull(config);

		var localPath = config.LocalPath ?? "";
		var modelPath = Path.Combine(localPath, "model.onnx");
		var vocabPath = Path.Combine(localPath, "vocab.txt");

		if (!File.Exists(modelPath))
			throw new FileNotFoundException($"Embedding model not found at {modelPath}");

		var sessionOptions = new SessionOptions
		{
			GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
		};

		_session = new InferenceSession(modelPath, sessionOptions);

		// Use custom vocab if present, otherwise fall back to built-in BERT uncased
		_tokenizer = File.Exists(vocabPath)
			? new BertUnasedCustomVocabulary(vocabPath)  // Note: typo in package
			: new BertUncasedBaseTokenizer();

		_maxTokens = config.MaxTokens > 0 ? config.MaxTokens : 256;
		_dimensions = config.Dimensions > 0 ? config.Dimensions : 384;
		_pooling = config.Pooling ?? "mean";
		_normalize = config.Normalize;

		Metadata = new EmbeddingGeneratorMetadata(
			providerName: "ONNX",
			providerUri: null,
			defaultModelId: config.Id,
			defaultModelDimensions: _dimensions);
	}

	public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
		IEnumerable<string> values,
		EmbeddingGenerationOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(values);

		var texts = values.ToList();
		var results = new GeneratedEmbeddings<Embedding<float>>();

		foreach (var text in texts)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var embedding = await Task.Run(() => GenerateEmbedding(text), cancellationToken);
			results.Add(new Embedding<float>(embedding));
		}

		return results;
	}

	private float[] GenerateEmbedding(string text)
	{
		var encoded = _tokenizer.Encode(_maxTokens, text);

		var inputIds = encoded.Select(t => t.InputIds).ToArray();
		var attentionMask = encoded.Select(t => t.AttentionMask).ToArray();
		var tokenTypeIds = encoded.Select(t => t.TokenTypeIds).ToArray();

		var inputIdsTensor = new DenseTensor<long>(inputIds, [1, inputIds.Length]);
		var attentionMaskTensor = new DenseTensor<long>(attentionMask, [1, attentionMask.Length]);
		var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, [1, tokenTypeIds.Length]);

		var inputs = new List<NamedOnnxValue>
		{
			NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
			NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
			NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
		};

		using var outputs = _session.Run(inputs);

		var lastHiddenState = outputs.First().AsTensor<float>();

		var result = _pooling switch
		{
			"cls" => ClsPool(lastHiddenState),
			_ => MeanPool(lastHiddenState, attentionMask)
		};

		if (_normalize)
			Normalize(result);

		return result;
	}

	private static float[] MeanPool(Tensor<float> hiddenState, long[] attentionMask)
	{
		var seqLen = hiddenState.Dimensions[1];
		var hiddenSize = hiddenState.Dimensions[2];
		var result = new float[hiddenSize];

		float tokenCount = attentionMask.Sum();
		if (tokenCount == 0) tokenCount = 1;

		for (int i = 0; i < seqLen; i++)
		{
			if (attentionMask[i] == 0) continue;

			for (int j = 0; j < hiddenSize; j++)
			{
				result[j] += hiddenState[0, i, j];
			}
		}

		for (int j = 0; j < hiddenSize; j++)
		{
			result[j] /= tokenCount;
		}

		return result;
	}

	private static float[] ClsPool(Tensor<float> hiddenState)
	{
		var hiddenSize = hiddenState.Dimensions[2];
		var result = new float[hiddenSize];

		for (int j = 0; j < hiddenSize; j++)
		{
			result[j] = hiddenState[0, 0, j];
		}

		return result;
	}

	private static void Normalize(float[] vector)
	{
		var norm = MathF.Sqrt(vector.Sum(x => x * x));
		if (norm > 0)
		{
			for (int i = 0; i < vector.Length; i++)
			{
				vector[i] /= norm;
			}
		}
	}

	public object? GetService(Type serviceType, object? serviceKey = null)
	{
		ArgumentNullException.ThrowIfNull(serviceType);

		if (serviceKey is not null)
			return null;

		if (serviceType == typeof(EmbeddingGeneratorMetadata))
			return Metadata;

		if (serviceType.IsAssignableFrom(GetType()))
			return this;

		return null;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_session.Dispose();
			_disposed = true;
		}
		GC.SuppressFinalize(this);
	}
}