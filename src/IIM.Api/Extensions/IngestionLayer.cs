using IIM.Infrastructure.Docling;
using IIM.Infrastructure.Embeddings;
using IIM.Ingestion.Indicators;
using IIM.Ingestion.Interfaces;
using IIM.Ingestion.Services;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Api.Extensions;

public static class IngestionExtensions
{
	public static IServiceCollection AddIngestionLayer(
		this IServiceCollection services)
	{
		// Register ingestion pipeline + services
		services.AddTransient<IIngestionPipeline, IngestionPipeline>();
		services.AddTransient<IGraphRagPipeline, InMemoryGraphRagPipeline>();
		services.AddTransient<IDoclingService, DoclingService>();
		services.AddTransient<DocumentShapeDetector>();
		services.AddTransient<ChunkingService>();
		services.AddTransient<GraphExtractionJob, GraphExtractionJob>();
		services.AddTransient<IndicatorExtractor>();



		// In your DI setup
		services.AddTransient<EntityLinkingJob>();

		services.AddTransient<AnalysisService>();
		services.AddTransient<KreuzbergExtractionService>();
		services.AddTransient<DoclingExtractionService>();
		services.AddTransient<DocumentExtractionRouter>();

		services.AddSingleton<IQdrantService>(sp =>
		{
			var cfg = sp.GetRequiredService<CaileConfig>()
				?? throw new InvalidOperationException("Missing Qdrant configuration.");

			var logger = sp.GetRequiredService<ILogger<QdrantService>>();
			return new QdrantService(cfg, logger);
		});

	

		services.AddHttpClient<IKreuzbergClient, KreuzbergClient>((sp, client) =>
		{
			var cfg = sp.GetRequiredService<CaileConfig>().Kreuzberg;

			client.BaseAddress = new Uri(cfg.BaseUrl);
			client.Timeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds);
		});

	
	

		return services;
	}
}
