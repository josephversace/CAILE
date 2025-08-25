
using IIM.Application.Wsl;
using IIM.Core.Mediator;
using IIM.Infrastructure.Platform;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

/// <summary>
/// WSL (Windows Subsystem for Linux) management endpoints
/// </summary>
public static class WslEndpoints
{
    /// <summary>
    /// Maps all WSL-related endpoints for management and monitoring
    /// </summary>
    public static void MapWslEndpoints(this IEndpointRouteBuilder app)
    {
        var wsl = app.MapGroup("/api/wsl")
            .WithTags("WSL")
            .WithOpenApi();

        // ========================================
        // WSL STATUS & HEALTH
        // ========================================

        // Get WSL status
        wsl.MapGet("/status", async (
            [FromServices] IWslManager wslManager,
            CancellationToken ct) =>
        {
            var status = await wslManager.GetStatusAsync(ct);
            return Results.Ok(status);
        })
        .WithName("GetWslStatus")
        .WithSummary("Get current WSL status and configuration")
        .Produces<WslStatus>();

        // Get WSL health with service status
        wsl.MapGet("/health", async (
            [FromServices] IWslManager wslManager,
            [FromServices] IWslServiceOrchestrator serviceOrchestrator,
            CancellationToken ct) =>
        {
            var status = await wslManager.GetStatusAsync(ct);
            var services = await serviceOrchestrator.GetAllServiceStatusAsync(ct);

            var health = new WslHealthResponse
            {
                IsHealthy = status.IsRunning && services.Values.All(s => s.IsHealthy),
                Status = status,
                Services = services,
                Issues = new List<string>()
            };

            // Check for issues
            if (!status.IsRunning)
                health.Issues.Add("WSL is not running");

            foreach (var service in services.Where(s => !s.Value.IsHealthy))
                health.Issues.Add($"Service {service.Key} is unhealthy");

            return Results.Ok(health);
        })
        .WithName("GetWslHealth")
        .WithSummary("Get WSL health including all service statuses")
        .Produces<WslHealthResponse>();

        // ========================================
        // WSL LIFECYCLE MANAGEMENT
        // ========================================

        // Ensure WSL is setup and running
        wsl.MapPost("/ensure", async (
            [FromServices] IMediator mediator,
            CancellationToken ct,
            [FromBody] EnsureWslRequest? request = null) =>
        {
            var command = new EnsureWslCommand
            {
                Timeout = request?.TimeoutSeconds != null
                    ? TimeSpan.FromSeconds(request.TimeoutSeconds.Value)
                    : TimeSpan.FromMinutes(5),
                ForceReinstall = request?.ForceReinstall ?? false
            };

            await mediator.Send(command, ct);
            return Results.Ok(new { message = "WSL setup completed successfully" });
        })
        .WithName("EnsureWsl")
        .WithSummary("Ensure WSL2 is installed and configured")
        .Produces<object>();

        //// Start WSL
        //wsl.MapPost("/start", async (
        //    [FromServices] IWslManager wslManager,
        //    [FromServices] IWslServiceOrchestrator serviceOrchestrator,
        //    CancellationToken ct,
        //    [FromBody] StartWslCommand? request = null) =>
        //{
        //    // Start WSL distro
        //    var started = await wslManager.StartServicesAsync(
        //        request?.DistroName ?? "IIM-Ubuntu",
        //        ct);

        //    if (!started)
        //    {
        //        return Results.Problem("Failed to start WSL distribution");
        //    }

        //    // Start services if requested
        //    if (request?.StartServices ?? true)
        //    {
        //        var servicesToStart = request?.ServicesToStart ??
        //            new List<string> { "ollama", "minio", "postgres", "redis" };

        //        foreach (var service in servicesToStart)
        //        {
        //            await serviceOrchestrator.StartServiceAsync(service, ct);
        //        }
        //    }

        //    return Results.Ok(new { message = "WSL started successfully" });
        //})
        //.WithName("StartWsl")
        //.WithSummary("Start WSL distribution and services")
        //.Produces<object>()
        //.ProducesProblem(StatusCodes.Status500InternalServerError);

        // Stop WSL
        wsl.MapPost("/stop", async (
            [FromServices] IWslManager wslManager,
            [FromServices] IWslServiceOrchestrator serviceOrchestrator,
            CancellationToken ct,
            [FromBody] StopWslCommand? request = null) =>
        {
            var distroName = request?.DistroName ?? "IIM-Ubuntu";

            // Stop services first if not force stopping
            if (!(request?.ForceStop ?? false))
            {
                await serviceOrchestrator.StopAllServicesAsync(ct);

                // Wait for graceful shutdown
                if (request?.GracePeriodSeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(request.GracePeriodSeconds.Value), ct);
                }
            }

            // Stop WSL
            var stopped = await wslManager.StopDistroAsync(distroName, ct);

            return stopped
                ? Results.Ok(new { message = "WSL stopped successfully" })
                : Results.Problem("Failed to stop WSL distribution");
        })
        .WithName("StopWsl")
        .WithSummary("Stop WSL distribution and services")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        //// Restart WSL
        //wsl.MapPost("/restart", async (
        //    [FromServices] IWslManager wslManager,
        //    [FromServices] IWslServiceOrchestrator serviceOrchestrator,
        //    CancellationToken ct) =>
        //{
        //    // Stop all services
        //    await serviceOrchestrator.StopAllServicesAsync(ct);

        //    // Stop WSL
        //    await wslManager.StopDistroAsync("IIM-Ubuntu", ct);

        //    // Wait a moment
        //    await Task.Delay(2000, ct);

        //    // Start WSL
        //    await wslManager.StartDistroAsync("IIM-Ubuntu", ct);

