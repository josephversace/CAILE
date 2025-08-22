using IIM.Components.Services;
using IIM.Core.Services;
using IIM.Desktop.Services;
using IIM.Shared.Interfaces;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows.Forms;

namespace IIM.Desktop;

/// <summary>
/// Main program entry point for the IIM Desktop application.
/// Configures and launches the Windows Forms Blazor Hybrid application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// Configures Windows Forms settings and launches the application host.
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
    /// Gets the global service provider for dependency injection.
    /// Used to resolve services throughout the application lifetime.
    /// </summary>
    public static IServiceProvider ServiceProvider { get; private set; } = default!;

    /// <summary>
    /// Creates and configures the host builder with all necessary services and configuration.
    /// Sets up configuration sources, logging, and dependency injection container.
    /// </summary>
    /// <returns>Configured IHostBuilder ready to build and run</returns>
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

                // Add memory cache for UI state
                services.AddMemoryCache();

                // ========================================
                // API Client Configuration (MAIN SERVICE)
                // ========================================

                // Configure the API client to talk to the backend
                services.AddHttpClient<IIMApiClient>(client =>
                {
                    var apiUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5080";
                    client.BaseAddress = new Uri(apiUrl);
                    client.DefaultRequestHeaders.Add("User-Agent", "IIM-Desktop/1.0");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                // Register the API client as the primary service
                services.AddScoped<IIIMApiClient, IIMApiClient>();

                //Notifications
                services.AddScoped<INotificationService, NotificationService>();

                // ========================================
                // UI-Only Services
                // ========================================

                // Register main form
                services.AddSingleton<MainForm>();

                // UI State Management
                services.AddSingleton<StateContainer>();

                // Notification Service (UI notifications)
                services.AddSingleton<INotificationService, NotificationService>();

                // SignalR Hub Connection for real-time updates
                services.AddSingleton<IHubConnectionService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<HubConnectionService>>();
                    var apiUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5080";
                    return new HubConnectionService(logger, apiUrl);
                });

                // UI-specific services
                services.AddScoped<DataFormattingService>();
                services.AddScoped<ModelSizeCalculator>();

                // ========================================
                // Configuration for UI
                // ========================================

                // Deployment configuration (determines UI behavior)
                services.AddSingleton<DeploymentConfiguration>(sp =>
                {
                    var deploymentConfig = new DeploymentConfiguration();
                    configuration.GetSection("Deployment").Bind(deploymentConfig);
                    return deploymentConfig;
                });

                // Local settings (UI preferences)
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
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
    /// Gets the current application version from assembly metadata.
    /// Used for displaying version information in the UI and for telemetry.
    /// </summary>
    /// <returns>Version string in format "Major.Minor.Build" or "1.0.0" if not available</returns>
    public static string GetApplicationVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    /// <summary>
    /// Gets the local settings directory path for storing UI preferences.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    /// <returns>Full path to the settings directory</returns>
    public static string GetLocalSettingsDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsDir = Path.Combine(appData, "IIM", "Settings");

        if (!Directory.Exists(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }

        return settingsDir;
    }
}