using IIM.Core.AI;
using IIM.Core.Inference;
using IIM.Core.Platform;
using IIM.Core.RAG;
using IIM.Core.Security;
using IIM.Core.Services;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI.Interfaces;
using System;
using System.IO;
using System.Windows.Forms;
using IFileService = IIM.Core.Services.IFileService;

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
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        // Build the host
        var host = CreateHostBuilder().Build();
        
        // Store service provider globally for access across the app
        ServiceProvider = host.Services;
        
        // Run the application
        try
        {
            var mainForm = ServiceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
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
                // Add configuration sources
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                
                config.SetBasePath(appPath);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                
                #if DEBUG
                config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
                #endif
                
                config.AddEnvironmentVariables("IIM_");
                
                // Add user secrets in development
                #if DEBUG
                config.AddUserSecrets<MainForm>();
                #endif
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                
                // ========================================
                // Core Services
                // ========================================
                
                // Add Windows Forms and Blazor support
                services.AddWindowsFormsBlazorWebView();
                
                #if DEBUG
                // Enable Blazor debugging in Debug mode
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
                
                // Register main form
                services.AddSingleton<MainForm>();
                
                // Add HttpClient factory
                services.AddHttpClient();
                services.AddHttpClient("IIM.API", client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "IIM-Desktop/1.0");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                // ========================================
                // Application Services
                // ========================================

                // TODO: Register your services here - uncomment as needed


                // ============================================
                // Register Core Services
                // ============================================

                // Investigation Services
                services.AddScoped<IInvestigationService, InvestigationService>();
                services.AddSingleton<ICaseManager, CaseManager>();
                services.AddSingleton<IEvidenceManager, EvidenceManager>();

                // AI/ML Services - Register concrete implementations when ready
                // For now, you can create mock implementations or throw NotImplementedException
                services.AddSingleton<IModelOrchestrator>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<MockModelOrchestrator>>();
                    return new MockModelOrchestrator(logger);
                });

                services.AddSingleton<IInferencePipeline>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<MockInferencePipeline>>();
                    return new MockInferencePipeline(logger);
                });

                // Model Management Service (depends on IModelOrchestrator)
                services.AddSingleton<IModelManagementService, ModelManagementService>();

                // RAG Services
                services.AddSingleton<IQdrantService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<MockQdrantService>>();
                    return new MockQdrantService(logger);
                });

                // Platform Services
                services.AddSingleton<IWslManager>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<MockWslManager>>();
                    return new MockWslManager(logger);
                });

                // ============================================
                // Register Desktop UI Services (if any)
                // ============================================

                // Notification service, theme service, etc.
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IHubConnectionService, HubConnectionService>();

                services.AddSingleton<EvidenceConfiguration>();

                services.AddSingleton<IInferenceService, InferenceService>();
                services.AddSingleton<IModelManagementService, ModelManagementService>();

                // In your Program.cs or wherever you configure services

               
                // Add Export/Security/File Services
             
                services.AddExportServices("");

                // Add Investigation Service - make sure this comes AFTER export services
                services.AddScoped<IInvestigationService, InvestigationService>();

                // Add any other missing services
               services.AddScoped<IEvidenceManager, EvidenceManager>();
                services.AddScoped<ICaseManager, CaseManager>();



                // HTTP Clients
                services.AddHttpClient<IimClient>(client =>
                {
                    client.BaseAddress = new Uri("http://localhost:5080");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                });

             


                // Investigation Services
                // services.AddSingleton<IInvestigationService, InvestigationService>();
                // services.AddSingleton<ICaseManagementService, CaseManagementService>();
                // services.AddSingleton<IEvidenceService, EvidenceService>();
                // services.AddSingleton<IReportingService, ReportingService>();

                // AI/ML Services
                // services.AddSingleton<IModelManagementService, ModelManagementService>();
                // services.AddSingleton<IOnnxRuntimeService, OnnxRuntimeService>();
                // services.AddSingleton<IPromptService, PromptService>();
                // services.AddSingleton<IVectorDatabaseService, VectorDatabaseService>();
                // services.AddSingleton<IEmbeddingService, EmbeddingService>();

                // Model Providers
                // services.AddSingleton<IOllamaProvider, OllamaProvider>();
                // services.AddSingleton<IWhisperProvider, WhisperProvider>();
                // services.AddSingleton<ICLIPProvider, CLIPProvider>();

                // Data Services
                // services.AddSingleton<IDatabaseService, DatabaseService>();
                // services.AddSingleton<IFileStorageService, FileStorageService>();
                // services.AddSingleton<IEncryptionService, EncryptionService>();

                // Tool Services
                // services.AddSingleton<IOSINTService, OSINTService>();
                // services.AddSingleton<IForensicsService, ForensicsService>();
                // services.AddSingleton<IHashingService, HashingService>();

                // ========================================
                // Configuration Objects
                // ========================================

                // Bind configuration sections to strongly-typed objects
                // services.Configure<ModelSettings>(configuration.GetSection("ModelSettings"));
                // services.Configure<SecuritySettings>(configuration.GetSection("Security"));
                // services.Configure<StorageSettings>(configuration.GetSection("Storage"));

                // ========================================
                // Database Configuration
                // ========================================

                // If using Entity Framework Core
                // services.AddDbContext<InvestigationContext>(options =>
                // {
                //     var connectionString = configuration.GetConnectionString("InvestigationDb") 
                //         ?? "Data Source=investigations.db";
                //     options.UseSqlite(connectionString);
                // });

                // ========================================
                // Background Services
                // ========================================

                // Register any background services
                // services.AddHostedService<ModelPreloadService>();
                // services.AddHostedService<TelemetryService>();

                // ========================================
                // Singleton State Services
                // ========================================

                // Application state management
                // services.AddSingleton<IApplicationState, ApplicationState>();
                // services.AddSingleton<ISessionManager, SessionManager>();
                // services.AddSingleton<INavigationService, NavigationService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                // Additional logging configuration if needed
                if (context.HostingEnvironment.IsDevelopment())
                {
                    logging.AddFilter("Microsoft.AspNetCore.Components.WebView", LogLevel.Debug);
                }
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                // Service provider validation in development
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