        //    // Start essential services
        //    await serviceOrchestrator.StartServiceAsync("ollama", ct);
        //    await serviceOrchestrator.StartServiceAsync("minio", ct);

        //    return Results.Ok(new { message = "WSL restarted successfully" });
        //})
        //.WithName("RestartWsl")
        //.WithSummary("Restart WSL and all services")
        //.Produces<object>();

        // ========================================
        // SERVICE MANAGEMENT
        // ========================================

        // Get all services status
        wsl.MapGet("/services", async (
            [FromServices] IWslServiceOrchestrator serviceOrchestrator,
            CancellationToken ct) =>
        {
            var services = await serviceOrchestrator.GetAllServiceStatusAsync(ct);

            return Results.Ok(new ServiceStatusListResponse
            {
                Services = services,
                TotalServices = services.Count,
                HealthyServices = services.Count(s => s.Value.IsHealthy),
                UnhealthyServices = services.Count(s => !s.Value.IsHealthy),
                CheckedAt = DateTimeOffset.UtcNow
            });
        })
        .WithName("GetWslServices")
        .WithSummary("Get status of all WSL services")
        .Produces<ServiceStatusListResponse>();

        // Start specific service
        wsl.MapPost("/services/{serviceName}/start", async (
            string serviceName,
            [FromServices] IWslServiceOrchestrator serviceOrchestrator,
            CancellationToken ct) =>
        {
            var started = await serviceOrchestrator.StartServiceAsync(serviceName, ct);

            return started
                ? Results.Ok(new { message = $"Service {serviceName} started" })
                : Results.Problem($"Failed to start service {serviceName}");
        })
        .WithName("StartWslService")
        .WithSummary("Start a specific WSL service")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Stop specific service
        wsl.MapPost("/services/{serviceName}/stop", async (
            string serviceName,
            [FromServices] IWslServiceOrchestrator serviceOrchestrator,
            CancellationToken ct) =>
        {
            var stopped = await serviceOrchestrator.StopServiceAsync(serviceName, ct);

            return stopped
                ? Results.Ok(new { message = $"Service {serviceName} stopped" })
                : Results.Problem($"Failed to stop service {serviceName}");
        })
        .WithName("StopWslService")
        .WithSummary("Stop a specific WSL service")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Restart specific service
        wsl.MapPost("/services/{serviceName}/restart", async (
            string serviceName,
            [FromServices] IWslServiceOrchestrator serviceOrchestrator,
            CancellationToken ct) =>
        {
            var restarted = await serviceOrchestrator.RestartServiceAsync(serviceName, ct);

            return restarted
                ? Results.Ok(new { message = $"Service {serviceName} restarted" })
                : Results.Problem($"Failed to restart service {serviceName}");
        })
        .WithName("RestartWslService")
        .WithSummary("Restart a specific WSL service")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // ========================================
        // COMMAND EXECUTION
        // ========================================

        // Execute command in WSL
        wsl.MapPost("/execute", async (
            [FromBody] ExecuteWslCommandRequest request,
            [FromServices] IWslManager wslManager,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            // Validate command (basic security check)
            if (string.IsNullOrWhiteSpace(request.Command))
            {
                return Results.BadRequest(new { error = "Command cannot be empty" });
            }

            // Execute command
            var result = await wslManager.ExecuteCommandAsync(
                request.DistroName ?? "IIM-Ubuntu",
                request.Command,
                ct);

            return Results.Ok(new
            {
                ExitCode = result.ExitCode,
                Output = result.StandardOutput,
                Error = result.StandardError,
                Success = result.ExitCode == 0,
                ExecutionTime = result.ExecutionTime
            });
        })
        .WithName("ExecuteWslCommand")
        .WithSummary("Execute a command in WSL")
        .RequireAuthorization() // Require auth for command execution
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ========================================
        // FILE OPERATIONS
        // ========================================

        // Copy file to WSL
        wsl.MapPost("/files/copy-to-wsl", async (
            [FromBody] CopyFileToWslRequest request,
            [FromServices] IWslManager wslManager,
            CancellationToken ct) =>
        {
            var success = await wslManager.CopyFileToWslAsync(
                request.WindowsPath,
                request.WslPath,
                ct);

            return success
                ? Results.Ok(new { message = "File copied successfully" })
                : Results.Problem("Failed to copy file to WSL");
        })
        .WithName("CopyFileToWsl")
        .WithSummary("Copy a file from Windows to WSL")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Copy file from WSL
        wsl.MapPost("/files/copy-from-wsl", async (
            [FromBody] CopyFileFromWslRequest request,
            [FromServices] IWslManager wslManager,
            CancellationToken ct) =>
        {
            var success = await wslManager.CopyFileFromWslAsync(
                request.WslPath,
                request.WindowsPath,
                ct);

            return success
                ? Results.Ok(new { message = "File copied successfully" })
                : Results.Problem("Failed to copy file from WSL");
        })
        .WithName("CopyFileFromWsl")
        .WithSummary("Copy a file from WSL to Windows")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // ========================================
        // PROXY CONFIGURATION
        // ========================================

        // Configure proxy (Tor)
        wsl.MapPost("/proxy/configure", async (
            [FromBody] ProxyConfigDto config,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ConfigureProxyCommand(config);
            await mediator.Send(command, ct);

            return Results.Ok(new { message = "Proxy configured successfully" });
        })
        .WithName("ConfigureWslProxy")
        .WithSummary("Configure proxy settings for WSL")
        .RequireAuthorization()
        .Produces<object>();
    }
}

// ========================================
// REQUEST DTOs for WSL
// ========================================

