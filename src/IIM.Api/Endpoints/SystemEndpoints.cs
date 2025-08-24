using IIM.Core.Services;
using IIM.Infrastructure.Platform;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

/// <summary>
/// System management and health check endpoints
/// </summary>
public static class SystemEndpoints
{
    /// <summary>
    /// Maps system-related endpoints including health checks and status monitoring
    /// </summary>
    public static void MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var system = app.MapGroup("/api/system")
            .WithTags("System")
            .WithOpenApi();

        // Get system status
        system.MapGet("/status", async (
            [FromServices] ISystemService systemService,
            CancellationToken ct) =>
        {
            var status = await systemService.GetSystemStatusAsync(ct);
            return Results.Ok(status);
        })
        .WithName("GetSystemStatus")
        .WithSummary("Get overall system status including services and resources")
        .Produces<SystemStatus>();

        // Get system metrics
        system.MapGet("/metrics", async (
            [FromServices] ISystemService systemService,
            CancellationToken ct) =>
        {
            var metrics = await systemService.GetMetricsAsync(ct);
            return Results.Ok(metrics);
        })
        .WithName("GetSystemMetrics")
        .WithSummary("Get system performance metrics")
        .Produces<SystemMetrics>();

        // Get resource usage
        system.MapGet("/resources", async (
            [FromServices] ISystemService systemService,
            CancellationToken ct) =>
        {
            var resources = await systemService.GetResourceUsageAsync(ct);
            return Results.Ok(resources);
        })
        .WithName("GetResourceUsage")
        .WithSummary("Get current resource usage (CPU, memory, GPU)")
        .Produces<ResourceUsage>();

        // Get configuration
        system.MapGet("/configuration", async (
            [FromServices] IConfigurationService configService,
            CancellationToken ct) =>
        {
            var config = await configService.GetConfigurationAsync(ct);
            return Results.Ok(config);
        })
        .WithName("GetConfiguration")
        .WithSummary("Get system configuration")
        .Produces<Dictionary<string, object>>();

        // Update configuration
        system.MapPost("/configuration", async (
            [FromBody] Dictionary<string, object> config,
            [FromServices] IConfigurationService configService,
            CancellationToken ct) =>
        {
            await configService.UpdateConfigurationAsync(config, ct);
            return Results.Ok(new { message = "Configuration updated successfully" });
        })
        .WithName("UpdateConfiguration")
        .WithSummary("Update system configuration")
        .RequireAuthorization();

        // Restart services
        system.MapPost("/restart", async (
            [FromServices] ISystemService systemService,
            CancellationToken ct) =>
        {
            await systemService.RestartServicesAsync(ct);
            return Results.Ok(new { message = "Services restarted successfully" });
        })
        .WithName("RestartServices")
        .WithSummary("Restart system services")
        .RequireAuthorization();
    }
}