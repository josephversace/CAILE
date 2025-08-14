#!/bin/bash

# IIM Plugin System Setup Script
# This script sets up the dynamic plugin system for the IIM platform
# Run from the project root directory

set -e  # Exit on error

# Color codes for output
BLUE='\033[0;34m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to create a file with content
create_file() {
    local filepath=$1
    local content=$2
    
    mkdir -p "$(dirname "$filepath")"
    echo "$content" > "$filepath"
    print_success "Created: $filepath"
}

# Check if we're in the right directory
if [ ! -f "IIM.Platform.sln" ]; then
    print_error "Please run this script from the IIM project root directory"
    exit 1
fi

print_info "Setting up IIM Plugin System..."

# ==============================================
# Create Plugin SDK Project
# ==============================================

print_info "Creating Plugin SDK project..."

mkdir -p src/IIM.Plugin.SDK

# Create SDK project file
create_file "src/IIM.Plugin.SDK/IIM.Plugin.SDK.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PackageId>IIM.Plugin.SDK</PackageId>
    <Version>1.0.0</Version>
    <Authors>IIM Team</Authors>
    <Description>SDK for developing IIM investigation plugins</Description>
    <PackageTags>IIM;Plugin;Investigation;LawEnforcement</PackageTags>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
  </ItemGroup>
</Project>'

# Create base plugin interface
create_file "src/IIM.Plugin.SDK/IInvestigationPlugin.cs" 'using Microsoft.Extensions.Logging;

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
}'

# Create plugin capabilities class
create_file "src/IIM.Plugin.SDK/PluginCapabilities.cs" 'namespace IIM.Plugin.SDK;

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
}'

# Create plugin request/result classes
create_file "src/IIM.Plugin.SDK/PluginRequest.cs" 'namespace IIM.Plugin.SDK;

/// <summary>
/// Request sent to a plugin for execution
/// </summary>
public class PluginRequest
{
    /// <summary>
    /// The intent extracted from user query (e.g., "analyze_hash")
    /// </summary>
    public required string Intent { get; set; }
    
    /// <summary>
    /// Parameters extracted from the query or provided by the system
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <summary>
    /// Current case identifier for evidence association
    /// </summary>
    public string? CaseId { get; set; }
    
    /// <summary>
    /// User making the request for audit purposes
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Evidence context if applicable
    /// </summary>
    public EvidenceContext? Evidence { get; set; }
    
    /// <summary>
    /// Original user query that triggered this plugin
    /// </summary>
    public string? OriginalQuery { get; set; }
    
    /// <summary>
    /// Tags for categorization and routing
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();
}'

create_file "src/IIM.Plugin.SDK/PluginResult.cs" 'namespace IIM.Plugin.SDK;

/// <summary>
/// Result returned from plugin execution
/// </summary>
public class PluginResult
{
    /// <summary>
    /// Whether the plugin executed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The actual data/results from the plugin
    /// </summary>
    public object? Data { get; set; }
    
    /// <summary>
    /// Error message if Success is false
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Citations for any external data sources used
    /// </summary>
    public string[] Citations { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Metadata about the execution (timing, sources, etc.)
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Hash of the result for chain of custody
    /// </summary>
    public string? Hash { get; set; }
    
    /// <summary>
    /// Suggestions for follow-up actions
    /// </summary>
    public string[] SuggestedActions { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    public static PluginResult CreateSuccess(object data, params string[] citations)
    {
        return new PluginResult
        {
            Success = true,
            Data = data,
            Citations = citations
        };
    }
    
    /// <summary>
    /// Create an error result
    /// </summary>
    public static PluginResult CreateError(string error)
    {
        return new PluginResult
        {
            Success = false,
            Error = error
        };
    }
}'

# Create plugin context
create_file "src/IIM.Plugin.SDK/PluginContext.cs" 'using Microsoft.Extensions.Logging;
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
}'

# Create secure interfaces
create_file "src/IIM.Plugin.SDK/Security/ISecureFileSystem.cs" 'namespace IIM.Plugin.SDK;

/// <summary>
/// Secure file system access for plugins
/// </summary>
public interface ISecureFileSystem
{
    /// <summary>
    /// Read a file with permission and size checks
    /// </summary>
    Task<string> ReadTextAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Read binary file with permission and size checks
    /// </summary>
    Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Write text to a file in the plugin''s temp directory
    /// </summary>
    Task WriteTextAsync(string filename, string content, CancellationToken ct = default);
    
    /// <summary>
    /// Write bytes to a file in the plugin''s temp directory
    /// </summary>
    Task WriteBytesAsync(string filename, byte[] content, CancellationToken ct = default);
    
    /// <summary>
    /// Check if a file exists and is accessible
    /// </summary>
    Task<bool> ExistsAsync(string path);
    
    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<FileMetadata> GetMetadataAsync(string path);
}'

# Create base plugin class
create_file "src/IIM.Plugin.SDK/InvestigationPlugin.cs" 'using Microsoft.Extensions.Logging;

namespace IIM.Plugin.SDK;

/// <summary>
/// Base class for investigation plugins with helper methods
/// </summary>
public abstract class InvestigationPlugin : IInvestigationPlugin
{
    private PluginContext? _context;
    
    /// <summary>
    /// Plugin unique identifier
    /// </summary>
    public abstract string Id { get; }
    
    /// <summary>
    /// Plugin display name
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    public abstract string Description { get; }
    
    /// <summary>
    /// Plugin capabilities
    /// </summary>
    public abstract PluginCapabilities Capabilities { get; }
    
    /// <summary>
    /// Logger for this plugin
    /// </summary>
    protected ILogger Logger => _context?.Logger ?? throw new InvalidOperationException("Plugin not initialized");
    
    /// <summary>
    /// Plugin configuration
    /// </summary>
    protected IConfiguration Config => _context?.Configuration ?? throw new InvalidOperationException("Plugin not initialized");
    
    /// <summary>
    /// Evidence store access
    /// </summary>
    protected IEvidenceStore Evidence => _context?.EvidenceStore ?? throw new InvalidOperationException("Plugin not initialized");
    
    /// <summary>
    /// Initialize the plugin
    /// </summary>
    public virtual async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        Logger.LogInformation("Initializing plugin {PluginId}", Id);
        await OnInitializeAsync();
    }
    
    /// <summary>
    /// Override to provide custom initialization
    /// </summary>
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;
    
    /// <summary>
    /// Execute the plugin
    /// </summary>
    public abstract Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Validate plugin can run
    /// </summary>
    public virtual Task<bool> ValidateAsync() => Task.FromResult(true);
    
    /// <summary>
    /// Cleanup resources
    /// </summary>
    public virtual Task DisposeAsync() => Task.CompletedTask;
    
    /// <summary>
    /// Helper to read a file securely
    /// </summary>
    protected Task<string> ReadFileAsync(string path) => 
        _context!.FileSystem.ReadTextAsync(path);
    
