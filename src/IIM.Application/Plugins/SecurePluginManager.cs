using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Plugins.Security;
using IIM.Plugin.SDK;
using Microsoft.Extensions.Logging;


namespace IIM.Core.Plugins;

/// <summary>
/// Secure implementation of the plugin manager with sandboxing
/// </summary>
public class SecurePluginManager : IPluginManager
{
    private readonly Dictionary<string, LoadedPlugin> _plugins = new();
    private readonly IPluginValidator _validator;
    private readonly IPluginSandbox _sandbox;
    private readonly ILogger<SecurePluginManager> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    
    /// <summary>
    /// Initialize the secure plugin manager
    /// </summary>
    public SecurePluginManager(
        IPluginValidator validator,
        IPluginSandbox sandbox,
        ILogger<SecurePluginManager> logger)
    {
        _validator = validator;
        _sandbox = sandbox;
        _logger = logger;
    }
    
    /// <summary>
    /// Discover plugins in a directory
    /// </summary>
    public async Task<IEnumerable<PluginInfo>> DiscoverPluginsAsync(string directory)
    {
        var plugins = new List<PluginInfo>();
        
        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Plugin directory does not exist: {Directory}", directory);
            return plugins;
        }
        
        // Look for .iimplugin files
        var pluginFiles = Directory.GetFiles(directory, "*.iimplugin", SearchOption.AllDirectories);
        
