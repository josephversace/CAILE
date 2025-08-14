namespace IIM.Plugin.SDK;

/// <summary>
/// Attribute to provide additional metadata about a plugin
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PluginMetadataAttribute : Attribute
{
    /// <summary>
    /// Plugin category (e.g., "forensics", "osint", "analysis")
    /// </summary>
    public string Category { get; set; } = "general";
    
    /// <summary>
    /// Tags for discovery and categorization
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Path to plugin icon
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Plugin author
    /// </summary>
    public string? Author { get; set; }
    
    /// <summary>
    /// Plugin version
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Minimum IIM version required
    /// </summary>
    public string? MinimumIIMVersion { get; set; }
}