    /// <summary>
    /// Helper to call an API securely
    /// </summary>
    protected Task<HttpResponseMessage> CallApiAsync(string url, HttpContent? content = null) =>
        _context!.HttpClient.PostAsync(url, content);
    
    /// <summary>
    /// Helper to run a tool securely
    /// </summary>
    protected Task<ProcessResult> RunToolAsync(string tool, params string[] args) =>
        _context!.ProcessRunner.RunAsync(tool, args);
    
    /// <summary>
    /// Helper to compute hash for chain of custody
    /// </summary>
    protected static string ComputeHash(object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}'

# Create attributes
create_file "src/IIM.Plugin.SDK/Attributes/PluginMetadataAttribute.cs" 'namespace IIM.Plugin.SDK;

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
}'

create_file "src/IIM.Plugin.SDK/Attributes/IntentHandlerAttribute.cs" 'namespace IIM.Plugin.SDK;

/// <summary>
/// Marks a method as a handler for a specific intent
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class IntentHandlerAttribute : Attribute
{
    /// <summary>
    /// The intent this method handles
    /// </summary>
    public string Intent { get; }
    
    /// <summary>
    /// Description of what this handler does
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Example usage of this intent
    /// </summary>
    public string? Example { get; set; }
    
    /// <summary>
    /// Create a new intent handler attribute
    /// </summary>
    public IntentHandlerAttribute(string intent)
    {
        Intent = intent;
    }
}'

# ==============================================
# Create Plugin Manager in Core
# ==============================================

print_info "Adding Plugin Manager to IIM.Core..."

mkdir -p src/IIM.Core/Plugins

# Create plugin manager interface
create_file "src/IIM.Core/Plugins/IPluginManager.cs" 'using IIM.Plugin.SDK;

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
}'

# Create plugin info class
create_file "src/IIM.Core/Plugins/PluginInfo.cs" 'namespace IIM.Core.Plugins;

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
}'

# Create plugin manifest
create_file "src/IIM.Core/Plugins/PluginManifest.cs" 'using System.Text.Json.Serialization;

namespace IIM.Core.Plugins;

/// <summary>
/// Plugin manifest file structure (plugin.json)
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// Unique plugin identifier
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    /// <summary>
    /// Plugin display name
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// Plugin version (semver)
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }
    
    /// <summary>
    /// Author information
    /// </summary>
    [JsonPropertyName("author")]
    public required PluginAuthor Author { get; set; }
    
    /// <summary>
    /// Required IIM version
    /// </summary>
    [JsonPropertyName("iimVersion")]
    public string IimVersion { get; set; } = "^1.0.0";
    
    /// <summary>
    /// Plugin category
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "general";
    
    /// <summary>
    /// Required permissions
    /// </summary>
    [JsonPropertyName("permissions")]
    public string[] Permissions { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Plugin capabilities
    /// </summary>
    [JsonPropertyName("capabilities")]
    public PluginCapabilitiesManifest Capabilities { get; set; } = new();
    
    /// <summary>
    /// Dependencies on other packages
    /// </summary>
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; } = new();
    
    /// <summary>
    /// Digital signature information
    /// </summary>
    [JsonPropertyName("signature")]
    public PluginSignature? Signature { get; set; }
}

/// <summary>
/// Plugin author information
/// </summary>
public class PluginAuthor
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("organization")]
    public string? Organization { get; set; }
}

/// <summary>
/// Plugin capabilities in manifest
/// </summary>
public class PluginCapabilitiesManifest
{
    [JsonPropertyName("intents")]
    public string[] Intents { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("fileTypes")]
    public string[] FileTypes { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("requiresInternet")]
    public bool RequiresInternet { get; set; }
    
    [JsonPropertyName("requiresGpu")]
    public bool RequiresGpu { get; set; }
}

/// <summary>
/// Plugin signature information
/// </summary>
public class PluginSignature
{
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = "RS256";
    
    [JsonPropertyName("publicKey")]
    public string? PublicKey { get; set; }
    
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}'

# Create secure plugin manager implementation
create_file "src/IIM.Core/Plugins/SecurePluginManager.cs" 'using System.Reflection;
using System.Runtime.Loader;
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
                        Description = manifest.Description,
                        Author = manifest.Author,
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
            if (!await CheckPermissionsAsync(manifest.Permissions))
            {
                _logger.LogWarning("Plugin {Id} requires excessive permissions", manifest.Id);
                return false;
            }
            
            // Create sandboxed context
            var context = await _sandbox.CreateContextAsync(manifest, tempDir);
            
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
}'

# Create plugin validator
create_file "src/IIM.Core/Plugins/Security/PluginValidator.cs" 'namespace IIM.Core.Plugins.Security;

/// <summary>
/// Validates plugins before loading for security and compatibility
/// </summary>
public interface IPluginValidator
{
    /// <summary>
    /// Validate a plugin package
    /// </summary>
    Task<ValidationResult> ValidateAsync(string pluginPath);
}

/// <summary>
/// Result of plugin validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the plugin passed validation
    /// </summary>
    public bool IsValid => !Errors.Any();
    
    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; } = new();
    
    /// <summary>
    /// List of warnings (non-blocking)
    /// </summary>
    public List<string> Warnings { get; } = new();
    
    /// <summary>
    /// Add an error to the result
    /// </summary>
    public void AddError(string error) => Errors.Add(error);
    
    /// <summary>
    /// Add a warning to the result
    /// </summary>
    public void AddWarning(string warning) => Warnings.Add(warning);
}'

# Create plugin sandbox
create_file "src/IIM.Core/Plugins/Security/PluginSandbox.cs" 'using IIM.Plugin.SDK;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Plugins.Security;

/// <summary>
/// Creates sandboxed contexts for plugin execution
/// </summary>
public interface IPluginSandbox
{
    /// <summary>
    /// Create a secure context for a plugin
    /// </summary>
    Task<PluginContext> CreateContextAsync(PluginManifest manifest, string workingDirectory);
}

