using DocumentFormat.OpenXml.Drawing.Charts;
using IIM.Application.Behaviors;
using IIM.Application.Commands.Investigation;
using IIM.Application.Commands.Models;
using IIM.Application.Commands.Wsl;
using IIM.Application.Handlers;
using IIM.Application.Interfaces;
using IIM.Application.Queries;
using IIM.Application.Services;
using IIM.Components.Services;
using IIM.Core.AI;
using IIM.Core.Configuration;
using IIM.Core.Inference;
using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Core.RAG;
using IIM.Core.Security;
using IIM.Core.Services;
using IIM.Core.Services.Configuration;
using IIM.Core.Storage;
using IIM.Infrastructure.Platform;
using IIM.Infrastructure.Storage;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.Caching.Memory;
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
                services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();

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
                services.AddScoped<ISessionService, SessionService>();  
                
                services.AddScoped<IModelConfigurationTemplateService , ModelConfigurationTemplateService>();

                services.AddScoped<IEvidenceManager, EvidenceManager>();
                services.AddScoped<ICaseManager, JsonCaseManager>();


                // ========================================
                // Mediator Services
                // ========================================

            // Add the mediator with assembly scanning
services.AddSimpleMediator(
    typeof(Program).Assembly,                          // Desktop assembly
    typeof(EnsureWslCommand).Assembly,                 // Application assembly (where commands are)
    typeof(IInvestigationService).Assembly,            // Core assembly
    typeof(IModelOrchestrator).Assembly                // Core assembly
);

                // Add memory cache for caching behavior (if not already added)
                services.AddMemoryCache(options =>
                {
                    options.SizeLimit = 100_000_000; // 100MB cache limit
                    options.CompactionPercentage = 0.25;
                    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
                });

                // Register ALL pipeline behaviors in order (they execute in registration order)
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));       // 1. Logging
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));    // 2. Validation
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));   // 3. Performance monitoring
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));       // 4. Caching for queries
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));         // 5. Retry logic
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));   // 6. Transactions
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));         // 7. Audit logging

                // Register notification handlers for WSL events
                services.AddTransient<INotificationHandler<WslSetupCompletedNotification>, WslSetupCompletedHandler>();
                services.AddTransient<INotificationHandler<WslSetupFailedNotification>, WslSetupFailedHandler>();
                services.AddTransient<INotificationHandler<WslFeatureEnabledNotification>, WslFeatureEnabledHandler>();
                services.AddTransient<INotificationHandler<WslDistroInstalledNotification>, WslDistroInstalledHandler>();

                // Register notification handlers for Model events
                services.AddTransient<INotificationHandler<ModelLoadedNotification>, ModelLoadedNotificationHandler>();
                services.AddTransient<INotificationHandler<ModelLoadedNotification>, ModelLoadedAuditHandler>(); // Same event, different handler
                services.AddTransient<INotificationHandler<ModelLoadFailedNotification>, ModelLoadFailedHandler>();
                services.AddTransient<INotificationHandler<ModelUnloadedNotification>, ModelUnloadedHandler>();

                // Register notification handlers for Investigation events
                services.AddTransient<INotificationHandler<InvestigationQueryStartedNotification>, InvestigationQueryStartedHandler>();
                services.AddTransient<INotificationHandler<InvestigationQueryCompletedNotification>, InvestigationQueryCompletedHandler>();
                services.AddTransient<INotificationHandler<InvestigationQueryFailedNotification>, InvestigationQueryFailedHandler>();
                services.AddTransient<INotificationHandler<SessionCreatedNotification>, SessionCreatedHandler>();


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
