using IIM.Application.Urls;
using IIM.Infrastructure.Docling;
using IIM.Infrastructure.Embeddings;
using IIM.Infrastructure.Services;
using IIM.Ingestion.Chunking;
using IIM.Ingestion.Services;
using IIM.Ingestion.Services.Steps;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Api.Extensions;

public static class IngestionExtensions
{
	public static IServiceCollection AddIngestionLayer(this IServiceCollection services)
	{
		// ════════════════════════════════════════════════════════════════════
		// CHUNKING V2 SERVICES
		// ════════════════════════════════════════════════════════════════════
		services.AddSingleton<ChunkingStrategyFactory>();
		services.AddSingleton<DocumentShapeDetector>();

		// ════════════════════════════════════════════════════════════════════
		// INGESTION RUNNER + PIPELINE (runner-based)
		// ════════════════════════════════════════════════════════════════════
		services.AddScoped<IngestionStepRunner>();

		services.AddScoped<IIngestionRunner, IngestionRunnerService>();

		// ════════════════════════════════════════════════════════════════════
		// STEPS
		// ════════════════════════════════════════════════════════════════════
		// Core/meta
		services.AddScoped<IIngestionStep, MetaExifFastStep>();

		// Text path
		services.AddScoped<IIngestionStep, DocExtractTextStep>();
		services.AddScoped<IIngestionStep, IocRegexExtractStep>();
		services.AddScoped<IIngestionStep, ChunkBuildStep>();
		services.AddScoped<IIngestionStep, EmbedIndexQdrantStep>();

		// Excel path (still “last in plan”, but deps will pull them in when needed)
		services.AddScoped<IIngestionStep, ExcelStructureDetectStep>();
		services.AddScoped<IIngestionStep, ExcelCanonicalizeStep>();

		// AI steps
		services.AddScoped<IIngestionStep, AiTextAnalysisStep>();
		services.AddScoped<IIngestionStep, AiImageDescribeStep>();

		// ════════════════════════════════════════════════════════════════════
		// SUPPORTING SERVICES USED BY STEPS / PIPELINE CONTEXT
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
		services.AddTransient<ExcelStructureDetector>();
		services.AddTransient<ExcelCanonicalizer>();
		services.AddTransient<ICanonicalDocumentBuilder, CanonicalDocumentBuilder>();
		services.AddTransient<IAriaSnapshotParser, AriaSnapshotParser>();

		// ExifTool can remain singleton if it’s stateless/thread-safe
		services.AddSingleton<IExifToolService, ExifToolService>();

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
}