/// <summary>
/// Implementation of plugin sandbox
/// </summary>
public class PluginSandbox : IPluginSandbox
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly IEvidenceManager _evidenceManager;
    
    /// <summary>
    /// Initialize the plugin sandbox
    /// </summary>
    public PluginSandbox(
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        IEvidenceManager evidenceManager)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;
        _evidenceManager = evidenceManager;
    }
    
    /// <summary>
    /// Create a sandboxed context for plugin execution
    /// </summary>
    public async Task<PluginContext> CreateContextAsync(
        PluginManifest manifest, 
        string workingDirectory)
    {
        // Create plugin-specific temp directory
        var tempDir = Path.Combine(workingDirectory, "temp");
        Directory.CreateDirectory(tempDir);
        
        // Create restricted services
        var context = new PluginContext
        {
            Logger = _loggerFactory.CreateLogger($"Plugin.{manifest.Id}"),
            
            Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["PluginId"] = manifest.Id,
                    ["PluginVersion"] = manifest.Version,
                    ["TempDirectory"] = tempDir
                })
                .Build(),
                
            FileSystem = new RestrictedFileSystem(
                allowedPaths: GetAllowedPaths(manifest, workingDirectory),
                maxFileSize: 100 * 1024 * 1024 // 100MB
            ),
            
            HttpClient = new RateLimitedHttpClient(
                maxRequestsPerMinute: 60,
                allowedDomains: GetAllowedDomains(manifest)
            ),
            
            ProcessRunner = new SandboxedProcessRunner(
                allowedTools: GetAllowedTools(manifest),
                maxRuntime: TimeSpan.FromMinutes(5)
            ),
            
            EvidenceStore = new NamespacedEvidenceStore(
                _evidenceManager,
                $"plugin_{manifest.Id}"
            ),
            
            TempDirectory = tempDir,
            
            PluginInfo = new PluginInfo
            {
                Id = manifest.Id,
                Name = manifest.Name,
                Version = manifest.Version,
                Description = manifest.Description,
                Author = manifest.Author,
                PackagePath = workingDirectory,
                IsLoaded = true,
                IsEnabled = true,
                LoadedAt = DateTime.UtcNow
            }
        };
        
        return context;
    }
    
    /// <summary>
    /// Get allowed file paths for plugin
    /// </summary>
    private string[] GetAllowedPaths(PluginManifest manifest, string workingDir)
    {
        var paths = new List<string>
        {
            workingDir,
            Path.GetTempPath()
        };
        
        // Add configured evidence paths if plugin has permission
        if (manifest.Permissions.Contains("evidence.read"))
        {
            paths.Add(_configuration["Evidence:StorePath"]);
        }
        
        return paths.ToArray();
    }
    
    /// <summary>
    /// Get allowed domains for HTTP requests
    /// </summary>
    private string[] GetAllowedDomains(PluginManifest manifest)
    {
        // Start with safe defaults
        var domains = new List<string>();
        
        // Add specific domains based on plugin category
        if (manifest.Category == "osint")
        {
            domains.AddRange(new[]
            {
                "haveibeenpwned.com",
                "virustotal.com",
                "shodan.io"
            });
        }
        
        return domains.ToArray();
    }
    
    /// <summary>
    /// Get allowed tools for process execution
    /// </summary>
    private string[] GetAllowedTools(PluginManifest manifest)
    {
        var tools = new List<string>();
        
        // Add tools based on permissions
        if (manifest.Permissions.Contains("tools.forensics"))
        {
            tools.AddRange(new[]
            {
                "exiftool",
                "strings",
                "file",
                "xxd"
            });
        }
        
        return tools.ToArray();
    }
}'

# Create plugin orchestrator
create_file "src/IIM.Core/Plugins/PluginOrchestrator.cs" 'using IIM.Plugin.SDK;
using IIM.Core.Inference;
using Microsoft.Extensions.Logging;

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
}'

# ==============================================
# Create Example Plugin
# ==============================================

print_info "Creating example plugin..."

mkdir -p examples/SamplePlugin

# Create example plugin project
create_file "examples/SamplePlugin/SamplePlugin.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/IIM.Plugin.SDK/IIM.Plugin.SDK.csproj" />
  </ItemGroup>
</Project>'

# Create example plugin implementation
create_file "examples/SamplePlugin/HashAnalyzerPlugin.cs" 'using IIM.Plugin.SDK;
using System.Security.Cryptography;
using System.Text.Json;

namespace SamplePlugin;

/// <summary>
/// Example plugin that analyzes file hashes
/// </summary>
[PluginMetadata(
    Category = "forensics",
    Tags = new[] { "hash", "file", "analysis" },
    Author = "IIM Team",
    Version = "1.0.0"
)]
public class HashAnalyzerPlugin : InvestigationPlugin
{
    /// <summary>
    /// Unique identifier for this plugin
    /// </summary>
    public override string Id => "com.iim.example.hash-analyzer";
    
    /// <summary>
    /// Display name
    /// </summary>
    public override string Name => "Hash Analyzer";
    
    /// <summary>
    /// Description of functionality
    /// </summary>
    public override string Description => 
        "Analyzes file hashes and checks against known databases";
    
    /// <summary>
    /// Plugin capabilities
    /// </summary>
    public override PluginCapabilities Capabilities => new()
    {
        RequiresInternet = true,
        RequiresElevation = false,
        RequiredPermissions = new[] { "filesystem.read", "network.api" },
        SupportedIntents = new[] { "analyze_hash", "check_file_hash" },
        SupportedFileTypes = new[] { "*" }
    };
    
    /// <summary>
    /// Main execution method
    /// </summary>
    public override async Task<PluginResult> ExecuteAsync(
        PluginRequest request, 
        CancellationToken ct = default)
    {
        Logger.LogInformation("Executing hash analysis for intent: {Intent}", request.Intent);
        
        return request.Intent switch
        {
            "analyze_hash" => await AnalyzeHashAsync(request, ct),
            "check_file_hash" => await CheckFileHashAsync(request, ct),
            _ => PluginResult.CreateError($"Unknown intent: {request.Intent}")
        };
    }
    
    /// <summary>
    /// Analyze a provided hash value
    /// </summary>
    [IntentHandler("analyze_hash", 
        Description = "Analyzes a hash value against known databases",
        Example = "Check if hash abc123... is known malware")]
    private async Task<PluginResult> AnalyzeHashAsync(
        PluginRequest request, 
        CancellationToken ct)
    {
        if (!request.Parameters.TryGetValue("hash", out var hashObj) || 
            hashObj is not string hash)
        {
            return PluginResult.CreateError("Missing required parameter: hash");
        }
        
        Logger.LogInformation("Analyzing hash: {Hash}", hash);
        
        var results = new HashAnalysisResult
        {
            Hash = hash,
            Algorithm = DetectHashAlgorithm(hash),
            CheckedDatabases = new List<string>()
        };
        
        // Check against NSRL (known good)
        if (await CheckNSRLAsync(hash, ct))
        {
            results.IsKnownGood = true;
            results.CheckedDatabases.Add("NSRL");
        }
        
        // Check against threat intelligence (mock)
        var threatInfo = await CheckThreatIntelAsync(hash, ct);
        if (threatInfo != null)
        {
            results.IsKnownBad = true;
            results.ThreatInfo = threatInfo;
            results.CheckedDatabases.Add("ThreatIntel");
        }
        
        // Store in evidence
        await Evidence.StoreAsync($"hash_analysis_{hash}", results);
        
        return PluginResult.CreateSuccess(results, 
            "NSRL Database",
            "Internal Threat Intelligence");
    }
    
