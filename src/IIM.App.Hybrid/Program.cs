
using IIM.App.Hybrid.Services;
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
    builder.Services.AddSingleton<IInferenceService, MockInferenceService>();
    builder.Services.AddLogging(configure =>
    {
        configure.AddConsole();
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
        builder.Services.AddSingleton<WslManager>();
        builder.Services.AddSingleton<IimClient>();
        builder.Services.AddSingleton<TrayService>();

        return builder.Build();
    }
}
