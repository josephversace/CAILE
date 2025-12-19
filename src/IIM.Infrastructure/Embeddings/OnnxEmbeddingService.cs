using IIM.Shared.Dtos;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;


namespace IIM.Infrastructure.Embeddings;

public sealed class OnnxEmbeddingGenerator
	: IEmbeddingGenerator<EmbeddingWorkItem, Embedding<float>>,
	 IEmbeddingGenerator<string, Embedding<float>>,
	 IDisposable
{
	private readonly InferenceSession _session;
	private readonly BertTokenizer _tokenizer;
	private readonly int _maxTokens;
	private readonly string _pooling;
	private readonly bool _normalize;
	private bool _disposed;

	private readonly int _hiddenSize;

	public EmbeddingGeneratorMetadata Metadata { get; }

public OnnxEmbeddingGenerator(EmbeddingModelDto config)
{
	ArgumentNullException.ThrowIfNull(config);

	var localPath = config.LocalPath ?? "";
	var modelPath = Path.Combine(localPath, "model.onnx");
	var vocabPath = Path.Combine(localPath, "vocab.txt");

	if (!File.Exists(modelPath))
		throw new FileNotFoundException($"Embedding model not found at {modelPath}");

	if (!File.Exists(vocabPath))
		throw new FileNotFoundException($"Tokenizer vocab not found at {vocabPath}");

		// ONNX session
		//var sessionOptions = new SessionOptions
		//{
		//	GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
		//};
		var sessionOptions = new SessionOptions();

		_session = new InferenceSession(modelPath, sessionOptions);

		string name = "";

		foreach (var kvp in _session.InputMetadata)
		{
			name =
				$"Name={kvp.Key}, Type={kvp.Value.ElementType}, Dims=[{string.Join(",", kvp.Value.Dimensions)}]";
		}

		foreach (var kvp in _session.OutputMetadata)
{
    name =
        $"Out={kvp.Key}, Type={kvp.Value.ElementType}, Dims=[{string.Join(",", kvp.Value.Dimensions)}]";
}



		// CORRECT tokenizer initialization (ML.Tokenizers)
		_tokenizer = BertTokenizer.Create(
		vocabPath,
		new BertOptions
		{
			LowerCaseBeforeTokenization = true
		});


	_maxTokens = config.MaxTokens > 0 ? config.MaxTokens : 256;
	_hiddenSize = config.Dimensions > 0 ? config.Dimensions : 384;
	_pooling = config.Pooling?.ToLowerInvariant() ?? "mean";
	_normalize = config.Normalize;

	Metadata = new EmbeddingGeneratorMetadata(
		providerName: "ONNX",
		providerUri: null,
		defaultModelId: config.Id,
		defaultModelDimensions: _hiddenSize);
}



public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
		IEnumerable<EmbeddingWorkItem> values,
		EmbeddingGenerationOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(values);

		var results = new GeneratedEmbeddings<Embedding<float>>();

		foreach (var item in values)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var vector = GenerateEmbedding(item.Text);
			results.Add(new Embedding<float>(vector));
		}

		return results;
	}

	async Task<GeneratedEmbeddings<Embedding<float>>> IEmbeddingGenerator<string, Embedding<float>>.GenerateAsync(
		IEnumerable<string> values,
		EmbeddingGenerationOptions? options,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(values);

		var results = new GeneratedEmbeddings<Embedding<float>>();

		foreach (var text in values)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var vector = GenerateEmbedding(text);
			results.Add(new Embedding<float>(vector));
		}

		return results;
	}

	private float[] GenerateEmbedding(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return new float[_hiddenSize];

		// Tokenize
		var ids = _tokenizer.EncodeToIds(
			text,
			addSpecialTokens: true,
			considerNormalization: true);


		int seqLen = Math.Min(ids.Count, 16); // keep it small

		if (seqLen == 0)
		{
			Console.WriteLine("No tokens produced.");
			return new float[_hiddenSize];

		}

		Console.WriteLine("Token IDs: " + string.Join(", ", ids.Take(seqLen)));

		// Build Int64 tensors [1, seqLen]
		var inputIds = new DenseTensor<long>(new[] { 1, seqLen });
		var attentionMask = new DenseTensor<long>(new[] { 1, seqLen });
		var tokenTypeIds = new DenseTensor<long>(new[] { 1, seqLen });

		for (int i = 0; i < seqLen; i++)
		{
			inputIds[0, i] = ids[i];
			attentionMask[0, i] = 1;
			tokenTypeIds[0, i] = 0;
		}

		var n1 = NamedOnnxValue.CreateFromTensor("input_ids", inputIds);
		var n2 = NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask);
		var n3 = NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds);

		var inputArray = new[] { n1, n2, n3 };

		Console.WriteLine("Running inference...");
		using var outputs = _session.Run(inputArray);

		var hidden = outputs
			.First(v => v.Name == "last_hidden_state")
			.AsTensor<float>();

		// hidden: [1, seqLen, hiddenSize]
		seqLen = hidden.Dimensions[1];
		int hiddenSize = hidden.Dimensions[2];

		// ---- Mean pooling over attention mask ----
		var embedding = new float[hiddenSize];
		float tokenCount = 0;

		for (int t = 0; t < seqLen; t++)
		{
			if (attentionMask[0, t] == 0)
				continue;

			for (int d = 0; d < hiddenSize; d++)
				embedding[d] += hidden[0, t, d];

			tokenCount++;
		}

		if (tokenCount > 0)
		{
			for (int d = 0; d < hiddenSize; d++)
				embedding[d] /= tokenCount;
		}

		// ---- Optional L2 normalization (BGE expects this) ----
		float norm = 0f;
		for (int i = 0; i < embedding.Length; i++)
			norm += embedding[i] * embedding[i];

		norm = MathF.Sqrt(norm);

		if (norm > 0)
		{
			for (int i = 0; i < embedding.Length; i++)
				embedding[i] /= norm;
		}

		// Debug sanity check
		Console.WriteLine(
			$"Embedding dims={embedding.Length}, first 8: {string.Join(", ", embedding.Take(8))}");

		return embedding;



	}



	private static float[] MeanPool(DenseTensor<float> hidden, DenseTensor<long> mask)
	{
		int seqLen = hidden.Dimensions[1];
		int hiddenSize = hidden.Dimensions[2];

		var result = new float[hiddenSize];
		float count = 0;

		for (int t = 0; t < seqLen; t++)
		{
			if (mask[0, t] == 0)
				continue;

			for (int d = 0; d < hiddenSize; d++)
				result[d] += hidden[0, t, d];

			count++;
		}

		if (count == 0)
			return result;

		for (int d = 0; d < hiddenSize; d++)
			result[d] /= count;

		return result;
	}


	private static float[] ClsPool(Tensor<float> hiddenState)
	{
		var hiddenSize = (int)hiddenState.Dimensions[2];
		var result = new float[hiddenSize];

		for (int j = 0; j < hiddenSize; j++)
			// 🐛 FIX CS1501 (Line 241): Use the correct indexer for DenseTensor
			result[j] = hiddenState[0, 0, j]; // [Batch=0, CLS_Token=0, Dim=j]

		return result;
	}

	private static void Normalize(float[] vector)
	{
		var norm = MathF.Sqrt(vector.Sum(x => x * x));
		if (norm == 0) return;

		for (int i = 0; i < vector.Length; i++)
			vector[i] /= norm;
	}

	public object? GetService(Type serviceType, object? serviceKey = null)
	{
		if (serviceKey != null)
			return null;

		if (serviceType == typeof(EmbeddingGeneratorMetadata))
			return Metadata;

		if (serviceType.IsAssignableFrom(GetType()))
			return this;

		return null;
	}

	public void Dispose()
	{
		if (_disposed) return;
		_session.Dispose();
		_disposed = true;
		GC.SuppressFinalize(this);
	}
}