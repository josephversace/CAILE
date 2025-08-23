using System.CommandLine;
using System.IO.Compression;
using IIM.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IIM.Core.Plugins.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using IIM.Plugin.SDK;
using IIM.Shared.DTOs;
using IIM.Shared.Interfaces;

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
        var buildCommand = new Command("build", "Build a plugin package");
        var projectOption = new Option<string>("--project", getDefaultValue: () => ".", "Project directory");
        var outputOption = new Option<string>("--output", getDefaultValue: () => "./bin/Release", "Output directory");
        var signOption = new Option<bool>("--sign", "Sign the plugin package");
        
        buildCommand.AddOption(projectOption);
        buildCommand.AddOption(outputOption);
        buildCommand.AddOption(signOption);
        
        buildCommand.SetHandler(async (project, output, sign) =>
        {
            await BuildPlugin(project, output, sign);
        }, projectOption, outputOption, signOption);
        
        pluginCommand.Add(buildCommand);
        
        // Test command
        var testCommand = new Command("test", "Test a plugin locally");
        var pluginArg = new Argument<string>("plugin", "Path to plugin package");
        var intentOption = new Option<string>("--intent", "Intent to test");
        var paramsOption = new Option<string>("--params", "JSON parameters for the intent");
        
        testCommand.AddArgument(pluginArg);
        testCommand.AddOption(intentOption);
        testCommand.AddOption(paramsOption);
        
        testCommand.SetHandler(async (pluginPath, intent, paramsJson) =>
        {
            await TestPlugin(pluginPath, intent, paramsJson);
        }, pluginArg, intentOption, paramsOption);
        
        pluginCommand.Add(testCommand);
        
        // Validate command
        var validateCommand = new Command("validate", "Validate a plugin package");
        var validatePluginArg = new Argument<string>("plugin", "Path to plugin package");
        validateCommand.AddArgument(validatePluginArg);
        
        validateCommand.SetHandler(async (pluginPath) =>
        {
            await ValidatePlugin(pluginPath);
        }, validatePluginArg);
        
        pluginCommand.Add(validateCommand);
        
        // Package command
        var packageCommand = new Command("package", "Package a plugin for distribution");
        var packageProjectOption = new Option<string>("--project", getDefaultValue: () => ".", "Project directory");
        var packageOutputOption = new Option<string>("--output", "Output file path");
        
        packageCommand.AddOption(packageProjectOption);
        packageCommand.AddOption(packageOutputOption);
        
        packageCommand.SetHandler(async (project, output) =>
        {
            await PackagePlugin(project, output);
        }, packageProjectOption, packageOutputOption);
        
        pluginCommand.Add(packageCommand);
        
        return await rootCommand.InvokeAsync(args);
    }
    
    /// <summary>
    /// Build a plugin project
    /// </summary>
    static async Task BuildPlugin(string project, string output, bool sign)
    {
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
    static async Task TestPlugin(string pluginPath, string? intent, string? paramsJson)
    {
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
            
            var request = new PluginRequest
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
        
        // Check its a valid zip
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
    static async Task PackagePlugin(string project, string? output)
    {
        Console.WriteLine($"Packaging plugin from {project}...");
        
        // First build the project
        await BuildPlugin(project, Path.Combine(project, "bin", "Release"), false);
        
        // Find the output directory
        var binDir = Path.Combine(project, "bin", "Release", "net9.0");
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
    public Task<PluginContext> CreateContextAsync(PluginManifest plugin)
    {
        // Return a mock context for testing
        var context = new PluginContext
        {
            Logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Test"),
            Configuration = new ConfigurationBuilder().Build(),
            FileSystem = new MockFileSystem(),
            HttpClient = new MockHttpClient(),
            ProcessRunner = new MockProcessRunner(),
            EvidenceStore = new MockEvidenceStore(),
            PluginInfo = new PluginInfo
            {
                Id = plugin.Id,
                Name = plugin.Name,
                Version = plugin.Version,
                Description = plugin.Description ?? string.Empty,
                Author = plugin.Author?.Name ?? "Unknown",
                IsLoaded = true,
                IsEnabled = true
            }
        };
        return Task.FromResult(context);
    }

    public Task DestroyContextAsync(string pluginId)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ValidateSecurityAsync(PluginManifest plugin)
    {
        return Task.FromResult(true);
    }
}

// Mock file system implementation
internal class MockFileSystem : IIM.Plugin.SDK.Security.ISecureFileSystem
{
    public Task<byte[]> ReadFileAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<byte>());

    public Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(string.Empty);

    public Task WriteFileAsync(string path, byte[] data, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task WriteTextAsync(string path, string text, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<FileMetadata?> GetFileMetadataAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult<FileMetadata?>(null);

    public Task<string[]> ListFilesAsync(string directory, string searchPattern = "*", CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<string>());
}

// Mock HTTP client implementation
internal class MockHttpClient : IIM.Shared.Interfaces.ISecureHttpClient
{
    public Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
        => Task.FromResult<T?>(default);

    public Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken cancellationToken = default)
        => Task.FromResult<TResponse?>(default);

    public Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<byte>());

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
}

// Mock process runner implementation
internal class MockProcessRunner : ISecureProcessRunner
{
    public Task<ProcessResult> RunAsync(string command, string[] args, CancellationToken cancellationToken = default)
        => Task.FromResult(new ProcessResult { ExitCode = 0, StandardOutput = string.Empty, StandardError = string.Empty });
}

// Mock evidence store implementation
internal class MockEvidenceStore : IEvidenceStore
{
    public Task StoreAsync(string key, object data, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<T?> RetrieveAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult<T?>(default);

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}