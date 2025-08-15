
using IIM.App.Hybrid.Services;
using IIM.Core.AI;
using IIM.Core.Platform;
using IIM.Core.RAG;
using IIM.Core.Security;
using IIM.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IIM.App.Hybrid;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif


#if DEBUG
        // Use mock services for local UI development
        // builder.Services.AddSingleton<IInferenceService, MockInferenceService>();
        builder.Services.AddLogging(configure =>
        {
            // configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Debug);
        });
#else
        // Use real GPU services in production
        builder.Services.AddSingleton<IInferenceService, GpuInferenceService>();
        builder.Services.AddLogging(configure =>
        {
            configure.SetMinimumLevel(LogLevel.Information);
        });
#endif

        // ============================================
        // Register Core Services
        // ============================================

        // Investigation Services
        builder.Services.AddSingleton<IInvestigationService, InvestigationService>();
        builder.Services.AddSingleton<ICaseManager, CaseManager>();
        builder.Services.AddSingleton<IEvidenceManager, EvidenceManager>();

        // AI/ML Services - Register concrete implementations when ready
        // For now, you can create mock implementations or throw NotImplementedException
        builder.Services.AddSingleton<IModelOrchestrator>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MockModelOrchestrator>>();
            return new MockModelOrchestrator(logger);
        });

        builder.Services.AddSingleton<IInferencePipeline>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MockInferencePipeline>>();
            return new MockInferencePipeline(logger);
        });

        // Model Management Service (depends on IModelOrchestrator)
        builder.Services.AddSingleton<IModelManagementService, ModelManagementService>();

        // RAG Services
        builder.Services.AddSingleton<IQdrantService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MockQdrantService>>();
            return new MockQdrantService(logger);
        });

        // Platform Services
        builder.Services.AddSingleton<IWslManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MockWslManager>>();
            return new MockWslManager(logger);
        });

        // ============================================
        // Register App.Hybrid UI Services (if any)
        // ============================================

        // Notification service, theme service, etc.
        builder.Services.AddSingleton<INotificationService, NotificationService>();
        builder.Services.AddSingleton<IHubConnectionService, HubConnectionService>();

        builder.Services.AddSingleton<EvidenceConfiguration>();

        builder.Services.AddSingleton<IInferenceService, InferenceService>();
        builder.Services.AddSingleton<IModelManagementService, ModelManagementService>();

        // App Services
        builder.Services.AddSingleton<IimClient>();

        // HTTP Clients
        builder.Services.AddHttpClient<IimClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5080");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ============================================
        // Configuration
        // ============================================

        // Add configuration if needed
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        builder.Services.AddSingleton<IConfiguration>(configuration);

        // Add HttpClient for API calls
        builder.Services.AddHttpClient("IIM.Api", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7001"); // Your API URL
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });


        return builder.Build();
    }
}