        foreach (var file in pluginFiles)
        {
            try
            {
                var manifest = await GetPluginManifestAsync(file);
                if (manifest != null)
                {
                    plugins.Add(new PluginInfo
                    {
                        Id = manifest.Id,
                        Name = manifest.Name,
                        Version = manifest.Version,
                        Description = manifest.Description ?? string.Empty,
                        Author = manifest.Author?.Name ?? "Unknown",
                        PackagePath = file,
                        IsLoaded = _plugins.ContainsKey(manifest.Id),
                        IsEnabled = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read plugin manifest from {File}", file);
            }
        }
        
        return plugins;
    }
    
    /// <summary>
    /// Load a plugin with validation and sandboxing
    /// </summary>
    public async Task<bool> LoadPluginAsync(string pluginPath)
    {
        await _loadLock.WaitAsync();
        try
        {
            _logger.LogInformation("Loading plugin from {Path}", pluginPath);
            
            // Validate the plugin
            var validation = await _validator.ValidateAsync(pluginPath);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Plugin validation failed: {Reasons}", 
                    string.Join(", ", validation.Errors));
                return false;
            }
            
            // Extract plugin to temp directory
            var tempDir = Path.Combine(Path.GetTempPath(), "iim-plugins", Guid.NewGuid().ToString());
            await ExtractPluginAsync(pluginPath, tempDir);
            
            // Load manifest
            var manifest = await LoadManifestAsync(Path.Combine(tempDir, "plugin.json"));
            
            // Check if already loaded
            if (_plugins.ContainsKey(manifest.Id))
            {
                _logger.LogWarning("Plugin {Id} is already loaded", manifest.Id);
                return false;
            }
            
            // Check permissions
            if (manifest.Permissions != null && 
                !await CheckPermissionsAsync(manifest.Permissions.RequiredAPIs.ToArray()))
            {
                _logger.LogWarning("Plugin {Id} requires excessive permissions", manifest.Id);
                return false;
            }
            
            // Create sandboxed context
            var context = await _sandbox.CreateContextAsync(manifest);
            
            // Load the plugin assembly
            var plugin = await LoadPluginAssemblyAsync(tempDir, manifest, context);
            
            if (plugin == null)
            {
                _logger.LogError("Failed to instantiate plugin {Id}", manifest.Id);
                return false;
            }
            
            // Initialize the plugin
            await plugin.InitializeAsync(context);
            
            // Validate the plugin can run
            if (!await plugin.ValidateAsync())
            {
                _logger.LogError("Plugin {Id} validation failed", manifest.Id);
                return false;
            }
            
            // Register the plugin
            _plugins[manifest.Id] = new LoadedPlugin
            {
                Plugin = plugin,
                Manifest = manifest,
                Context = context,
                TempDirectory = tempDir,
                LoadedAt = DateTime.UtcNow
            };
            
            _logger.LogInformation("Successfully loaded plugin {Id} v{Version}", 
                manifest.Id, manifest.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from {Path}", pluginPath);
            return false;
        }
        finally
        {
            _loadLock.Release();
        }
    }
    
    /// <summary>
    /// Unload a plugin and cleanup resources
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string pluginId)
    {
        await _loadLock.WaitAsync();
        try
        {
            if (!_plugins.TryGetValue(pluginId, out var loaded))
            {
                _logger.LogWarning("Plugin {Id} is not loaded", pluginId);
                return false;
            }
            
            _logger.LogInformation("Unloading plugin {Id}", pluginId);
            
            // Dispose the plugin
            await loaded.Plugin.DisposeAsync();
            
            // Cleanup context
            await loaded.Context.DisposeAsync();
            
            // Remove temp directory
            if (Directory.Exists(loaded.TempDirectory))
            {
                Directory.Delete(loaded.TempDirectory, true);
            }
            
            // Remove from registry
            _plugins.Remove(pluginId);
            
            // Force garbage collection to unload assembly
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            _logger.LogInformation("Successfully unloaded plugin {Id}", pluginId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin {Id}", pluginId);
            return false;
        }
        finally
        {
            _loadLock.Release();
        }
    }
    
    /// <summary>
    /// Get a loaded plugin by ID
    /// </summary>
    public IInvestigationPlugin? GetPlugin(string pluginId)
    {
        return _plugins.TryGetValue(pluginId, out var loaded) ? loaded.Plugin : null;
    }
    
    /// <summary>
    /// Get plugins that support a specific intent
    /// </summary>
    public IEnumerable<IInvestigationPlugin> GetPluginsByIntent(string intent)
    {
        return _plugins.Values
            .Where(p => p.Plugin.Capabilities.SupportedIntents.Contains(intent))
            .Select(p => p.Plugin);
    }
    
    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    public IEnumerable<IInvestigationPlugin> GetAllPlugins()
    {
        return _plugins.Values.Select(p => p.Plugin);
    }
    
    /// <summary>
    /// Read plugin manifest without loading
    /// </summary>
    public async Task<PluginManifest?> GetPluginManifestAsync(string pluginPath)
    {
        try
        {
            // Extract just the manifest from the package
            using var archive = System.IO.Compression.ZipFile.OpenRead(pluginPath);
            var manifestEntry = archive.GetEntry("plugin.json");
            
            if (manifestEntry == null)
            {
                _logger.LogWarning("No plugin.json found in {Path}", pluginPath);
                return null;
            }
            
            using var stream = manifestEntry.Open();
            return await System.Text.Json.JsonSerializer.DeserializeAsync<PluginManifest>(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read manifest from {Path}", pluginPath);
            return null;
        }
    }
    
    /// <summary>
    /// Extract plugin package to directory
    /// </summary>
    private async Task ExtractPluginAsync(string pluginPath, string targetDir)
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(targetDir);
            System.IO.Compression.ZipFile.ExtractToDirectory(pluginPath, targetDir);
        });
    }
    
    /// <summary>
    /// Load manifest from extracted plugin
    /// </summary>
    private async Task<PluginManifest> LoadManifestAsync(string manifestPath)
    {
        var json = await File.ReadAllTextAsync(manifestPath);
        return System.Text.Json.JsonSerializer.Deserialize<PluginManifest>(json)
            ?? throw new InvalidOperationException("Invalid plugin manifest");
    }
    
    /// <summary>
    /// Check if requested permissions are acceptable
    /// </summary>
    private Task<bool> CheckPermissionsAsync(string[] permissions)
    {
        // Reject plugins requesting dangerous permissions
        var dangerous = new[] { "system.admin", "kernel.access", "security.bypass" };
        
        if (permissions.Any(p => dangerous.Contains(p)))
        {
            return Task.FromResult(false);
        }
        
        return Task.FromResult(true);
    }
    
    /// <summary>
    /// Load plugin assembly and instantiate
    /// </summary>
    private async Task<IInvestigationPlugin?> LoadPluginAssemblyAsync(
        string directory, 
        PluginManifest manifest,
        PluginContext context)
    {
        return await Task.Run(() =>
        {
            // Find the main assembly
            var assemblyFile = Directory.GetFiles(directory, "*.dll")
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) != "IIM.Plugin.SDK");
                
            if (assemblyFile == null)
            {
                _logger.LogError("No plugin assembly found in {Directory}", directory);
                return null;
            }
            
            // Create isolated load context
            var loadContext = new PluginLoadContext(directory);
            
            // Load the assembly
            var assembly = loadContext.LoadFromAssemblyPath(assemblyFile);
            
            // Find plugin implementation
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IInvestigationPlugin).IsAssignableFrom(t) && 
                                   !t.IsAbstract && 
                                   !t.IsInterface);
                                   
            if (pluginType == null)
            {
                _logger.LogError("No IInvestigationPlugin implementation found in {Assembly}", 
                    assembly.FullName);
                return null;
            }
            
            // Create instance
            return Activator.CreateInstance(pluginType) as IInvestigationPlugin;
        });
    }
    
    /// <summary>
    /// Internal class to track loaded plugins
    /// </summary>
    private class LoadedPlugin
    {
        public required IInvestigationPlugin Plugin { get; init; }
        public required PluginManifest Manifest { get; init; }
        public required PluginContext Context { get; init; }
        public required string TempDirectory { get; init; }
        public required DateTime LoadedAt { get; init; }
    }
}

/// <summary>
/// Custom assembly load context for plugin isolation
/// </summary>
internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    
    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }
    
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }
}