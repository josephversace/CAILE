using IIM.Application.Interfaces;
using IIM.Application.Services;
using IIM.Components.Services;
using IIM.Core.AI;
using IIM.Core.Configuration;
using IIM.Core.Inference;
using IIM.Core.Models;
using IIM.Core.Platform;
using IIM.Core.RAG;
using IIM.Core.Security;
using IIM.Core.Services;
using IIM.Core.Storage;
using IIM.Infrastructure.Platform;
using IIM.Infrastructure.Storage;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows.Forms;

namespace IIM.Desktop;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Configure Windows Forms for high DPI and modern rendering
        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.SystemAware);
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        // Build the host
        var host = CreateHostBuilder().Build();

        // Store service provider globally for access across the app
        ServiceProvider = host.Services;

        // Run the application
        try
        {
            var mainForm = ServiceProvider.GetRequiredService<MainForm>();
            System.Windows.Forms.Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start application:\n{ex.Message}",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            var logger = ServiceProvider.GetService<ILogger<MainForm>>();
            logger?.LogCritical(ex, "Application failed to start");
        }
    }

    /// <summary>
    /// Global service provider for the application
    /// </summary>
    public static IServiceProvider ServiceProvider { get; private set; } = default!;

    /// <summary>
    /// Creates and configures the host builder
    /// </summary>
    static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                var appPath = AppDomain.CurrentDomain.BaseDirectory;

                config.SetBasePath(appPath);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

#if DEBUG
                config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
                config.AddUserSecrets<MainForm>();
#endif

                config.AddEnvironmentVariables("IIM_");
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // ========================================
                // Core Framework Services
                // ========================================

                // Add Windows Forms and Blazor support
                services.AddWindowsFormsBlazorWebView();

#if DEBUG
                services.AddBlazorWebViewDeveloperTools();
#endif

                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddDebug();
                    builder.AddConsole();

#if DEBUG
                    builder.SetMinimumLevel(LogLevel.Debug);
#else
                    builder.SetMinimumLevel(LogLevel.Information);
