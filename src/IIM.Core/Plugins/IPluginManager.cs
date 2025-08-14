using IIM.Plugin.SDK;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IIM.Core.Plugins;

/// <summary>
/// Manages the lifecycle of investigation plugins
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Discover available plugins in a directory
    /// </summary>
    Task<IEnumerable<PluginInfo>> DiscoverPluginsAsync(string directory);
    
    /// <summary>
    /// Load a plugin from a package file
    /// </summary>
    Task<bool> LoadPluginAsync(string pluginPath);
    
    /// <summary>
    /// Unload a plugin and free its resources
    /// </summary>
    Task<bool> UnloadPluginAsync(string pluginId);
    
    /// <summary>
    /// Get a loaded plugin by ID
    /// </summary>
    IInvestigationPlugin? GetPlugin(string pluginId);
    
    /// <summary>
    /// Get all plugins that support a specific intent
    /// </summary>
    IEnumerable<IInvestigationPlugin> GetPluginsByIntent(string intent);
    
    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    IEnumerable<IInvestigationPlugin> GetAllPlugins();
    
    /// <summary>
    /// Get plugin metadata without loading
    /// </summary>
    Task<PluginManifest?> GetPluginManifestAsync(string pluginPath);
}
