// ═══════════════════════════════════════════════════════════════════════════════
// SERVICE REGISTRATION FOR CHUNKING V2
// ═══════════════════════════════════════════════════════════════════════════════
//
// Add this to your Program.cs or Startup.cs to register the new services.
//
// ═══════════════════════════════════════════════════════════════════════════════

using IIM.Application.Workspace;
using IIM.Ingestion.Chunking;
using IIM.Ingestion.Services;
using IIM.Infrastructure.Services;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Ingestion.Extensions;

public static class ChunkingServiceExtensions
{
    /// <summary>
    /// Register all services for the V2 chunking and context system.
    /// </summary>
    public static IServiceCollection AddChunkingV2Services(this IServiceCollection services)
    {
        // ────────────────────────────────────────────────────────────────────
        // INGESTION SERVICES
        // ────────────────────────────────────────────────────────────────────

        // Chunking strategy factory (routes shapes to strategies)
        services.AddSingleton<ChunkingStrategyFactory>();

        // Document shape detector (already exists, ensure registered)
        services.AddSingleton<DocumentShapeDetector>();

        // V2 Ingestion pipeline
        services.AddScoped<IIngestionPipeline, IngestionPipelineV2>();

        // ────────────────────────────────────────────────────────────────────
        // QUERY-TIME SERVICES
        // ────────────────────────────────────────────────────────────────────

        // V2 Context manager with tiered retrieval
        services.AddScoped<IWorkspaceContextManager, WorkspaceContextManagerV2>();

        // V2 Evidence planner
        services.AddScoped<IWorkspaceEvidencePlanner, WorkspaceEvidencePlannerV2>();

        return services;
    }

    /// <summary>
    /// Register only the chunking components (for testing or gradual migration).
    /// </summary>
    public static IServiceCollection AddChunkingComponents(this IServiceCollection services)
    {
        services.AddSingleton<ChunkingStrategyFactory>();
        services.AddSingleton<DocumentShapeDetector>();
        return services;
    }
}
