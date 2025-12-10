using IIM.Infrastructure.Docling;
using IIM.Infrastructure.Embeddings;
using IIM.Ingestion.Interfaces;
using IIM.Ingestion.Services;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Api.Extensions;

public static class IngestionExtensions
{
	public static IServiceCollection AddIngestionLayer(
		this IServiceCollection services,
		IConfiguration config)
	{
		// Register ingestion pipeline + services
		services.AddTransient<IIngestionPipeline, IngestionPipeline>();
		services.AddTransient<IGraphRagPipeline, InMemoryGraphRagPipeline>();
		services.AddTransient<IDoclingService, DoclingService>();
		services.AddTransient<ChunkingService>();
		services.AddTransient<GraphService>();
		services.AddTransient<AnalysisService>();
		services.AddTransient<GraphRagPipeline>();

		services.AddSingleton<IQdrantService>(sp =>
		{
			var cfg = sp.GetRequiredService<CaileConfig>().Qdrant
				?? throw new InvalidOperationException("Missing Qdrant configuration.");

			var logger = sp.GetRequiredService<ILogger<QdrantService>>();
			return new QdrantService(cfg, logger);
		});

		// ONNX Embeddings
		services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
		{

			var templateResolver = sp.GetRequiredService<IModelTemplateResolver>();

			var active = templateResolver.GetActiveTemplateAsync().GetAwaiter().GetResult();


			var embeddingModel = active.Models.Embedding
				?? throw new InvalidOperationException($"No Embedding model defined in template '{active.Name}'.");

			var localPath = embeddingModel.LocalPath
				?? throw new InvalidOperationException($"No LocalPath defined for Embedding model in template '{active.Name}'.");

			var modelPath = Path.Combine(localPath, "model.onnx");
			var vocabPath = Path.Combine(localPath, "vocab.txt");

			if (!File.Exists(modelPath))
				throw new FileNotFoundException($"Embedding model not found at {modelPath}");

			if (!File.Exists(vocabPath))
				throw new FileNotFoundException($"Vocab file not found at {vocabPath}");

			return new OnnxEmbeddingGenerator((EmbeddingModelDto)embeddingModel);
		});

	
		return services;
	}
}
