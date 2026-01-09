using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace IIM.Infrastructure.Embeddings;

public sealed class OllamaEmbeddingGenerator
	: IEmbeddingGenerator<EmbeddingWorkItem, Embedding<float>>,
	  IEmbeddingGenerator<string, Embedding<float>>,
	  IDisposable
{
	private readonly OllamaApiClient _client;
	private readonly string _modelId;
	private readonly int _dimensions;
	private bool _disposed;

	public EmbeddingGeneratorMetadata Metadata { get; }

	public OllamaEmbeddingGenerator(EmbeddingModelConfig config, ProviderConfig provider)
	{
		ArgumentNullException.ThrowIfNull(config);
		ArgumentNullException.ThrowIfNull(provider);

		if (string.IsNullOrWhiteSpace(config.ModelId))
			throw new ArgumentException("Embedding model ID is required.", nameof(config));

		_modelId = config.ModelId;
		_dimensions = config.Dimensions;
		_client = new OllamaApiClient(new Uri(provider.Endpoint));

		Metadata = new EmbeddingGeneratorMetadata(
			providerName: "Ollama",
			providerUri: new Uri(provider.Endpoint),
			defaultModelId: _modelId,
			defaultModelDimensions: _dimensions);
	}

	public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
		IEnumerable<EmbeddingWorkItem> values,
		EmbeddingGenerationOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(values);

		var texts = values.Select(v => v.Text).ToList();
		return await GenerateCoreAsync(texts, cancellationToken);
	}

	async Task<GeneratedEmbeddings<Embedding<float>>> IEmbeddingGenerator<string, Embedding<float>>.GenerateAsync(
		IEnumerable<string> values,
		EmbeddingGenerationOptions? options,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(values);

		var texts = values.ToList();
		return await GenerateCoreAsync(texts, cancellationToken);
	}

	private async Task<GeneratedEmbeddings<Embedding<float>>> GenerateCoreAsync(
		List<string> texts,
		CancellationToken cancellationToken)
	{
		var results = new GeneratedEmbeddings<Embedding<float>>();
		if (texts.Count == 0) return results;

		// Optional: cap input length here too (belt + suspenders)
		const int maxChars = 3500;
		var safe = texts.Select(t => string.IsNullOrWhiteSpace(t) ? "" :
			(t.Length <= maxChars ? t : t.Substring(0, maxChars))
		).ToList();

		// Batch size: keep it modest to avoid server limits
		const int batchSize = 32;

		for (int i = 0; i < safe.Count; i += batchSize)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var batch = safe.Skip(i).Take(batchSize).ToList();

			// Replace empties with a known short token to avoid weird server behavior
			for (int k = 0; k < batch.Count; k++)
				if (string.IsNullOrWhiteSpace(batch[k]))
					batch[k] = " ";

			OllamaSharp.Models.EmbedResponse response;
			try
			{
				response = await _client.EmbedAsync(new OllamaSharp.Models.EmbedRequest
				{
					Model = _modelId,
					Input = batch
				}, cancellationToken);
			}
			catch (OllamaSharp.Models.Exceptions.OllamaException ex)
				when (ex.Message.Contains("exceeds the context length", StringComparison.OrdinalIgnoreCase))
			{
				// Hard fallback: re-run each item with aggressive truncation
				foreach (var t in batch)
				{
					var tiny = t.Length <= 1500 ? t : t.Substring(0, 1500);

					var one = await _client.EmbedAsync(new OllamaSharp.Models.EmbedRequest
					{
						Model = _modelId,
						Input = [tiny]
					}, cancellationToken);

					results.Add(ToEmbedding(one, 0));
				}

				continue;
			}

			if (response.Embeddings == null || response.Embeddings.Count != batch.Count)
			{
				// Defensive: pad zeros for missing
				for (int k = 0; k < batch.Count; k++)
					results.Add(new Embedding<float>(new float[_dimensions]));
				continue;
			}

			for (int k = 0; k < response.Embeddings.Count; k++)
				results.Add(ToEmbedding(response, k));
		}

		return results;

		Embedding<float> ToEmbedding(OllamaSharp.Models.EmbedResponse r, int idx)
		{
			var v = r.Embeddings![idx];
			var floats = v.Select(d => (float)d).ToArray();
			return new Embedding<float>(floats.Length == _dimensions ? floats : PadOrTrim(floats));
		}

		float[] PadOrTrim(float[] v)
		{
			if (v.Length == _dimensions) return v;
			var outV = new float[_dimensions];
			Array.Copy(v, outV, Math.Min(v.Length, _dimensions));
			return outV;
		}
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
		_disposed = true;
		GC.SuppressFinalize(this);
	}
}