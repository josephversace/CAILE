using System.Collections.Generic;
using System.Threading.Tasks;
using IIM.Plugin.SDK;

using IIM.Core.Plugins.Security; // Add this for PluginManifest

namespace IIM.Core.Plugins;

/// <summary>
/// Interface for managing investigation plugins
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Discover available plugins in a directory
    /// </summary>
    /// <param name="directory">Directory to search for plugins</param>
    /// <returns>List of discovered plugin information</returns>
    Task<IEnumerable<PluginInfo>> DiscoverPluginsAsync(string directory);
    
    /// <summary>
    /// Load a plugin from a package file
    /// </summary>
    /// <param name="pluginPath">Path to the plugin package</param>
    /// <returns>True if loaded successfully</returns>
    Task<bool> LoadPluginAsync(string pluginPath);
    
    /// <summary>
    /// Unload a plugin and cleanup resources
    /// </summary>
    /// <param name="pluginId">ID of the plugin to unload</param>
    /// <returns>True if unloaded successfully</returns>
    Task<bool> UnloadPluginAsync(string pluginId);
    
    /// <summary>
    /// Get a loaded plugin by ID
    /// </summary>
    /// <param name="pluginId">Plugin ID</param>
    /// <returns>The plugin instance or null</returns>
    IInvestigationPlugin? GetPlugin(string pluginId);
    
    /// <summary>
    /// Get plugins that support a specific intent
    /// </summary>
    /// <param name="intent">The intent to search for</param>
    /// <returns>List of plugins supporting the intent</returns>
    IEnumerable<IInvestigationPlugin> GetPluginsByIntent(string intent);
    
    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    /// <returns>All loaded plugin instances</returns>
    IEnumerable<IInvestigationPlugin> GetAllPlugins();
    
    /// <summary>
    /// Get plugin manifest without loading the plugin
    /// </summary>
    /// <param name="pluginPath">Path to plugin package</param>
    /// <returns>Plugin manifest or null</returns>
    Task<PluginManifest?> GetPluginManifestAsync(string pluginPath);
}