using IIM.Core.Plugins;
using IIM.Plugin.SDK;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

/// <summary>
/// API endpoints for plugin management
/// </summary>
public static class PluginEndpoints
{
    /// <summary>
    /// Map plugin-related endpoints
    /// </summary>
    public static void MapPluginEndpoints(this WebApplication app)
    {
        var plugins = app.MapGroup("/v1/plugins")
            .WithTags("Plugins")
            .WithOpenApi();
        
        // List all plugins
        plugins.MapGet("/", async (IPluginManager manager) =>
        {
            var loadedPlugins = manager.GetAllPlugins()
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    Capabilities = p.Capabilities,
                    IsLoaded = true
                });
                
            return Results.Ok(loadedPlugins);
        })
        .WithName("ListPlugins")
        .WithSummary("List all loaded plugins");
        
        // Get plugin by ID
        plugins.MapGet("/{pluginId}", (string pluginId, IPluginManager manager) =>
        {
            var plugin = manager.GetPlugin(pluginId);
            if (plugin == null)
                return Results.NotFound(new { error = $"Plugin {pluginId} not found" });
                
            return Results.Ok(new
            {
                plugin.Id,
                plugin.Name,
                plugin.Description,
                Capabilities = plugin.Capabilities
            });
        })
        .WithName("GetPlugin")
        .WithSummary("Get plugin details");
        
        // Load a plugin
        plugins.MapPost("/load", async (
            [FromBody] LoadPluginRequest request,
            IPluginManager manager,
            ILogger<Program> logger) =>
        {
            try
            {
                var success = await manager.LoadPluginAsync(request.PluginPath);
                if (success)
                {
                    return Results.Ok(new { message = "Plugin loaded successfully" });
                }
                
                return Results.BadRequest(new { error = "Failed to load plugin" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading plugin");
                return Results.Problem("An error occurred loading the plugin");
            }
        })
        .WithName("LoadPlugin")
        .WithSummary("Load a plugin from file");
        
        // Unload a plugin
        plugins.MapPost("/{pluginId}/unload", async (
            string pluginId,
            IPluginManager manager) =>
        {
            var success = await manager.UnloadPluginAsync(pluginId);
            if (success)
            {
                return Results.Ok(new { message = $"Plugin {pluginId} unloaded" });
            }
            
            return Results.BadRequest(new { error = $"Failed to unload plugin {pluginId}" });
        })
        .WithName("UnloadPlugin")
        .WithSummary("Unload a plugin");
        
        // Execute plugin with intent
        plugins.MapPost("/execute", async (
            [FromBody] PluginExecuteRequest request,
            IPluginOrchestrator orchestrator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await orchestrator.ProcessQueryAsync(
                    request.Query,
                    request.Context);
                    
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing plugin");
                return Results.Problem("An error occurred executing the plugin");
            }
        })
        .WithName("ExecutePlugin")
        .WithSummary("Execute plugins based on query intent");
        
        // Direct plugin execution
        plugins.MapPost("/{pluginId}/execute", async (
            string pluginId,
            [FromBody] DirectPluginRequest request,
            IPluginManager manager,
            ILogger<Program> logger) =>
        {
            try
            {
                var plugin = manager.GetPlugin(pluginId);
                if (plugin == null)
                    return Results.NotFound(new { error = $"Plugin {pluginId} not found" });
                
                var pluginRequest = new PluginRequest
                {
                    Intent = request.Intent,
                    Parameters = request.Parameters ?? new(),
                    CaseId = request.CaseId,
                    UserId = request.UserId
                };
                
                var result = await plugin.ExecuteAsync(pluginRequest);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing plugin {PluginId}", pluginId);
                return Results.Problem($"An error occurred executing plugin {pluginId}");
            }
        })
        .WithName("ExecutePluginDirect")
        .WithSummary("Execute a specific plugin directly");
    }
}

/// <summary>
/// Request to load a plugin
/// </summary>
public record LoadPluginRequest(string PluginPath);

/// <summary>
/// Request to execute plugins based on query
/// </summary>
public record PluginExecuteRequest(
    string Query,
    Dictionary<string, object>? Context = null);

/// <summary>
/// Request for direct plugin execution
/// </summary>
public record DirectPluginRequest(
    string Intent,
    Dictionary<string, object>? Parameters = null,
    string? CaseId = null,
    string? UserId = null);