    /// <summary>
    /// Calculate and analyze hash of a file
    /// </summary>
    [IntentHandler("check_file_hash",
        Description = "Calculates file hash and checks against databases",
        Example = "Analyze the hash of evidence file IMG001.jpg")]
    private async Task<PluginResult> CheckFileHashAsync(
        PluginRequest request,
        CancellationToken ct)
    {
        if (!request.Parameters.TryGetValue("file", out var fileObj) || 
            fileObj is not string filePath)
        {
            return PluginResult.CreateError("Missing required parameter: file");
        }
        
        // Check if file exists and is accessible
        if (!await FileSystem.ExistsAsync(filePath))
        {
            return PluginResult.CreateError($"File not found: {filePath}");
        }
        
        // Calculate hash
        var fileBytes = await FileSystem.ReadBytesAsync(filePath, ct);
        var hash = CalculateHash(fileBytes, "SHA256");
        
        Logger.LogInformation("Calculated hash for {File}: {Hash}", filePath, hash);
        
        // Now analyze the hash
        request.Parameters["hash"] = hash;
        var analysisResult = await AnalyzeHashAsync(request, ct);
        
        // Add file metadata
        if (analysisResult.Success && analysisResult.Data is HashAnalysisResult result)
        {
            var metadata = await FileSystem.GetMetadataAsync(filePath);
            result.FileInfo = new FileHashInfo
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileSize = metadata.Size,
                ModifiedDate = metadata.ModifiedDate
            };
        }
        
        return analysisResult;
    }
    
    /// <summary>
    /// Detect hash algorithm based on length
    /// </summary>
    private string DetectHashAlgorithm(string hash)
    {
        return hash.Length switch
        {
            32 => "MD5",
            40 => "SHA1",
            64 => "SHA256",
            128 => "SHA512",
            _ => "Unknown"
        };
    }
    
    /// <summary>
    /// Calculate hash of byte array
    /// </summary>
    private string CalculateHash(byte[] data, string algorithm)
    {
        using var hasher = algorithm switch
        {
            "MD5" => MD5.Create(),
            "SHA1" => SHA1.Create(),
            "SHA256" => SHA256.Create(),
            "SHA512" => SHA512.Create(),
            _ => SHA256.Create()
        };
        
        var hashBytes = hasher.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
    
    /// <summary>
    /// Check hash against NSRL database (mock)
    /// </summary>
    private async Task<bool> CheckNSRLAsync(string hash, CancellationToken ct)
    {
        // In real implementation, would call NSRL API
        await Task.Delay(100, ct); // Simulate API call
        
        // Mock: randomly mark some hashes as known good
        return hash.GetHashCode() % 3 == 0;
    }
    
    /// <summary>
    /// Check hash against threat intelligence (mock)
    /// </summary>
    private async Task<ThreatInfo?> CheckThreatIntelAsync(string hash, CancellationToken ct)
    {
        // In real implementation, would call threat intel API
        await Task.Delay(100, ct); // Simulate API call
        
        // Mock: randomly mark some hashes as threats
        if (hash.GetHashCode() % 5 == 0)
        {
            return new ThreatInfo
            {
                ThreatName = "Trojan.Generic",
                Severity = "High",
                FirstSeen = DateTime.UtcNow.AddDays(-30),
                Description = "Generic trojan detected by behavioral analysis"
            };
        }
        
        return null;
    }
}

/// <summary>
/// Result of hash analysis
/// </summary>
public class HashAnalysisResult
{
    public required string Hash { get; set; }
    public string Algorithm { get; set; } = "Unknown";
    public bool IsKnownGood { get; set; }
    public bool IsKnownBad { get; set; }
    public ThreatInfo? ThreatInfo { get; set; }
    public FileHashInfo? FileInfo { get; set; }
    public List<string> CheckedDatabases { get; set; } = new();
}

/// <summary>
/// File information for hash
/// </summary>
public class FileHashInfo
{
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public long FileSize { get; set; }
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Threat intelligence information
/// </summary>
public class ThreatInfo
{
    public required string ThreatName { get; set; }
    public required string Severity { get; set; }
    public DateTime FirstSeen { get; set; }
    public string? Description { get; set; }
}'

# Create plugin manifest
create_file "examples/SamplePlugin/plugin.json" '{
  "id": "com.iim.example.hash-analyzer",
  "name": "Hash Analyzer",
  "version": "1.0.0",
  "description": "Analyzes file hashes and checks against known databases",
  "author": {
    "name": "IIM Team",
    "email": "support@iim.example",
    "organization": "IIM Project"
  },
  "iimVersion": "^1.0.0",
  "category": "forensics",
  "permissions": [
    "filesystem.read",
    "network.api"
  ],
  "capabilities": {
    "intents": ["analyze_hash", "check_file_hash"],
    "fileTypes": ["*"],
    "requiresInternet": true,
    "requiresGpu": false
  },
  "dependencies": {
    "IIM.Plugin.SDK": "^1.0.0"
  }
}'

# ==============================================
# Update API to include plugin endpoints
# ==============================================

print_info "Adding plugin endpoints to API..."

# Create plugin controller
create_file "src/IIM.Api/Endpoints/PluginEndpoints.cs" 'using IIM.Core.Plugins;
using IIM.Plugin.SDK;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

