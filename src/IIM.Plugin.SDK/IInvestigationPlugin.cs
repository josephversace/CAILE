using Microsoft.Extensions.Logging;

namespace IIM.Plugin.SDK;

/// <summary>
/// Core interface that all IIM investigation plugins must implement
/// </summary>
public interface IInvestigationPlugin
{
    /// <summary>
    /// Unique identifier for the plugin (e.g., "com.example.forensics.hash")
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Human-readable name of the plugin
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Detailed description of plugin functionality
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Plugin capabilities and requirements
    /// </summary>
    PluginCapabilities Capabilities { get; }
    
    /// <summary>
    /// Initialize the plugin with the provided context
    /// </summary>
    /// <param name="context">Secure context with access to IIM services</param>
    Task InitializeAsync(PluginContext context);
    
    /// <summary>
    /// Execute the plugin with the given request
    /// </summary>
    /// <param name="request">Request containing intent, parameters, and context</param>
    /// <param name="ct">Cancellation token for long-running operations</param>
    /// <returns>Result containing data, citations, and metadata</returns>
    Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Validate that the plugin can run in the current environment
    /// </summary>
    /// <returns>True if all dependencies and requirements are met</returns>
    Task<bool> ValidateAsync();
    
    /// <summary>
    /// Clean up any resources before plugin unload
    /// </summary>
    Task DisposeAsync();
}
