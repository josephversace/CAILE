using IIM.Plugin.SDK;
using IIM.Core.Inference;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace IIM.Core.Plugins;

/// <summary>
/// Orchestrates plugin execution based on user intents
/// </summary>
public interface IPluginOrchestrator
{
    /// <summary>
    /// Process a user query and execute appropriate plugins
    /// </summary>
    Task<OrchestratorResult> ProcessQueryAsync(
        string query, 
        Dictionary<string, object>? context = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Execute a specific plugin directly
    /// </summary>
    Task<PluginResult> ExecutePluginAsync(
        string pluginId,
        PluginRequest request,
        CancellationToken ct = default);
}

/// <summary>
/// Result from orchestrator execution
/// </summary>
public class OrchestratorResult
{
    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Extracted intent from query
    /// </summary>
    public string? Intent { get; set; }
    
    /// <summary>
    /// Plugins that were executed
    /// </summary>
    public List<string> ExecutedPlugins { get; set; } = new();
    
    /// <summary>
    /// Combined results from all plugins
    /// </summary>
    public List<PluginResult> Results { get; set; } = new();
    
    /// <summary>
    /// Synthesized answer combining all results
    /// </summary>
    public string? SynthesizedAnswer { get; set; }
    
    /// <summary>
    /// Suggested follow-up actions
    /// </summary>
    public List<string> SuggestedActions { get; set; } = new();
}
