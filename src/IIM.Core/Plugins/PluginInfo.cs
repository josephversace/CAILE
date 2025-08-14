using System;

namespace IIM.Core.Plugins;

/// <summary>
/// Information about a discovered plugin
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// Unique plugin identifier
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// Plugin display name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Plugin version
    /// </summary>
    public required string Version { get; init; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// Plugin author information
    /// </summary>
    public required PluginAuthor Author { get; init; }
    
    /// <summary>
    /// Path to the plugin package
    /// </summary>
    public required string PackagePath { get; init; }
    
    /// <summary>
    /// Whether the plugin is currently loaded
    /// </summary>
    public bool IsLoaded { get; set; }
    
    /// <summary>
    /// Whether the plugin is enabled
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Plugin load timestamp
    /// </summary>
    public DateTime? LoadedAt { get; set; }
}
