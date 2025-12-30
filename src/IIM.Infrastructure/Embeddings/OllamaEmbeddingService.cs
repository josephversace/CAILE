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

		if (texts.Count == 0)
			return results;

		// Ollama supports batch embedding
		foreach (var text in texts)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (string.IsNullOrWhiteSpace(text))
			{
				// Return zero vector for empty text
				results.Add(new Embedding<float>(new float[_dimensions]));
				continue;
			}

			var response = await _client.EmbedAsync(new OllamaSharp.Models.EmbedRequest
			{
				Model = _modelId,
				Input = [text]
			}, cancellationToken);

			if (response.Embeddings != null && response.Embeddings.Count > 0)
			{
				// OllamaSharp returns double[], convert to float[]
				var embedding = response.Embeddings[0];
				var floatVector = embedding.Select(d => (float)d).ToArray();
				results.Add(new Embedding<float>(floatVector));
			}
			else
			{
				// Fallback to zero vector if no embedding returned
				results.Add(new Embedding<float>(new float[_dimensions]));
			}
		}

		return results;
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