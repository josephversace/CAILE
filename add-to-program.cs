// ============================================================================
// Add this to your Program.cs or MauiProgram.cs in the service configuration section
// This configures dependency injection to use mocks in development
// ============================================================================

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