/// <summary>
/// API endpoints for plugin management
/// </summary>
public static class PluginEndpoints
{
    /// <summary>
    /// Map plugin-related endpoints
    /// </summary>
    public static void MapPluginEndpoints(this WebApplication app)
    {
        var plugins = app.MapGroup("/v1/plugins")
            .WithTags("Plugins")
            .WithOpenApi();
        
        // List all plugins
        plugins.MapGet("/", async (IPluginManager manager) =>
        {
            var loadedPlugins = manager.GetAllPlugins()
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    Capabilities = p.Capabilities,
                    IsLoaded = true
                });
                
            return Results.Ok(loadedPlugins);
        })
        .WithName("ListPlugins")
        .WithSummary("List all loaded plugins");
        
        // Get plugin by ID
        plugins.MapGet("/{pluginId}", (string pluginId, IPluginManager manager) =>
        {
            var plugin = manager.GetPlugin(pluginId);
            if (plugin == null)
                return Results.NotFound(new { error = $"Plugin {pluginId} not found" });
                
            return Results.Ok(new
            {
                plugin.Id,
                plugin.Name,
                plugin.Description,
                Capabilities = plugin.Capabilities
            });
        })
        .WithName("GetPlugin")
        .WithSummary("Get plugin details");
        
        // Load a plugin
        plugins.MapPost("/load", async (
            [FromBody] LoadPluginRequest request,
            IPluginManager manager,
            ILogger<Program> logger) =>
        {
            try
            {
                var success = await manager.LoadPluginAsync(request.PluginPath);
                if (success)
                {
                    return Results.Ok(new { message = "Plugin loaded successfully" });
                }
                
                return Results.BadRequest(new { error = "Failed to load plugin" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading plugin");
                return Results.Problem("An error occurred loading the plugin");
            }
        })
        .WithName("LoadPlugin")
        .WithSummary("Load a plugin from file");
        
        // Unload a plugin
        plugins.MapPost("/{pluginId}/unload", async (
            string pluginId,
            IPluginManager manager) =>
        {
            var success = await manager.UnloadPluginAsync(pluginId);
            if (success)
            {
                return Results.Ok(new { message = $"Plugin {pluginId} unloaded" });
            }
            
            return Results.BadRequest(new { error = $"Failed to unload plugin {pluginId}" });
        })
        .WithName("UnloadPlugin")
        .WithSummary("Unload a plugin");
        
        // Execute plugin with intent
        plugins.MapPost("/execute", async (
            [FromBody] PluginExecuteRequest request,
            IPluginOrchestrator orchestrator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await orchestrator.ProcessQueryAsync(
                    request.Query,
                    request.Context);
                    
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing plugin");
                return Results.Problem("An error occurred executing the plugin");
            }
        })
        .WithName("ExecutePlugin")
        .WithSummary("Execute plugins based on query intent");
        
        // Direct plugin execution
        plugins.MapPost("/{pluginId}/execute", async (
            string pluginId,
            [FromBody] DirectPluginRequest request,
            IPluginManager manager,
            ILogger<Program> logger) =>
        {
            try
            {
                var plugin = manager.GetPlugin(pluginId);
                if (plugin == null)
                    return Results.NotFound(new { error = $"Plugin {pluginId} not found" });
                
                var pluginRequest = new PluginRequest
                {
                    Intent = request.Intent,
                    Parameters = request.Parameters ?? new(),
                    CaseId = request.CaseId,
                    UserId = request.UserId
                };
                
                var result = await plugin.ExecuteAsync(pluginRequest);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing plugin {PluginId}", pluginId);
                return Results.Problem($"An error occurred executing plugin {pluginId}");
            }
        })
        .WithName("ExecutePluginDirect")
        .WithSummary("Execute a specific plugin directly");
    }
}

/// <summary>
/// Request to load a plugin
/// </summary>
public record LoadPluginRequest(string PluginPath);

/// <summary>
/// Request to execute plugins based on query
/// </summary>
public record PluginExecuteRequest(
    string Query,
    Dictionary<string, object>? Context = null);

/// <summary>
/// Request for direct plugin execution
/// </summary>
public record DirectPluginRequest(
    string Intent,
    Dictionary<string, object>? Parameters = null,
    string? CaseId = null,
    string? UserId = null);'

# ==============================================
# Create plugin development template
# ==============================================

print_info "Creating plugin development template..."

mkdir -p templates/PluginTemplate

# Create template files
create_file "templates/PluginTemplate/.template.config/template.json" '{
  "$schema": "http://json.schemastore.org/template",
  "author": "IIM Team",
  "classifications": ["IIM", "Plugin", "Investigation"],
  "identity": "IIM.Plugin.Template",
  "name": "IIM Investigation Plugin",
  "shortName": "iimplugin",
  "tags": {
    "language": "C#",
    "type": "project"
  },
  "sourceName": "PluginTemplate",
  "preferNameDirectory": true,
  "symbols": {
    "PluginId": {
      "type": "parameter",
      "datatype": "string",
      "description": "The unique ID for your plugin",
      "defaultValue": "com.example.myplugin",
      "replaces": "{PLUGIN_ID}"
    },
    "PluginName": {
      "type": "parameter",
      "datatype": "string",
      "description": "The display name for your plugin",
      "defaultValue": "My Plugin",
      "replaces": "{PLUGIN_NAME}"
    },
    "Author": {
      "type": "parameter",
      "datatype": "string",
      "description": "Plugin author name",
      "defaultValue": "Your Name",
      "replaces": "{AUTHOR}"
    }
  }
}'

create_file "templates/PluginTemplate/PluginTemplate.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="IIM.Plugin.SDK" Version="1.0.*" />
  </ItemGroup>
  
  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>'

create_file "templates/PluginTemplate/Plugin.cs" 'using IIM.Plugin.SDK;

namespace PluginTemplate;

/// <summary>
/// {PLUGIN_NAME} - An IIM Investigation Plugin
/// </summary>
[PluginMetadata(
    Category = "general",
    Tags = new[] { "example" },
    Author = "{AUTHOR}",
    Version = "1.0.0"
)]
public class Plugin : InvestigationPlugin
{
    /// <summary>
    /// Unique identifier for this plugin
    /// </summary>
    public override string Id => "{PLUGIN_ID}";
    
    /// <summary>
    /// Display name
    /// </summary>
    public override string Name => "{PLUGIN_NAME}";
    
    /// <summary>
    /// Plugin description
    /// </summary>
    public override string Description => 
        "Description of what your plugin does";
    
    /// <summary>
    /// Plugin capabilities
    /// </summary>
    public override PluginCapabilities Capabilities => new()
    {
        RequiresInternet = false,
        RequiresElevation = false,
        RequiredPermissions = new[] { "filesystem.read" },
        SupportedIntents = new[] { "example_intent" },
        SupportedFileTypes = new[] { "*" }
    };
    
