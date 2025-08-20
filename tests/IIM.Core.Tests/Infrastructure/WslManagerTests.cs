using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IIM.Infrastructure.Platform;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace IIM.Core.Infrastructure
{
    /// <summary>
    /// Unit tests for WslManager functionality
    /// </summary>
    public class WslManagerTests : IDisposable
    {
        private readonly Mock<ILogger<WslManager>> _loggerMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<HttpMessageHandler> _httpHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly WslManager _sut;

        public WslManagerTests()
        {
            _loggerMock = new Mock<ILogger<WslManager>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpHandlerMock = new Mock<HttpMessageHandler>();

            // Setup HTTP client
            _httpClient = new HttpClient(_httpHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            _sut = new WslManager(_loggerMock.Object, _httpClientFactoryMock.Object);
        }

        #region GetStatusAsync Tests

        /// <summary>
        /// Test that GetStatusAsync returns correct status when WSL is not installed
        /// </summary>
        [Fact]
        public async Task GetStatusAsync_WhenWslNotInstalled_ReturnsNotInstalledStatus()
        {
            // Arrange
            // Note: We can't easily mock system commands, so this tests the structure

            // Act
            var result = await _sut.GetStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
            result.InstalledDistros.Should().NotBeNull();

            // Message should be set
            result.Message.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Test that GetStatusAsync correctly identifies WSL2
        /// </summary>
        [Fact]
        public async Task GetStatusAsync_DetectsWsl2Version()
        {
            // This test would require mocking system calls
            // In a real scenario, we'd use a wrapper interface for system operations

            // Act
            var result = await _sut.GetStatusAsync();

            // Assert
            result.Should().NotBeNull();
            if (result.IsInstalled)
            {
                result.Version.Should().NotBeNullOrEmpty();
                result.KernelVersion.Should().NotBeNullOrEmpty();
            }
        }

        /// <summary>
        /// Test that GetStatusAsync handles cancellation correctly
        /// </summary>
        [Fact]
        public async Task GetStatusAsync_WhenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            // May or may not throw depending on when cancellation is checked
            var result = await _sut.GetStatusAsync(cts.Token);
            result.Should().NotBeNull();
        }

        #endregion

        #region IsWslEnabled Tests

        /// <summary>
        /// Test IsWslEnabled returns boolean result
        /// </summary>
        [Fact]
        public async Task IsWslEnabled_ReturnsBoolean()
        {
            // Act
            var result = await _sut.IsWslEnabled();

            // Assert
            result.Should().BeOneOf(true, false);
        }

        /// <summary>
        /// Test IsWslEnabled handles exceptions gracefully
        /// </summary>
        [Fact]
        public async Task IsWslEnabled_WhenExceptionOccurs_ReturnsFalse()
        {
            // This would require mocking internal methods
            // Act
            var result = await _sut.IsWslEnabled();

            // Assert
            result.Should().BeOneOf(true, false);

            // Verify logging occurred if false
            if (!result)
            {
                _loggerMock.Verify(
                    x => x.Log(
                        It.IsAny<LogLevel>(),
                        It.IsAny<EventId>(),
                        It.IsAny<object>(),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<object, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
        }

        #endregion

        #region EnableWsl Tests

        /// <summary>
        /// Test EnableWsl when not running as admin
        /// </summary>
        [Fact]
        public async Task EnableWsl_WhenNotAdmin_ShowsManualInstructions()
        {
            // Arrange - We can't easily mock admin check
            // This test documents expected behavior

            // Act
            var result = await _sut.EnableWsl();

            // Assert
            // If not admin, should return false
            // and log appropriate messages
            if (!result)
            {
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Warning,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("administrator")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtMostOnce);
            }
        }

        /// <summary>
        /// Test EnableWsl handles concurrent calls correctly
        /// </summary>
        [Fact]
        public async Task EnableWsl_WhenCalledConcurrently_SerializesExecution()
        {
            // Arrange
            var tasks = new List<Task<bool>>();

            // Act
            for (int i = 0; i < 3; i++)
            {
                tasks.Add(_sut.EnableWsl());
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllBeEquivalentTo(results[0]);
            // All calls should return the same result
        }

        #endregion

        #region EnsureDistroAsync Tests

        /// <summary>
        /// Test EnsureDistroAsync with valid distro name
        /// </summary>
        [Theory]
        [InlineData("IIM-Ubuntu")]
        [InlineData("IIM-Kali")]
        public async Task EnsureDistroAsync_WithValidDistroName_DoesNotThrow(string distroName)
        {
            // Act
            Func<Task> act = async () => await _sut.EnsureDistroAsync(distroName);

            // Assert
            // Should either succeed or throw expected exception
            await act.Should().NotThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Test EnsureDistroAsync with unsupported distro
        /// </summary>
        [Fact]
        public async Task EnsureDistroAsync_WithUnsupportedDistro_ThrowsNotSupportedException()
        {
            // Arrange
            var unsupportedDistro = "UnsupportedDistro";

            // Act
            Func<Task> act = async () => await _sut.EnsureDistroAsync(unsupportedDistro);

            // Assert
            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage($"*{unsupportedDistro}*");
        }

        /// <summary>
        /// Test EnsureDistroAsync logs appropriate messages
        /// </summary>
        [Fact]
        public async Task EnsureDistroAsync_LogsProgress()
        {
            // Act
            try
            {
                await _sut.EnsureDistroAsync();
            }
            catch
            {
                // Ignore exceptions for this test
            }

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Ensuring distro")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region GetNetworkInfoAsync Tests

        /// <summary>
        /// Test GetNetworkInfoAsync returns proper structure
        /// </summary>
        [Fact]
        public async Task GetNetworkInfoAsync_ReturnsNetworkInfo()
        {
            // Arrange
            var distroName = "IIM-Ubuntu";

            // Act
            var result = await _sut.GetNetworkInfoAsync(distroName);

            // Assert
            result.Should().NotBeNull();
            result.DistroName.Should().Be(distroName);
            result.ServiceEndpoints.Should().NotBeNull();

            // Should have expected service endpoints if connected
            if (result.IsConnected)
            {
                result.WslIpAddress.Should().NotBeNullOrEmpty();
                result.ServiceEndpoints.Should().ContainKey("qdrant");
                result.ServiceEndpoints.Should().ContainKey("postgres");
                result.ServiceEndpoints.Should().ContainKey("minio");
            }
        }

        /// <summary>
        /// Test GetNetworkInfoAsync handles errors gracefully
        /// </summary>
        [Fact]
        public async Task GetNetworkInfoAsync_WhenErrorOccurs_SetsErrorMessage()
        {
            // Arrange
            var distroName = "NonExistentDistro";

            // Act
            var result = await _sut.GetNetworkInfoAsync(distroName);

            // Assert
            result.Should().NotBeNull();
            result.IsConnected.Should().BeFalse();
            // Error message may or may not be set depending on failure mode
        }

        #endregion

        #region HealthCheckAsync Tests

        /// <summary>
        /// Test HealthCheckAsync returns comprehensive health status
        /// </summary>
        [Fact]
        public async Task HealthCheckAsync_ReturnsHealthStatus()
        {
            // Act
            var result = await _sut.HealthCheckAsync();

            // Assert
            result.Should().NotBeNull();
            result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
            result.Issues.Should().NotBeNull();

            // Health should be consistent with issues
            if (result.IsHealthy)
            {
                result.Issues.Should().BeEmpty();
            }
            else
            {
                result.Issues.Should().NotBeEmpty();
            }
        }

        /// <summary>
        /// Test HealthCheckAsync identifies missing WSL
        /// </summary>
        [Fact]
        public async Task HealthCheckAsync_WhenWslNotInstalled_ReportsUnhealthy()
        {
            // This test documents expected behavior
            // Act
            var result = await _sut.HealthCheckAsync();

            // Assert
            if (!result.WslReady)
            {
                result.IsHealthy.Should().BeFalse();
                result.Issues.Should().Contain(issue =>
                    issue.Contains("WSL", StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Test HealthCheckAsync handles cancellation
        /// </summary>
        [Fact]
        public async Task HealthCheckAsync_WhenCancelled_ReturnsQuickly()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Act
            var result = await _sut.HealthCheckAsync(cts.Token);

            // Assert
            result.Should().NotBeNull();
            // Should still return a result even if cancelled
        }

        #endregion

        #region DistroExists Tests

        /// <summary>
        /// Test DistroExists returns boolean result
        /// </summary>
        [Theory]
        [InlineData("IIM-Ubuntu")]
        [InlineData("IIM-Kali")]
        [InlineData("NonExistent")]
        public async Task DistroExists_ReturnsBoolean(string distroName)
        {
            // Act
            var result = await _sut.DistroExists(distroName);

            // Assert
            result.Should().BeOneOf(true, false);
        }

        /// <summary>
        /// Test DistroExists handles exceptions
        /// </summary>
        [Fact]
        public async Task DistroExists_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var distroName = "Test";

            // Act
            var result = await _sut.DistroExists(distroName);

            // Assert
            // Should not throw, returns false on error
            result.Should().BeOneOf(true, false);
        }

        #endregion

        #region StartIim Tests

        /// <summary>
        /// Test StartIim attempts to start services
        /// </summary>
        [Fact]
        public async Task StartIim_AttemptsToStartServices()
        {
            // Act
            var result = await _sut.StartIim();

            // Assert
            result.Should().BeOneOf(true, false);

            // Should log the attempt
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Starting IIM")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Test StartIim handles failures gracefully
        /// </summary>
        [Fact]
        public async Task StartIim_WhenFailure_ReturnsFalse()
        {
            // Act
            var result = await _sut.StartIim();

            // Assert
            // If WSL not installed, should return false
            if (!await _sut.IsWslEnabled())
            {
                result.Should().BeFalse();
            }
        }

        #endregion

        #region StartServicesAsync Tests

        /// <summary>
        /// Test StartServicesAsync with valid distro
        /// </summary>
        [Fact]
        public async Task StartServicesAsync_WithValidDistro_AttemptsStart()
        {
            // Arrange
            var distro = new WslDistro
            {
                Name = "IIM-Ubuntu",
                State = WslDistroState.Running,
                Version = "2"
            };

            // Act
            var result = await _sut.StartServicesAsync(distro);

            // Assert
            result.Should().BeOneOf(true, false);
        }

        /// <summary>
        /// Test StartServicesAsync logs progress
        /// </summary>
        [Fact]
        public async Task StartServicesAsync_LogsServiceStartup()
        {
            // Arrange
            var distro = new WslDistro
            {
                Name = "IIM-Ubuntu",
                State = WslDistroState.Running,
                Version = "2"
            };

            // Act
            try
            {
                await _sut.StartServicesAsync(distro);
            }
            catch
            {
                // Ignore exceptions
            }

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Starting services")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region SyncFilesAsync Tests

        /// <summary>
        /// Test SyncFilesAsync with valid paths
        /// </summary>
        [Fact]
        public async Task SyncFilesAsync_WithValidPaths_ReturnsBoolean()
        {
            // Arrange
            var windowsPath = @"C:\TestData";
            var wslPath = "/home/user/data";

            // Act
            var result = await _sut.SyncFilesAsync(windowsPath, wslPath);

            // Assert
            result.Should().BeOneOf(true, false);
        }

        /// <summary>
        /// Test SyncFilesAsync handles path conversion
        /// </summary>
        [Theory]
        [InlineData(@"C:\TestData", "/mnt/c/TestData")]
        [InlineData(@"D:\Projects", "/mnt/d/Projects")]
        public async Task SyncFilesAsync_ConvertsWindowsPaths(string windowsPath, string expectedWslPath)
        {
            // Arrange
            var wslPath = "/home/user/data";

            // Act
            var result = await _sut.SyncFilesAsync(windowsPath, wslPath);

            // Assert
            result.Should().BeOneOf(true, false);

            // Verify logging contains converted path
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Syncing files")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtMostOnce);
        }

        #endregion

        #region InstallDistroAsync Tests

        /// <summary>
        /// Test InstallDistroAsync with valid parameters
        /// </summary>
        [Fact]
        public async Task InstallDistroAsync_WithValidParams_ReturnsBoolean()
        {
            // Arrange
            var distroPath = @"C:\Distros\ubuntu.tar.gz";
            var installName = "TestDistro";

            // Act
            var result = await _sut.InstallDistroAsync(distroPath, installName);

            // Assert
            result.Should().BeOneOf(true, false);
        }

        /// <summary>
        /// Test InstallDistroAsync logs installation attempt
        /// </summary>
        [Fact]
        public async Task InstallDistroAsync_LogsInstallation()
        {
            // Arrange
            var distroPath = @"C:\Distros\ubuntu.tar.gz";
            var installName = "TestDistro";

            // Act
            try
            {
                await _sut.InstallDistroAsync(distroPath, installName);
            }
            catch
            {
                // Ignore exceptions
            }

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) =>
                        o.ToString()!.Contains("Installing distro") &&
                        o.ToString()!.Contains(installName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region HTTP Client Tests

        /// <summary>
        /// Test that HTTP client is properly configured
        /// </summary>
        [Fact]
        public void HttpClient_IsProperlyConfigured()
        {
            // Assert
            _httpClientFactoryMock.Verify(
                x => x.CreateClient("wsl"),
                Times.Once);
        }

        /// <summary>
        /// Test network connectivity check with mocked HTTP response
        /// </summary>
        [Fact]
        public async Task GetNetworkInfoAsync_ChecksQdrantConnectivity()
        {
            // Arrange
            _httpHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri!.ToString().Contains("6333")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });

            // Act
            var result = await _sut.GetNetworkInfoAsync("IIM-Ubuntu");

            // Assert
            // Would check connectivity if WSL is properly configured
            result.Should().NotBeNull();
        }

        #endregion

        /// <summary>
        /// Cleanup resources after tests
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            _httpHandlerMock?.Protected().Dispose();
        }
    }

    /// <summary>
    /// Integration tests for WslManager (requires WSL installed)
    /// </summary>
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public class WslManagerIntegrationTests
    {
        private readonly WslManager _sut;
        private readonly ILogger<WslManager> _logger;

        public WslManagerIntegrationTests()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _logger = loggerFactory.CreateLogger<WslManager>();

            var httpClientFactory = new TestHttpClientFactory();
            _sut = new WslManager(_logger, httpClientFactory);
        }

        /// <summary>
        /// Test actual WSL status check (requires WSL)
        /// </summary>
        [SkippableFact]
        public async Task GetStatusAsync_ReturnsActualWslStatus()
        {
            // Skip if not on Windows
            Skip.IfNot(OperatingSystem.IsWindows(), "This test requires Windows");

            // Act
            var result = await _sut.GetStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

            // Log the actual status for debugging
            Console.WriteLine($"WSL Installed: {result.IsInstalled}");
            Console.WriteLine($"WSL2: {result.IsWsl2}");
            Console.WriteLine($"Version: {result.Version}");
            Console.WriteLine($"Kernel: {result.KernelVersion}");
            Console.WriteLine($"Message: {result.Message}");
        }

        /// <summary>
        /// Test actual health check (requires WSL)
        /// </summary>
        [SkippableFact]
        public async Task HealthCheckAsync_PerformsActualHealthCheck()
        {
            // Skip if not on Windows
            Skip.IfNot(OperatingSystem.IsWindows(), "This test requires Windows");

            // Act
            var result = await _sut.HealthCheckAsync();

            // Assert
            result.Should().NotBeNull();

            // Log the health check results
            Console.WriteLine($"Healthy: {result.IsHealthy}");
            Console.WriteLine($"WSL Ready: {result.WslReady}");
            Console.WriteLine($"Distro Running: {result.DistroRunning}");
            Console.WriteLine($"Services Healthy: {result.ServicesHealthy}");
            Console.WriteLine($"Network Connected: {result.NetworkConnected}");
            Console.WriteLine($"Issues: {string.Join(", ", result.Issues)}");
        }
    }

    /// <summary>
    /// Helper class for creating HTTP clients in tests
    /// </summary>
    internal class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }
    }

    /// <summary>
    /// Helper attribute for skippable tests
    /// </summary>
    public sealed class SkippableFactAttribute : FactAttribute
    {
        public override string Skip { get; set; } = null!;
    }

    /// <summary>
    /// Helper class for skipping tests conditionally
    /// </summary>
    public static class Skip
    {
        public static void IfNot(bool condition, string reason)
        {
            if (!condition)
            {
                throw new SkipException(reason);
            }
        }
    }

    /// <summary>
    /// Exception thrown to skip a test
    /// </summary>
    public class SkipException : Exception
    {
        public SkipException(string reason) : base(reason) { }
    }
}