#endif
                });

                // Add memory cache
                services.AddMemoryCache();

                // Add HttpClient factory
                services.AddHttpClient();
                services.AddHttpClient("IIM.API", client =>
                {
                    client.BaseAddress = new Uri(configuration["Api:BaseUrl"] ?? "http://localhost:5080");
                    client.DefaultRequestHeaders.Add("User-Agent", "IIM-Desktop/1.0");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                // Register main form
                services.AddSingleton<MainForm>();

                // ========================================
                // Configuration & Storage
                // ========================================

                // Storage configuration
                services.AddSingleton<StorageConfiguration>(sp =>
                {
                    var config = new StorageConfiguration
                    {
                        BasePath = configuration["Storage:LocalBasePath"] ??
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIM")
                    };
                    config.EnsureDirectoriesExist();
                    return config;
                });

                // Evidence configuration
                services.AddSingleton<EvidenceConfiguration>(sp =>
                {
                    var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                    return new EvidenceConfiguration
                    {
                        StorePath = storageConfig.EvidencePath,
                        EnableEncryption = configuration.GetValue<bool>("Evidence:EnableEncryption", false),
                        RequireDualControl = configuration.GetValue<bool>("Evidence:RequireDualControl", false),
                        MaxFileSizeMb = configuration.GetValue<int>("Evidence:MaxFileSizeMb", 10240)
                    };
                });

                // Configure MinIO settings
                services.Configure<MinIOConfiguration>(
                    configuration.GetSection("Storage:MinIO"));

                // ========================================
                // Platform Services (WSL, Docker, etc.)
                // ========================================

                // WSL Manager - Fixed with IHttpClientFactory
                services.AddSingleton<IWslManager>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<WslManager>>();
                    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                    return new WslManager(logger, httpFactory);
                });

                // ========================================
                // AI/ML Services
                // ========================================

                // Model Orchestrator - TODO: Replace with real implementation
                services.AddSingleton<IModelOrchestrator, DefaultModelOrchestrator>();

                // Inference Pipeline
                services.AddSingleton<IInferencePipeline, InferencePipeline>();

                // Model Management Service
                services.AddSingleton<IModelManagementService, ModelManagementService>();

                // Inference Service
                services.AddSingleton<IInferenceService, InferenceService>();

                // ========================================
                // RAG & Vector Services
                // ========================================

                // Qdrant service (in-memory for now, replace with real implementation)
                services.AddSingleton<IQdrantService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<InMemoryQdrantService>>();
                    var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                    return new InMemoryQdrantService(logger, storageConfig);
                });

                // ========================================
                // Investigation Services
                // ========================================

                // Core investigation services
                services.AddScoped<IInvestigationService, InvestigationService>();
                services.AddScoped<ISessionProvider, InvestigationService>();  
                services.AddScoped<IEvidenceManager, EvidenceManager>();
                services.AddScoped<ICaseManager, JsonCaseManager>();

                // ========================================
                // Storage Services
                // ========================================

                // Deduplication service
                services.AddSingleton<IDeduplicationService, FixedSizeDeduplicationService>();

                // MinIO storage service
                services.AddSingleton<IMinIOStorageService, MinIOStorageService>();

                // ========================================
                // Export Services
                // ========================================

                // Register export services with base path
                services.AddExportServices(GetDataDirectory());

                // ========================================
                // UI Services
                // ========================================

                // Desktop-specific UI services
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IHubConnectionService, HubConnectionService>();
                services.AddScoped<IVisualizationService, VisualizationService>();
                services.AddScoped<DataFormattingService>();
                services.AddScoped<ModelSizeCalculator>();

                // ========================================
                // HTTP Clients
                // ========================================

                // IIM API Client
                services.AddHttpClient<IimClient>(client =>
                {
                    client.BaseAddress = new Uri(configuration["Api:BaseUrl"] ?? "http://localhost:5080");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                });
            })
            .ConfigureLogging((context, logging) =>
            {
                if (context.HostingEnvironment.IsDevelopment())
                {
                    logging.AddFilter("Microsoft.AspNetCore.Components.WebView", LogLevel.Debug);
                }
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                var isDevelopment = context.HostingEnvironment.IsDevelopment();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            });
    }

    /// <summary>
    /// Gets the current application version
    /// </summary>
    public static string GetApplicationVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    /// <summary>
    /// Gets the application data directory
    /// </summary>
    public static string GetDataDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDir = Path.Combine(appData, "IIM", "Data");

        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        return dataDir;
    }

    /// <summary>
    /// Gets the models directory
    /// </summary>
    public static string GetModelsDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var modelsDir = Path.Combine(appData, "IIM", "Models");

        if (!Directory.Exists(modelsDir))
        {
            Directory.CreateDirectory(modelsDir);
        }

        return modelsDir;
    }
}

// ========================================
// Mock Implementations (Remove when real ones are ready)
// ========================================

#region Mock Implementations

/// <summary>
/// Default model orchestrator implementation
/// TODO: Replace with real implementation
/// </summary>
/// <summary>
/// Default model orchestrator implementation
/// TODO: Replace with real implementation
/// </summary>

/// <summary>
/// Default model orchestrator implementation
/// TODO: Replace with real implementation
/// </summary>
public class DefaultModelOrchestrator : IModelOrchestrator
{
    private readonly ILogger<DefaultModelOrchestrator> _logger;

    // Events - using the correct types from the interface
    public event EventHandler<ModelLoadedEventArgs>? ModelLoaded;
    public event EventHandler<ModelUnloadedEventArgs>? ModelUnloaded;
    public event EventHandler<ModelErrorEventArgs>? ModelError;
    public event EventHandler<ResourceThresholdEventArgs>? ResourceThresholdExceeded;

    public DefaultModelOrchestrator(ILogger<DefaultModelOrchestrator> logger)
    {
        _logger = logger;
    }

