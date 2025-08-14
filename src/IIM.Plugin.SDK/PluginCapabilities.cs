namespace IIM.Plugin.SDK;

/// <summary>
/// Defines what a plugin can do and what it requires
/// </summary>
public class PluginCapabilities
{
    /// <summary>
    /// Whether the plugin requires internet connectivity
    /// </summary>
    public bool RequiresInternet { get; set; }
    
    /// <summary>
    /// Whether the plugin requires elevated privileges
    /// </summary>
    public bool RequiresElevation { get; set; }
    
    /// <summary>
    /// Whether the plugin requires GPU acceleration
    /// </summary>
    public bool RequiresGpu { get; set; }
    
    /// <summary>
    /// List of permissions required (e.g., "filesystem.read", "network.api")
    /// </summary>
    public string[] RequiredPermissions { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// List of intents this plugin can handle (e.g., "analyze_hash", "lookup_email")
    /// </summary>
    public string[] SupportedIntents { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Whether the plugin supports asynchronous execution
    /// </summary>
    public bool SupportsAsync { get; set; } = true;
    
    /// <summary>
    /// Default timeout for plugin execution
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Maximum memory the plugin is allowed to use
    /// </summary>
    public long MaxMemoryBytes { get; set; } = 1024 * 1024 * 1024; // 1GB default
    
    /// <summary>
    /// File types this plugin can process (e.g., "*.exe", "*.jpg")
    /// </summary>
    public string[] SupportedFileTypes { get; set; } = Array.Empty<string>();
}
