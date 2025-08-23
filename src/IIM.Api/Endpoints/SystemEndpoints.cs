using IIM.Core.AI;
using IIM.Core.Inference;
using IIM.Shared.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace IIM.Api.Endpoints;

public static class SystemEndpoints
{
    public static void MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        // Health check - simple endpoint for monitoring
        api.MapGet("/healthz", () => Results.Text("ok"))
            .WithName("HealthCheck")
            .WithOpenApi()
            .AllowAnonymous();

        // GPU information endpoint
        api.MapGet("/v1/gpu", async (IModelOrchestrator orchestrator) =>
        {
            var stats = await orchestrator.GetStatsAsync();

            var response = new GpuInfoResponse(
                Vendor: "AMD/NVIDIA/CPU",
                Provider: "directml",
                VramGb: 128,
                AvailableMemoryGb: stats.AvailableMemory / 1_073_741_824,
                UsedMemoryGb: stats.TotalMemoryUsage / 1_073_741_824,
                LoadedModels: stats.LoadedModels,
                Models: stats.Models
            );

            return Results.Ok(response);
        })
        .WithName("GetGpuInfo")
        .WithOpenApi()
        .Produces<GpuInfoResponse>(200);

        // System statistics endpoint
        api.MapGet("/v1/stats", async (
            IInferencePipeline pipeline,
            IModelOrchestrator orchestrator) =>
        {
            var pipelineStats = pipeline.GetStats();
            var orchestratorStats = await orchestrator.GetStatsAsync();

            var response = new SystemStatsResponse(
                Pipeline: pipelineStats,
                Orchestrator: orchestratorStats,
                Timestamp: DateTimeOffset.UtcNow
            );

            return Results.Ok(response);
        })
        .WithName("GetSystemStats")
        .WithOpenApi()
        .Produces<SystemStatsResponse>(200);
    }
}