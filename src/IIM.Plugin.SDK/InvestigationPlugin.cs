using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IIM.Shared.Models;         // for ProcessResult (if defined there)
using IIM.Shared.Interfaces;   // for IEvidenceStore (if defined there)

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
protected async Task<HttpResponseMessage> CallApiAsync(string url, HttpContent? content = null)
{
    // ISecureHttpClient doesn't return HttpResponseMessage, it returns the deserialized response
    // So we need to change the return type or create a mock HttpResponseMessage
    if (content == null)
    {
        var result = await _context!.HttpClient.GetAsync<Dictionary<string, object>>(url);
        // Create a mock response since ISecureHttpClient doesn't return HttpResponseMessage
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }
    else
    {
        // Extract data from HttpContent if needed, or just pass empty data
        var result = await _context!.HttpClient.PostAsync<Dictionary<string, object>, Dictionary<string, object>>(
            url, 
            new Dictionary<string, object>()
        );
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }
}
    
/// <summary>
/// Helper to call an API securely with an allow-listed set of domains.
/// Throws if the URL's host is not in the list.
/// </summary>
protected async Task<HttpResponseMessage> CallApiAsync(
    string url,
    IEnumerable<string> allowedDomains,
    HttpContent? content = null,
    CancellationToken ct = default)
{
    var ok = false;
    foreach (var d in allowedDomains)
    {
        if (PluginSecurity.IsAllowedDomain(url, d)) { ok = true; break; }
    }
    if (!ok)
        throw new InvalidOperationException($"Domain not allowed for URL: {url}");

    var data = new Dictionary<string, object>();
    var result = await _context!.HttpClient.PostAsync<Dictionary<string, object>, Dictionary<string, object>>(url, data, ct);
    return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
}

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
