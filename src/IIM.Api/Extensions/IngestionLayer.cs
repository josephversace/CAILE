using IIM.Application.Workspace;
using IIM.Infrastructure.Docling;
using IIM.Infrastructure.Embeddings;
using IIM.Infrastructure.Services;
using IIM.Ingestion.Chunking;
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
		// ════════════════════════════════════════════════════════════════════
		// CHUNKING V2 SERVICES
		// ════════════════════════════════════════════════════════════════════

		// Chunking strategy factory (routes shapes to strategies)
		services.AddSingleton<ChunkingStrategyFactory>();

		// Document shape detector
		services.AddSingleton<DocumentShapeDetector>();

		// ════════════════════════════════════════════════════════════════════
		// INGESTION PIPELINE
		// ════════════════════════════════════════════════════════════════════

		// Use V2 pipeline with shape-aware chunking
		services.AddTransient<IIngestionPipeline, IngestionPipeline>();

		// ════════════════════════════════════════════════════════════════════
		// SUPPORTING SERVICES
		// ════════════════════════════════════════════════════════════════════

		services.AddTransient<IGraphRagPipeline, InMemoryGraphRagPipeline>();
		services.AddTransient<IDoclingService, DoclingService>();
		services.AddTransient<GraphExtractionJob, GraphExtractionJob>();
		services.AddTransient<IndicatorExtractor>();
		services.AddTransient<EntityLinkingJob>();
		services.AddTransient<AnalysisService>();
		services.AddTransient<KreuzbergExtractionService>();
		services.AddTransient<DoclingExtractionService>();
		services.AddTransient<DocumentExtractionRouter>();

		// ════════════════════════════════════════════════════════════════════
		// QDRANT
		// ════════════════════════════════════════════════════════════════════

		services.AddSingleton<IQdrantService>(sp =>
		{
			var cfg = sp.GetRequiredService<CaileConfig>()
				?? throw new InvalidOperationException("Missing Qdrant configuration.");

			var logger = sp.GetRequiredService<ILogger<QdrantService>>();
			return new QdrantService(cfg, logger);
		});

		// ════════════════════════════════════════════════════════════════════
		// KREUZBERG HTTP CLIENT
		// ════════════════════════════════════════════════════════════════════

		services.AddHttpClient<IKreuzbergClient, KreuzbergClient>((sp, client) =>
		{
			var cfg = sp.GetRequiredService<CaileConfig>().Kreuzberg;

			client.BaseAddress = new Uri(cfg.BaseUrl);
			client.Timeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds);
		});

		return services;
	}

	/// <summary>
	/// Register V2 context and evidence planning services.
	/// Call this in addition to AddIngestionLayer.
	/// </summary>
	public static IServiceCollection AddWorkspaceContextServices(
		this IServiceCollection services)
	{
		// V2 Context manager with tiered retrieval (full text vs semantic search)
		services.AddScoped<IWorkspaceContextManager, WorkspaceContextManager>();

		// V2 Evidence planner (intent → retrieval plan)
		services.AddScoped<IWorkspaceEvidencePlanner, WorkspaceEvidencePlanner>();

		return services;
	}
}