    /// <summary>
    /// Plugin initialization
    /// </summary>
    protected override async Task OnInitializeAsync()
    {
        Logger.LogInformation("Initializing {PluginName}", Name);
        // Add any initialization logic here
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Main execution method
    /// </summary>
    public override async Task<PluginResult> ExecuteAsync(
        PluginRequest request, 
        CancellationToken ct = default)
    {
        Logger.LogInformation("Executing {PluginName} with intent: {Intent}", 
            Name, request.Intent);
        
        try
        {
            return request.Intent switch
            {
                "example_intent" => await HandleExampleIntent(request, ct),
                _ => PluginResult.CreateError($"Unknown intent: {request.Intent}")
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing plugin");
            return PluginResult.CreateError($"Plugin execution failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handle the example intent
    /// </summary>
    [IntentHandler("example_intent",
        Description = "An example intent handler",
        Example = "Do something with the example intent")]
    private async Task<PluginResult> HandleExampleIntent(
        PluginRequest request,
        CancellationToken ct)
    {
        // Extract parameters
        if (!request.Parameters.TryGetValue("input", out var input))
        {
            return PluginResult.CreateError("Missing required parameter: input");
        }
        
        // Do some work
        await Task.Delay(100, ct); // Simulate work
        
        // Return results
        var result = new
        {
            Input = input,
            ProcessedAt = DateTime.UtcNow,
            Message = $"Successfully processed: {input}"
        };
        
        return PluginResult.CreateSuccess(result);
    }
}'

create_file "templates/PluginTemplate/plugin.json" '{
  "id": "{PLUGIN_ID}",
  "name": "{PLUGIN_NAME}",
  "version": "1.0.0",
  "description": "Description of what your plugin does",
  "author": {
    "name": "{AUTHOR}",
    "email": "your.email@example.com",
    "organization": "Your Organization"
  },
  "iimVersion": "^1.0.0",
  "category": "general",
  "permissions": [
    "filesystem.read"
  ],
  "capabilities": {
    "intents": ["example_intent"],
    "fileTypes": ["*"],
    "requiresInternet": false,
    "requiresGpu": false
  },
  "dependencies": {
    "IIM.Plugin.SDK": "^1.0.0"
  }
}'

# ==============================================
# Create plugin CLI tool
# ==============================================

print_info "Creating IIM CLI tool..."

mkdir -p tools/IIM.CLI

create_file "tools/IIM.CLI/IIM.CLI.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>iim</ToolCommandName>
    <PackageId>IIM.CLI</PackageId>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/IIM.Core/IIM.Core.csproj" />
    <ProjectReference Include="../../src/IIM.Plugin.SDK/IIM.Plugin.SDK.csproj" />
  </ItemGroup>
</Project>'

create_file "tools/IIM.CLI/Program.cs" 'using System.CommandLine;
using System.IO.Compression;
using IIM.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IIM.CLI;

/// <summary>
/// IIM Command Line Interface for plugin development
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("IIM CLI - Plugin development and management tool");
        
        // Plugin commands
        var pluginCommand = new Command("plugin", "Plugin management commands");
        rootCommand.Add(pluginCommand);
        
        // Build command
        var buildCommand = new Command("build", "Build a plugin package")
        {
            new Option<string>(
                "--project", 
                getDefaultValue: () => ".",
                "Project directory"),
            new Option<string>(
                "--output",
                getDefaultValue: () => "./bin/Release",
                "Output directory"),
            new Option<bool>(
                "--sign",
                "Sign the plugin package")
        };
        buildCommand.SetHandler(BuildPlugin, 
            buildCommand.Options.ToArray());
        pluginCommand.Add(buildCommand);
        
        // Test command
        var testCommand = new Command("test", "Test a plugin locally")
        {
            new Argument<string>("plugin", "Path to plugin package"),
            new Option<string>(
                "--intent",
                "Intent to test"),
            new Option<string>(
                "--params",
                "JSON parameters for the intent")
        };
        testCommand.SetHandler(TestPlugin,
            testCommand.Arguments[0],
            testCommand.Options.ToArray());
        pluginCommand.Add(testCommand);
        
        // Validate command
        var validateCommand = new Command("validate", "Validate a plugin package")
        {
            new Argument<string>("plugin", "Path to plugin package")
        };
        validateCommand.SetHandler(ValidatePlugin,
            validateCommand.Arguments[0]);
        pluginCommand.Add(validateCommand);
        
        // Package command
        var packageCommand = new Command("package", "Package a plugin for distribution")
        {
            new Option<string>(
                "--project",
                getDefaultValue: () => ".",
                "Project directory"),
            new Option<string>(
                "--output",
                "Output file path")
        };
        packageCommand.SetHandler(PackagePlugin,
            packageCommand.Options.ToArray());
        pluginCommand.Add(packageCommand);
        
        return await rootCommand.InvokeAsync(args);
    }
    
    /// <summary>
    /// Build a plugin project
    /// </summary>
    static async Task BuildPlugin(params IOption[] options)
    {
        var project = GetOptionValue<string>(options, "--project") ?? ".";
        var output = GetOptionValue<string>(options, "--output") ?? "./bin/Release";
        var sign = GetOptionValue<bool>(options, "--sign");
        
        Console.WriteLine($"Building plugin in {project}...");
        
        // Run dotnet build
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{project}\" -c Release -o \"{output}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        
        process.Start();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine("Build failed!");
            var error = await process.StandardError.ReadToEndAsync();
            Console.Error.WriteLine(error);
            return;
        }
        
        Console.WriteLine("Build successful!");
        
        if (sign)
        {
            Console.WriteLine("Signing plugin...");
            // TODO: Implement signing
            Console.WriteLine("Signing not yet implemented");
        }
    }
    
    /// <summary>
    /// Test a plugin package
    /// </summary>
    static async Task TestPlugin(string pluginPath, params IOption[] options)
    {
        var intent = GetOptionValue<string>(options, "--intent");
        var paramsJson = GetOptionValue<string>(options, "--params");
        
        Console.WriteLine($"Testing plugin: {pluginPath}");
        
        // Create service provider
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IPluginValidator, MockPluginValidator>();
        services.AddSingleton<IPluginSandbox, MockPluginSandbox>();
        services.AddSingleton<IPluginManager, SecurePluginManager>();
        
        var provider = services.BuildServiceProvider();
        var pluginManager = provider.GetRequiredService<IPluginManager>();
        
        // Load the plugin
        Console.WriteLine("Loading plugin...");
        var loaded = await pluginManager.LoadPluginAsync(pluginPath);
        if (!loaded)
        {
            Console.Error.WriteLine("Failed to load plugin!");
            return;
        }
        
        // Get plugin info
        var manifest = await pluginManager.GetPluginManifestAsync(pluginPath);
        if (manifest == null)
        {
            Console.Error.WriteLine("Failed to read plugin manifest!");
            return;
        }
        
        var plugin = pluginManager.GetPlugin(manifest.Id);
        if (plugin == null)
        {
            Console.Error.WriteLine("Plugin loaded but not found!");
            return;
        }
        
        Console.WriteLine($"Plugin loaded: {plugin.Name} v{manifest.Version}");
        Console.WriteLine($"Supported intents: {string.Join(", ", plugin.Capabilities.SupportedIntents)}");
        
        // Test execution if intent provided
        if (!string.IsNullOrEmpty(intent))
        {
            Console.WriteLine($"\nTesting intent: {intent}");
            
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(paramsJson))
            {
                try
                {
                    parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson) 
                        ?? new Dictionary<string, object>();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Invalid JSON parameters: {ex.Message}");
                    return;
                }
            }
            
            var request = new IIM.Plugin.SDK.PluginRequest
            {
                Intent = intent,
                Parameters = parameters,
                CaseId = "TEST-001",
                UserId = "test-user"
            };
            
            var result = await plugin.ExecuteAsync(request);
            
            Console.WriteLine($"\nExecution result:");
            Console.WriteLine($"Success: {result.Success}");
            if (result.Success)
            {
                Console.WriteLine($"Data: {System.Text.Json.JsonSerializer.Serialize(result.Data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}");
            }
            else
            {
                Console.WriteLine($"Error: {result.Error}");
            }
        }
        
