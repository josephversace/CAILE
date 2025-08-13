
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
        builder.Services.AddSingleton<WslManager>();
        builder.Services.AddSingleton<IimClient>();
        builder.Services.AddSingleton<TrayService>();

        return builder.Build();
    }
}
