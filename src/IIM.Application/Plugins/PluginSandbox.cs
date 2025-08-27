using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IIM.Plugin.SDK;
using IIM.Plugin.SDK.Security;
using IIM.Shared.Interfaces;
using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Core.Plugins.Security;
using IIM.Core.Plugins.Security.Implementations;
using IIM.Core.Plugins;


namespace IIM.Application.Plugins.Security;

/// <summary>
/// Creates sandboxed execution contexts for plugins
/// </summary>
public class PluginSandbox : IPluginSandbox
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly IEvidenceManager _evidenceManager;
    private readonly Dictionary<string, PluginContext> _contexts = new();

    public PluginSandbox(
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        IEvidenceManager evidenceManager)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;
        _evidenceManager = evidenceManager;
    }

    public async Task<PluginContext> CreateContextAsync(PluginManifest plugin)
    {
        // Create plugin-specific configuration
        var pluginConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PluginId"] = plugin.Id,
                ["PluginName"] = plugin.Name,
                ["MaxMemoryMB"] = "100",
                ["MaxCpuPercent"] = "25",
                ["NetworkAccess"] = "Restricted"
            })
            .Build();

        // Create sandboxed services
        var tempPath = Path.Combine(Path.GetTempPath(), "IIM", "Plugins", plugin.Id);
        Directory.CreateDirectory(tempPath);
        
        var fileSystem = new RestrictedFileSystem(
            tempPath,
            _loggerFactory.CreateLogger<RestrictedFileSystem>()
        );

        var httpClient = new RateLimitedHttpClient(
            new HttpClient(),
            _loggerFactory.CreateLogger<RateLimitedHttpClient>()
        );

        var processRunner = new SandboxedProcessRunner(
            _loggerFactory.CreateLogger<SandboxedProcessRunner>()
        );

        var evidenceStore = new NamespacedEvidenceStore(
            plugin.Id,
            _loggerFactory.CreateLogger<NamespacedEvidenceStore>()
        );

        var context = new PluginContext
        {
            Logger = _loggerFactory.CreateLogger($"Plugin.{plugin.Id}"),
            Configuration = pluginConfig,
            FileSystem = fileSystem,
            HttpClient = httpClient,
            ProcessRunner = processRunner,
            EvidenceStore = evidenceStore,
            PluginInfo = new PluginInfo
            {
                Id = plugin.Id,
                Name = plugin.Name,
                Version = plugin.Version,
                Author = plugin.Author?.Name ?? "Unknown",
                Description = plugin.Description ?? string.Empty,
                IsEnabled = true,
                IsLoaded = true
            }
        };

        _contexts[plugin.Id] = context;
        return context;
    }

    public async Task DestroyContextAsync(string pluginId)
    {
        if (_contexts.TryGetValue(pluginId, out var context))
        {
            await context.DisposeAsync();
            _contexts.Remove(pluginId);
        }
    }

    public Task<bool> ValidateSecurityAsync(PluginManifest plugin)
    {
        // Validate plugin security requirements
        var violations = new List<string>();

        // Check requested permissions
        if (plugin.Permissions?.NetworkAccess == "Unrestricted")
        {
            violations.Add("Unrestricted network access not allowed");
        }

        if (plugin.Permissions?.FileSystemAccess == "Full")
        {
            violations.Add("Full file system access not allowed");
        }

        return Task.FromResult(violations.Count == 0);
    }
}

