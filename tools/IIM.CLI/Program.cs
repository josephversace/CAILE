using System.CommandLine;
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

// Additional mock implementations would go here...
