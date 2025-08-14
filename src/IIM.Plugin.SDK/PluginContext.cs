using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace IIM.Plugin.SDK;

/// <summary>
/// Secure context provided to plugins with access to IIM services
/// </summary>
public class PluginContext
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
    /// Secure file system access with permission checks
    /// </summary>
    public required ISecureFileSystem FileSystem { get; init; }
    
    /// <summary>
    /// HTTP client with rate limiting and domain restrictions
    /// </summary>
    public required ISecureHttpClient HttpClient { get; init; }
    
    /// <summary>
    /// Process runner for executing whitelisted tools
    /// </summary>
    public required ISecureProcessRunner ProcessRunner { get; init; }
    
    /// <summary>
    /// Evidence store for saving investigation data
    /// </summary>
    public required IEvidenceStore EvidenceStore { get; init; }
    
    /// <summary>
    /// Plugin-specific temporary directory
    /// </summary>
    public required string TempDirectory { get; init; }
    
    /// <summary>
    /// Current plugin instance information
    /// </summary>
    public required PluginInfo PluginInfo { get; init; }
}
