using IIM.Core.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;

namespace IIM.Integration.Tests;

[Collection("Integration")]
public class WslIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IWslManager _wslManager;
    private readonly ILogger<WslIntegrationTests> _logger;

    public WslIntegrationTests(IntegrationTestFixture fixture)
    {
        _wslManager = fixture.ServiceProvider.GetRequiredService<IWslManager>();
        _logger = fixture.ServiceProvider.GetRequiredService<ILogger<WslIntegrationTests>>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WslManager_Should_DetectWslStatus()
    {
        // Act
        var status = await _wslManager.GetStatusAsync();

        // Assert
        status.Should().NotBeNull();
        _logger.LogInformation("WSL Status: {Status}", status.IsReady);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WslManager_Should_CheckHealth()
    {
        // Act
        var health = await _wslManager.HealthCheckAsync();

        // Assert
        health.Should().NotBeNull();
        health.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}

public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public IntegrationTestFixture()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
        });

        // Add services
        services.AddHttpClient("wsl");
        services.AddSingleton<IWslManager, MockWslManager>(); // Use mock for CI/CD

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}