using IIM.Infrastructure.Platform;
using IIM.Shared.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Api.Endpoints;

public static class WslEndpoints
{
    public static void MapWslEndpoints(this IEndpointRouteBuilder app)
    {
        var wsl = app.MapGroup("/api/wsl");
        
        // Get WSL status
        wsl.MapGet("/status", async (IWslManager wslManager) =>
        {
            var status = await wslManager.GetStatusAsync();
            
            var response = new WslStatusResponse(
                IsRunning: status.IsRunning,
                IsInstalled: status.IsInstalled,
                Distribution: status.Distribution,
                Version: status.Version,
                WslVersion: status.WslVersion,
                MemoryUsage: status.MemoryUsage,
                DiskUsage: status.DiskUsage,
                Features: status.Features,
                RunningServices: status.RunningServices,
                DefaultUser: status.DefaultUser,
                MountPath: status.MountPath
            );
            
            return Results.Ok(response);
        })
        .WithName("GetWslStatus")
        .WithOpenApi()
        .Produces<WslStatusResponse>(200);
        
        // WSL health check
        wsl.MapGet("/health", async (IWslManager wslManager) =>
        {
            var health = await wslManager.HealthCheckAsync();
            
            if (health.IsHealthy)
            {
                return Results.Ok(health);
            }
            
            return Results.Problem(new ErrorResponse(
                ErrorCode: "WSL_UNHEALTHY",
                Message: "WSL is not healthy",
                Details: string.Join("; ", health.Issues)
            ));
        })
        .WithName("GetWslHealth")
        .WithOpenApi()
        .Produces<WslHealthResponse>(200)
        .Produces<ErrorResponse>(503);
        
        // Ensure WSL distro is set up
        wsl.MapPost("/ensure", async (IWslManager wslManager) =>
        {
            try
            {
                var distro = await wslManager.EnsureDistroAsync();
                
                var response = new ServiceOperationResponse(
                    Success: true,
                    Message: $"WSL distro {distro.Name} is ready",
                    ServiceName: distro.Name,
                    Status: "Running"
                );
                
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem(new ErrorResponse(
                    ErrorCode: "WSL_SETUP_FAILED",
                    Message: "WSL distro setup failed",
                    Details: ex.Message
                ));
            }
        })
        .WithName("EnsureWslDistro")
        .WithOpenApi()
        .Produces<ServiceOperationResponse>(200)
        .Produces<ErrorResponse>(500);
        
        // WSL service management
        var services = wsl.MapGroup("/services");
        
        // Get all services status
        services.MapGet("/", async (IWslServiceOrchestrator orchestrator) =>
        {
            var services = await orchestrator.GetAllServicesStatusAsync();
            return Results.Ok(services);
        })
        .WithName("GetAllServices")
        .WithOpenApi()
        .Produces<ServiceStatusListResponse>(200);
        
        // Start a service
        services.MapPost("/{name}/start", async (
            string name,
            IWslServiceOrchestrator orchestrator) =>
        {
            var result = await orchestrator.StartServiceAsync(name);
            
            if (result)
            {
                return Results.Ok(new ServiceOperationResponse(
                    Success: true,
                    Message: $"Service {name} started",
                    ServiceName: name,
                    Status: "Running"
                ));
            }
            
            return Results.Problem(new ErrorResponse(
                ErrorCode: "SERVICE_START_FAILED",
                Message: $"Failed to start service {name}"
            ));
        })
        .WithName("StartService")
        .WithOpenApi()
        .Produces<ServiceOperationResponse>(200)
        .Produces<ErrorResponse>(500);
        
        // Stop a service
        services.MapPost("/{name}/stop", async (
            string name,
            IWslServiceOrchestrator orchestrator) =>
        {
            var result = await orchestrator.StopServiceAsync(name);
            
            if (result)
            {
                return Results.Ok(new ServiceOperationResponse(
                    Success: true,
                    Message: $"Service {name} stopped",
                    ServiceName: name,
                    Status: "Stopped"
                ));
            }
            
            return Results.Problem(new ErrorResponse(
                ErrorCode: "SERVICE_STOP_FAILED",
                Message: $"Failed to stop service {name}"
            ));
        })
        .WithName("StopService")
        .WithOpenApi()
        .Produces<ServiceOperationResponse>(200)
        .Produces<ErrorResponse>(500);
        
        // Restart a service
        services.MapPost("/{name}/restart", async (
            string name,
            IWslServiceOrchestrator orchestrator) =>
        {
            var stopResult = await orchestrator.StopServiceAsync(name);
            if (!stopResult)
            {
                return Results.Problem(new ErrorResponse(
                    ErrorCode: "SERVICE_RESTART_FAILED",
                    Message: $"Failed to stop service {name} for restart"
                ));
            }
            
            await Task.Delay(1000); // Brief pause between stop and start
            
            var startResult = await orchestrator.StartServiceAsync(name);
            if (startResult)
            {
                return Results.Ok(new ServiceOperationResponse(
                    Success: true,
                    Message: $"Service {name} restarted",
                    ServiceName: name,
                    Status: "Running"
                ));
            }
            
            return Results.Problem(new ErrorResponse(
                ErrorCode: "SERVICE_RESTART_FAILED",
                Message: $"Failed to restart service {name}"
            ));
        })
        .WithName("RestartService")
        .WithOpenApi()
        .Produces<ServiceOperationResponse>(200)
        .Produces<ErrorResponse>(500);
    }
}