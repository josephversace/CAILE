using System;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.DTOs;
using IIM.Plugin.SDK.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IIM.Plugin.SDK;

/// <summary>
/// Secure context provided to plugins with access to IIM services
/// </summary>
public class PluginContext : IAsyncDisposable
{
    /// <summary>
    /// Logger scoped to the plugin
    /// </summary>
    public required ILogger Logger { get; init; }
    
    /// <summary>
    /// Configuration specific to this plugin
    /// </summary>
    public required IConfiguration Configuration { get; init; }
    
    /// <summary>
    /// Secure file system access
    /// </summary>
    public required ISecureFileSystem FileSystem { get; init; }
    
    /// <summary>
    /// HTTP client with rate limiting
    /// </summary>
    public required IIM.Shared.Interfaces.ISecureHttpClient HttpClient { get; init; }
    
    /// <summary>
    /// Process runner with sandboxing
    /// </summary>
    public required ISecureProcessRunner ProcessRunner { get; init; }
    
    /// <summary>
    /// Evidence store for chain of custody
    /// </summary>
    public required IEvidenceStore EvidenceStore { get; init; }
    
    /// <summary>
    /// Information about the current plugin
    /// </summary>
    public required PluginInfo PluginInfo { get; init; }

    /// <summary>
    /// Clean up resources
    /// </summary>
    public ValueTask DisposeAsync()
    {
        // Clean up any resources if needed
        Logger.LogInformation("Disposing plugin context for {PluginId}", PluginInfo.Id);
        return ValueTask.CompletedTask;
    }
}