    event EventHandler<Core.AI.ModelErrorEventArgs>? IModelOrchestrator.ModelError
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    event EventHandler<Core.AI.ResourceThresholdEventArgs>? IModelOrchestrator.ResourceThresholdExceeded
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    public Task<ModelHandle> LoadModelAsync(ModelRequest request, IProgress<float>? progress = null, CancellationToken ct = default)
    {
        _logger.LogWarning("Using mock ModelOrchestrator - LoadModelAsync not implemented");
        progress?.Report(1.0f);
        return Task.FromResult(new ModelHandle
        {
            ModelId = request.ModelId,
            SessionId = Guid.NewGuid().ToString("N"),
            Provider = "mock",
            Type = request.ModelType
        });
    }

    public Task<bool> UnloadModelAsync(string modelId, CancellationToken ct = default)
    {
        _logger.LogWarning("Using mock ModelOrchestrator - UnloadModelAsync not implemented");
        return Task.FromResult(true);
    }

    public Task<bool> IsModelLoadedAsync(string modelId, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    // Return List<ModelConfiguration> instead of List<ModelInfo>
    public Task<List<ModelConfiguration>> GetLoadedModelsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new List<ModelConfiguration>());
    }

    public Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new List<ModelConfiguration>());
    }

    public Task<ModelConfiguration?> GetModelInfoAsync(string modelId, CancellationToken ct = default)
    {
        return Task.FromResult<ModelConfiguration?>(null);
    }

    public Task<bool> UpdateModelParametersAsync(string modelId, Dictionary<string, object> parameters, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    // Use the correct GpuStats type from Core.Models
    public Task<GpuStats> GetGpuStatsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new GpuStats());
    }

    // Use ModelResourceUsage instead of ResourceUsage
    public Task<ModelResourceUsage> GetModelResourceUsageAsync(string modelId, CancellationToken ct = default)
    {
        return Task.FromResult(new ModelResourceUsage());
    }

    public Task<bool> OptimizeMemoryAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task<long> GetTotalMemoryUsageAsync(CancellationToken ct = default)
    {
        return Task.FromResult(0L);
    }

    // Implement the correct signature
    public Task<bool> DownloadModelAsync(string modelId, string url, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task<bool> DeleteModelAsync(string modelId, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task<long> GetModelSizeAsync(string modelId, CancellationToken ct = default)
    {
        return Task.FromResult(0L);
    }

    // Return Task<ModelStats> instead of OrchestratorStats
    public Task<ModelStats> GetStatsAsync()
    {
        return Task.FromResult(new ModelStats());
    }

    public Task<ModelStats> GetStatsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new ModelStats());
    }

    // Implement InferAsync
    public Task<T> InferAsync<T>(string modelId, object input, CancellationToken ct = default)
    {
        throw new NotImplementedException("Mock implementation - inference not available");
    }

    Task<Core.AI.GpuStats> IModelOrchestrator.GetGpuStatsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DownloadModelAsync(string modelId, string source, IProgress<Core.AI.DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<InferenceResult> InferAsync(string modelId, object input, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

// Supporting classes needed for the mock
public class ModelEventArgs : EventArgs
{
    public string ModelId { get; set; } = string.Empty;
}

public class ModelErrorEventArgs : EventArgs
{
    public string ModelId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class ResourceThresholdEventArgs : EventArgs
{
    public string ResourceType { get; set; } = string.Empty;
    public long CurrentValue { get; set; }
    public long Threshold { get; set; }
}

public class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public long Size { get; set; }
}

public class GpuStats
{
    public long TotalMemory { get; set; }
    public long UsedMemory { get; set; }
    public float Utilization { get; set; }
}

public class ResourceUsage
{
    public long MemoryBytes { get; set; }
    public float CpuPercent { get; set; }
    public float GpuPercent { get; set; }
}

public class OrchestratorStats
{
    public int LoadedModels { get; set; }
    public long TotalMemoryUsage { get; set; }
}

public class DownloadProgress
{
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public float PercentComplete => TotalBytes > 0 ? (float)BytesDownloaded / TotalBytes * 100 : 0;
}

#endregion