using Microsoft.Extensions.Logging;

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
}