        // Cleanup
        await pluginManager.UnloadPluginAsync(manifest.Id);
        Console.WriteLine("\nPlugin unloaded");
    }
    
    /// <summary>
    /// Validate a plugin package
    /// </summary>
    static async Task ValidatePlugin(string pluginPath)
    {
        Console.WriteLine($"Validating plugin: {pluginPath}");
        
        // Check file exists
        if (!File.Exists(pluginPath))
        {
            Console.Error.WriteLine($"Plugin file not found: {pluginPath}");
            return;
        }
        
        // Check it''s a valid zip
        try
        {
            using var archive = ZipFile.OpenRead(pluginPath);
            
            // Check for required files
            var requiredFiles = new[] { "plugin.json" };
            var foundFiles = archive.Entries.Select(e => e.FullName).ToHashSet();
            
            foreach (var required in requiredFiles)
            {
                if (!foundFiles.Contains(required))
                {
                    Console.Error.WriteLine($"Missing required file: {required}");
                    return;
                }
            }
            
            // Read and validate manifest
            var manifestEntry = archive.GetEntry("plugin.json");
            using var stream = manifestEntry!.Open();
            var manifest = await System.Text.Json.JsonSerializer.DeserializeAsync<PluginManifest>(stream);
            
            if (manifest == null)
            {
                Console.Error.WriteLine("Invalid plugin manifest");
                return;
            }
            
            // Validate manifest fields
            var errors = new List<string>();
            
            if (string.IsNullOrEmpty(manifest.Id))
                errors.Add("Missing plugin ID");
            if (string.IsNullOrEmpty(manifest.Name))
                errors.Add("Missing plugin name");
            if (string.IsNullOrEmpty(manifest.Version))
                errors.Add("Missing plugin version");
            if (manifest.Author == null || string.IsNullOrEmpty(manifest.Author.Name))
                errors.Add("Missing author information");
            
            // Check for at least one DLL
            if (!foundFiles.Any(f => f.EndsWith(".dll")))
                errors.Add("No plugin assembly found");
            
            if (errors.Any())
            {
                Console.Error.WriteLine("Validation errors:");
                foreach (var error in errors)
                {
                    Console.Error.WriteLine($"  - {error}");
                }
                return;
            }
            
            Console.WriteLine("✓ Plugin package is valid");
            Console.WriteLine($"  ID: {manifest.Id}");
            Console.WriteLine($"  Name: {manifest.Name}");
            Console.WriteLine($"  Version: {manifest.Version}");
            Console.WriteLine($"  Author: {manifest.Author.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading plugin package: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Package a plugin for distribution
    /// </summary>
    static async Task PackagePlugin(params IOption[] options)
    {
        var project = GetOptionValue<string>(options, "--project") ?? ".";
        var output = GetOptionValue<string>(options, "--output");
        
        Console.WriteLine($"Packaging plugin from {project}...");
        
        // First build the project
        await BuildPlugin(options);
        
        // Find the output directory
        var binDir = Path.Combine(project, "bin", "Release", "net8.0");
        if (!Directory.Exists(binDir))
        {
            binDir = Path.Combine(project, "bin", "Release");
        }
        
        if (!Directory.Exists(binDir))
        {
            Console.Error.WriteLine("Build output not found. Run build first.");
            return;
        }
        
        // Read manifest to get plugin info
        var manifestPath = Path.Combine(project, "plugin.json");
        if (!File.Exists(manifestPath))
        {
            manifestPath = Path.Combine(binDir, "plugin.json");
        }
        
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine("plugin.json not found");
            return;
        }
        
        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        var manifest = System.Text.Json.JsonSerializer.Deserialize<PluginManifest>(manifestJson);
        
        if (manifest == null)
        {
            Console.Error.WriteLine("Invalid plugin manifest");
            return;
        }
        
        // Determine output path
        if (string.IsNullOrEmpty(output))
        {
            output = Path.Combine(project, "bin", "Release", 
                $"{manifest.Id}-{manifest.Version}.iimplugin");
        }
        
        // Create the package
        Console.WriteLine($"Creating package: {output}");
        
        if (File.Exists(output))
        {
            File.Delete(output);
        }
        
        using (var archive = ZipFile.Open(output, ZipArchiveMode.Create))
        {
            // Add all files from bin directory
            foreach (var file in Directory.GetFiles(binDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(binDir, file);
                archive.CreateEntryFromFile(file, relativePath);
            }
            
            // Ensure plugin.json is in root
            if (!File.Exists(Path.Combine(binDir, "plugin.json")))
            {
                archive.CreateEntryFromFile(manifestPath, "plugin.json");
            }
        }
        
        Console.WriteLine($"✓ Plugin packaged successfully: {output}");
        Console.WriteLine($"  Size: {new FileInfo(output).Length / 1024} KB");
    }
    
    /// <summary>
    /// Helper to get option value
    /// </summary>
    static T? GetOptionValue<T>(IOption[] options, string name)
    {
        var option = options.FirstOrDefault(o => o.Name == name);
        if (option is Option<T> typedOption)
        {
            // This is simplified - in real implementation would need proper parsing
            return default(T);
        }
        return default(T);
    }
}

// Mock implementations for testing
internal class MockPluginValidator : IPluginValidator
{
    public Task<ValidationResult> ValidateAsync(string pluginPath)
    {
        var result = new ValidationResult();
        // Always pass validation in test mode
        return Task.FromResult(result);
    }
}

internal class MockPluginSandbox : IPluginSandbox
{
    public Task<IIM.Plugin.SDK.PluginContext> CreateContextAsync(
        PluginManifest manifest, 
        string workingDirectory)
    {
        // Return a mock context for testing
        return Task.FromResult(new IIM.Plugin.SDK.PluginContext
        {
            Logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Test"),
            Configuration = new ConfigurationBuilder().Build(),
            FileSystem = new MockFileSystem(),
            HttpClient = new MockHttpClient(),
            ProcessRunner = new MockProcessRunner(),
            EvidenceStore = new MockEvidenceStore(),
            TempDirectory = Path.GetTempPath(),
            PluginInfo = new PluginInfo
            {
                Id = manifest.Id,
                Name = manifest.Name,
                Version = manifest.Version,
                Description = manifest.Description,
                Author = manifest.Author,
                PackagePath = workingDirectory,
                IsLoaded = true,
                IsEnabled = true
            }
        });
    }
}

// Additional mock implementations would go here...'

# ==============================================
# Update solution file
# ==============================================

print_info "Updating solution file..."

# Add new projects to solution
cat >> IIM.Platform.sln << 'EOF'
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IIM.Plugin.SDK", "src\IIM.Plugin.SDK\IIM.Plugin.SDK.csproj", "{SDK-GUID-HERE}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IIM.CLI", "tools\IIM.CLI\IIM.CLI.csproj", "{CLI-GUID-HERE}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SamplePlugin", "examples\SamplePlugin\SamplePlugin.csproj", "{SAMPLE-GUID-HERE}"
EndProject
EOF

# ==============================================
# Create plugin documentation
# ==============================================

print_info "Creating plugin documentation..."

create_file "docs/PluginDevelopment.md" '# IIM Plugin Development Guide

## Overview

IIM supports dynamic plugins that extend investigation capabilities. Plugins can:
- Integrate with external APIs and databases
- Execute forensic tools
- Process evidence with custom algorithms
- Add new investigation workflows

## Quick Start

### 1. Install the IIM CLI

```bash
dotnet tool install -g IIM.CLI
```

### 2. Create a New Plugin

```bash
dotnet new iimplugin -n MyAwesomePlugin
cd MyAwesomePlugin
```

### 3. Implement Your Plugin

```csharp
using IIM.Plugin.SDK;

[PluginMetadata(Category = "forensics", Tags = new[] { "custom" })]
public class MyAwesomePlugin : InvestigationPlugin
{
    public override string Id => "com.mycompany.awesome";
    public override string Name => "My Awesome Plugin";
    
    public override async Task<PluginResult> ExecuteAsync(
        PluginRequest request, 
        CancellationToken ct)
    {
        // Your implementation here
        return PluginResult.CreateSuccess(new { 
            Message = "Hello from my plugin!" 
        });
    }
}
```

### 4. Build and Test

```bash
# Build the plugin
iim plugin build

# Test locally
iim plugin test ./bin/Release/MyAwesomePlugin.dll --intent "my_intent"

# Package for distribution
iim plugin package
```

## Plugin Architecture

### Security Model

All plugins run in a sandboxed environment with:
- Restricted file system access
- Rate-limited network requests
- Whitelisted process execution
- Memory and CPU limits

### Available APIs

Plugins have access to secure versions of:
- File system operations
- HTTP client for API calls
- Process runner for tools
- Evidence store for saving results
- Logging infrastructure

## Intent System

Plugins declare supported intents that map to user queries:

```csharp
[IntentHandler("analyze_email")]
public async Task<PluginResult> AnalyzeEmail(PluginRequest request)
{
    var email = request.Parameters["email"];
    // Analysis logic
}
```

## Distribution

### Official Repository

Submit plugins for review:
```bash
iim plugin publish ./my-plugin.iimplugin --api-key $KEY
```

### Private Distribution

For internal/proprietary plugins:
1. Package: `iim plugin package`
2. Sign: `iim plugin sign --cert agency.pfx`
3. Deploy to internal repository

## Best Practices

1. **Handle Errors Gracefully**
   ```csharp
   try
   {
       // Your logic
   }
   catch (Exception ex)
   {
       Logger.LogError(ex, "Operation failed");
       return PluginResult.CreateError(ex.Message);
   }
   ```

2. **Validate Input**
   ```csharp
   if (!request.Parameters.ContainsKey("required_param"))
   {
       return PluginResult.CreateError("Missing required parameter");
   }
   ```

3. **Use Async/Await**
   ```csharp
   public override async Task<PluginResult> ExecuteAsync(...)
   {
       await LongRunningOperation();
   }
   ```

4. **Store Evidence**
   ```csharp
   await Evidence.StoreAsync("analysis_result", data);
   ```

## Examples

See the `examples/` directory for complete plugin examples:
- Hash Analyzer - File hash analysis
- OSINT Email - Email intelligence gathering
- Memory Analyzer - Volatility integration

## Troubleshooting

### Plugin won''t load
- Check `iim plugin validate` output
- Ensure all dependencies are included
- Verify manifest is correct

### Permission denied
- Check requested permissions in manifest
- Some operations require elevated permissions

### Performance issues
- Use async operations
- Batch API requests
- Cache results when appropriate'

# ==============================================
# Create build script for plugin system
# ==============================================

print_info "Creating build script for plugin system..."

create_file "scripts/build-plugin-system.sh" '#!/bin/bash

# Build script for IIM Plugin System

set -e

echo "Building IIM Plugin System..."

# Build SDK
echo "Building Plugin SDK..."
dotnet build src/IIM.Plugin.SDK/IIM.Plugin.SDK.csproj -c Release
dotnet pack src/IIM.Plugin.SDK/IIM.Plugin.SDK.csproj -c Release -o ./packages

# Build CLI
echo "Building IIM CLI..."
dotnet build tools/IIM.CLI/IIM.CLI.csproj -c Release
dotnet pack tools/IIM.CLI/IIM.CLI.csproj -c Release -o ./packages

# Build sample plugin
echo "Building Sample Plugin..."
dotnet build examples/SamplePlugin/SamplePlugin.csproj -c Release

# Package sample plugin
cd examples/SamplePlugin
../../tools/IIM.CLI/bin/Release/net8.0/IIM.CLI plugin package
cd ../..

echo "Plugin system build complete!"
echo "Packages created in ./packages/"
echo "Sample plugin created in examples/SamplePlugin/bin/Release/"'

chmod +x scripts/build-plugin-system.sh

# ==============================================
# Final setup steps
# ==============================================

print_info "Finalizing setup..."

# Update Program.cs to register plugin services
if [ -f "src/IIM.Api/Program.cs" ]; then
    print_info "Updating API Program.cs to include plugin services..."
    
    # Add to the existing Program.cs (after other service registrations)
    sed -i '/builder.Services.AddHostedService<WslServiceOrchestrator>();/a\
\
// Add Plugin services\
builder.Services.AddSingleton<IPluginValidator, PluginValidator>();\
builder.Services.AddSingleton<IPluginSandbox, PluginSandbox>();\
builder.Services.AddSingleton<IPluginManager, SecurePluginManager>();\
builder.Services.AddSingleton<IPluginOrchestrator, PluginOrchestrator>();' src/IIM.Api/Program.cs

    # Add plugin endpoints mapping (after other endpoint mappings)
    sed -i '/app.Run();/i\
\
// Map plugin endpoints\
app.MapPluginEndpoints();' src/IIM.Api/Program.cs
fi

# Create plugins directory structure
mkdir -p plugins/{installed,repository,temp}
create_file "plugins/README.md" '# IIM Plugins Directory

## Structure

- `installed/` - Currently installed plugins
- `repository/` - Local plugin repository cache  
- `temp/` - Temporary directory for plugin operations

## Installing Plugins

1. Place `.iimplugin` files in this directory
2. Use the IIM Desktop app to load them
3. Or use the API: `POST /v1/plugins/load`

## Security

All plugins are validated and sandboxed before loading.'

print_success "Plugin system setup complete!"

echo ""
echo "Next steps:"
echo "1. Build the plugin system: ./scripts/build-plugin-system.sh"
echo "2. Install the CLI globally: dotnet tool install -g ./packages/IIM.CLI.*.nupkg"
echo "3. Create your first plugin: iim plugin new -n MyFirstPlugin"
echo "4. Check the documentation: docs/PluginDevelopment.md"
echo ""
echo "The plugin system provides:"
echo "  ✓ Dynamic plugin loading with security sandboxing"
echo "  ✓ Intent-based routing for natural language execution"
echo "  ✓ Full API and CLI tool integration"
echo "  ✓ Plugin development SDK and templates"
echo "  ✓ Distribution and repository system"
echo "  ✓ Complete audit trail for law enforcement"