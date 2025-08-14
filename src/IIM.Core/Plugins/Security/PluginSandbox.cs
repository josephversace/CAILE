using IIM.Core.Security;
using IIM.Plugin.SDK;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
}
