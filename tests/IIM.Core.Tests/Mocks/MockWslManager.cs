// ============================================
// File: tests/IIM.Core.Tests/Mocks/MockWslManager.cs
// Purpose: Complete mock implementation of IWslManager for testing
// Author: IIM Platform Team
// Created: 2024
// ============================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Infrastructure.Platform;
using IIM.Infrastructure.Platform.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Tests.Mocks
{
    /// <summary>
    /// Complete mock implementation of IWslManager for unit testing.
    /// Provides configurable responses without actual WSL operations.
    /// </summary>
    public sealed class MockWslManager : IWslManager
    {
        private readonly ILogger<MockWslManager> _logger;
        private bool _isEnabled = true;
        private bool _distroExists = true;
        private bool _isRunning = false;
        private bool _servicesHealthy = true;
        private readonly Dictionary<string, WslDistro> _distros = new();
        private readonly Dictionary<string, string> _configuration = new();
        private readonly Dictionary<string, ServiceHealthCheck> _serviceHealth = new();

        public MockWslManager(ILogger<MockWslManager> logger)
        {
            _logger = logger;
            InitializeDefaults();
        }

        // Configuration methods for test setup
        public void SetWslEnabled(bool enabled) => _isEnabled = enabled;
        public void SetDistroExists(bool exists) => _distroExists = exists;
        public void SetDistroRunning(bool running) => _isRunning = running;
        public void SetServicesHealthy(bool healthy) => _servicesHealthy = healthy;

        private void InitializeDefaults()
        {
            // Initialize default distros
            _distros["IIM-Ubuntu"] = new WslDistro
            {
                Name = "IIM-Ubuntu",
                State = WslDistroState.Stopped,
                Version = "2",
                InstallPath = "C:\\IIM\\WSL\\IIM-Ubuntu"
            };

            // Update state based on flag
            if (_isRunning)
            {
                _distros["IIM-Ubuntu"].State = WslDistroState.Running;
            }

            // Initialize default configuration
            _configuration["memory"] = "64GB";
            _configuration["processors"] = "8";
            _configuration["wsl.conf.systemd"] = "true";

            // Initialize service health
            var services = new[] { "qdrant", "postgres", "minio", "mcp-server" };
            foreach (var service in services)
            {
                _serviceHealth[service] = new ServiceHealthCheck
                {
                    ServiceName = service,
                    IsHealthy = true,
                    Details = "Mock service healthy",
                    ResponseTimeMs = 100,
                    Port = service switch
                    {
                        "qdrant" => 6333,
                        "postgres" => 5432,
                        "minio" => 9000,
                        "mcp-server" => 3000,
                        _ => 0
                    }
                };
            }
        }

        // ========================================
        // Core WSL Operations
        // ========================================

        public Task<WslStatus> GetStatusAsync(CancellationToken ct = default)
        {
            _logger.LogDebug("Mock: Getting WSL status");

            return Task.FromResult(new WslStatus
            {
                IsInstalled = _isEnabled,
                IsWsl2 = _isEnabled,
                Version = "2.0.0",
                KernelVersion = "5.15.0",
                VirtualMachinePlatform = true,
                HyperV = true,
                HasIimDistro = _distroExists,
                IsReady = _isEnabled && _distroExists && _isRunning,
                Message = _isEnabled ? "Mock WSL ready" : "Mock WSL not installed",
                InstalledDistros = _distros.Values.ToList(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        public Task<bool> IsWslEnabled()
        {
            _logger.LogDebug("Mock: Checking if WSL is enabled");
            return Task.FromResult(_isEnabled);
        }

        public Task<bool> EnableWsl()
        {
            _logger.LogInformation("Mock: Enabling WSL");
            _isEnabled = true;
            return Task.FromResult(true);
        }

        // ========================================
        // Distribution Management
        // ========================================

        public Task<WslDistro> EnsureDistroAsync(string distroName = "IIM-Ubuntu", CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Ensuring distro {Distro}", distroName);

            if (!_distros.ContainsKey(distroName))
            {
                _distros[distroName] = new WslDistro
                {
                    Name = distroName,
                    State = WslDistroState.Running,
                    Version = "2",
                    IpAddress = "172.20.0.2",
                    InstallPath = $"C:\\IIM\\WSL\\{distroName}"
                };
            }

            if (_isRunning)
            {
                _distros[distroName].State = WslDistroState.Running;
            }

            return Task.FromResult(_distros[distroName]);
        }

        public Task<bool> DistroExists(string distroName = "IIM-Ubuntu")
        {
            _logger.LogDebug("Mock: Checking if distro {Distro} exists", distroName);
            return Task.FromResult(_distros.ContainsKey(distroName));
        }

        public Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Installing distro {Name} from {Path}", installName, distroPath);

            _distros[installName] = new WslDistro
            {
                Name = installName,
                State = WslDistroState.Stopped,
                Version = "2",
                InstallPath = $"C:\\IIM\\WSL\\{installName}"
            };

            return Task.FromResult(true);
        }

        // ========================================
        // Service Management
        // ========================================

        public Task<bool> StartIim()
        {
            _logger.LogInformation("Mock: Starting IIM services");
            _isRunning = true;

            foreach (var distro in _distros.Values)
            {
                distro.State = WslDistroState.Running;
            }

            return Task.FromResult(true);
        }

        public Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Starting services in {Distro}", distro.Name);
            return Task.FromResult(_servicesHealthy);
        }

        public Task<bool> StopServicesAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Stopping services");
            _isRunning = false;
            return Task.FromResult(true);
        }

        public Task<bool> RestartServiceAsync(string serviceName, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Restarting service {Service}", serviceName);
            return Task.FromResult(true);
        }

        // ========================================
        // Network and Connectivity
        // ========================================

        public Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default)
        {
            _logger.LogDebug("Mock: Getting network info for {Distro}", distroName);

            return Task.FromResult(new WslNetworkInfo
            {
                DistroName = distroName,
                WslIpAddress = "172.20.0.2",
                WindowsHostIp = "172.20.0.1",
                WindowsWslInterface = "172.20.0.1",
                IsConnected = _isRunning,
                LatencyMs = 1.5,
                ServiceEndpoints = new Dictionary<string, string>
                {
                    ["qdrant"] = "http://172.20.0.1:6333",
                    ["postgres"] = "http://172.20.0.1:5432",
                    ["minio"] = "http://172.20.0.1:9000",
                    ["mcp-server"] = "http://172.20.0.1:3000"
                }
            });
        }

        public Task InstallTorAndApplyProxyAsync(string windowsProxyPath, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Installing Tor and applying proxy from {Path}", windowsProxyPath);
            return Task.CompletedTask;
        }

        public Task<bool> ConfigureProxyAsync(ProxyConfig config, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Configuring proxy");
            return Task.FromResult(true);
        }

        // ========================================
        // Health Monitoring
        // ========================================

        public Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
        {
            _logger.LogDebug("Mock: Performing health check");

            var issues = new List<string>();
            if (!_isEnabled) issues.Add("WSL not enabled");
            if (!_distroExists) issues.Add("Distro not found");
            if (!_isRunning) issues.Add("Services not running");
            if (!_servicesHealthy) issues.Add("Some services unhealthy");

            return Task.FromResult(new HealthCheckResult
            {
                IsHealthy = _isEnabled && _distroExists && _isRunning && _servicesHealthy,
                WslReady = _isEnabled,
                DistroRunning = _isRunning,
                ServicesHealthy = _servicesHealthy,
                NetworkConnected = _isRunning,
                ServiceChecks = _serviceHealth.Values.ToList(),
                Issues = issues,
                Timestamp = DateTimeOffset.UtcNow,
                ElapsedMs = 150
            });
        }

        public Task<ServiceHealthCheck> CheckServiceHealthAsync(string serviceName, CancellationToken ct = default)
        {
            _logger.LogDebug("Mock: Checking health of {Service}", serviceName);

            if (_serviceHealth.TryGetValue(serviceName, out var health))
            {
                return Task.FromResult(health);
            }

            return Task.FromResult(new ServiceHealthCheck
            {
                ServiceName = serviceName,
                IsHealthy = false,
                Details = "Service not found"
            });
        }

        // ========================================
        // File Operations
        // ========================================

        public Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Syncing files from {Windows} to {WSL}", windowsPath, wslPath);
            return Task.FromResult(true);
        }

        public Task<bool> SyncFilesAsync(FileSyncConfig config, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Advanced sync from {Windows} to {WSL}",
                config.WindowsPath, config.WslPath);

            // Simulate progress callbacks if provided
            if (config.ProgressCallback != null)
            {
                for (int i = 0; i <= 100; i += 20)
                {
                    config.ProgressCallback(new FileSyncProgress
                    {
                        ProgressPercent = i,
                        BytesTransferred = i * 1024 * 1024,
                        TotalBytes = 100 * 1024 * 1024,
                        FilesCompleted = i,
                        TotalFiles = 100,
                        CurrentFile = $"file_{i}.txt"
                    });
                }
            }

            return Task.FromResult(true);
        }

        public Task<bool> CopyFileToWslAsync(string windowsFilePath, string wslFilePath, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Copying file from {Windows} to {WSL}", windowsFilePath, wslFilePath);
            return Task.FromResult(true);
        }

        public Task<bool> CopyFileFromWslAsync(string wslFilePath, string windowsFilePath, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Copying file from {WSL} to {Windows}", wslFilePath, windowsFilePath);
            return Task.FromResult(true);
        }

        // ========================================
        // Command Execution
        // ========================================

        public Task<CommandResult> ExecuteCommandAsync(string command, CancellationToken ct = default)
        {
            return ExecuteCommandAsync("IIM-Ubuntu", command, ct);
        }

        public Task<CommandResult> ExecuteCommandAsync(string distroName, string command, CancellationToken ct = default)
        {
            _logger.LogDebug("Mock: Executing command in {Distro}: {Command}", distroName, command);

            return Task.FromResult(new CommandResult
            {
                ExitCode = 0,
                StandardOutput = $"Mock output for: {command}",
                StandardError = string.Empty,
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            });
        }

        public Task<int> ExecuteCommandWithStreamingAsync(string distroName, string command,
            Action<string> outputCallback, CancellationToken ct = default)
        {
            _logger.LogDebug("Mock: Streaming command in {Distro}: {Command}", distroName, command);

            // Simulate streaming output
            outputCallback($"Starting: {command}");
            outputCallback("Processing...");
            outputCallback("Complete.");

            return Task.FromResult(0);
        }

        // ========================================
        // Docker Management
        // ========================================

        public Task<bool> EnsureDockerReadyAsync(CancellationToken ct = default)
        {
            _logger.LogDebug("Mock: Ensuring Docker is ready");
            return Task.FromResult(_isRunning);
        }

        public Task<string?> StartDockerContainerAsync(DockerServiceConfig config, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Starting container {Name}", config.Name);
            return Task.FromResult<string?>($"mock-container-{config.Name}-{Guid.NewGuid():N}");
        }

        public Task<bool> StopDockerContainerAsync(string containerName, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Stopping container {Name}", containerName);
            return Task.FromResult(true);
        }

        public Task<Dictionary<string, string>> GetDockerContainerStatusAsync(CancellationToken ct = default)
        {
            _logger.LogDebug("Mock: Getting Docker container status");

            return Task.FromResult(new Dictionary<string, string>
            {
                ["iim-qdrant"] = _isRunning ? "Up 2 hours" : "Exited",
                ["iim-postgres"] = _isRunning ? "Up 2 hours" : "Exited",
                ["iim-minio"] = _isRunning ? "Up 2 hours" : "Exited",
                ["iim-mcp-server"] = _isRunning ? "Up 2 hours" : "Exited"
            });
        }

        // ========================================
        // Maintenance and Cleanup
        // ========================================

        public Task<long> CleanupAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Performing cleanup");
            return Task.FromResult(1024L * 1024 * 100); // Mock 100MB freed
        }

        public Task<bool> ExportDistroAsync(string distroName, string exportPath, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Exporting {Distro} to {Path}", distroName, exportPath);
            return Task.FromResult(true);
        }

        public Task<bool> ImportDistroAsync(string tarPath, string distroName, string installPath, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Importing {Distro} from {Path}", distroName, tarPath);

            _distros[distroName] = new WslDistro
            {
                Name = distroName,
                State = WslDistroState.Stopped,
                Version = "2",
                InstallPath = installPath
            };

            return Task.FromResult(true);
        }

        public Task<bool> RemoveDistroAsync(string distroName, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Removing distro {Distro}", distroName);
            _distros.Remove(distroName);
            return Task.FromResult(true);
        }

        // ========================================
        // Configuration
        // ========================================

        public Task<Dictionary<string, string>> GetConfigurationAsync(CancellationToken ct = default)
        {
            _logger.LogDebug("Mock: Getting configuration");
            return Task.FromResult(new Dictionary<string, string>(_configuration));
        }

        public Task<bool> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Updating configuration");

            foreach (var (key, value) in settings)
            {
                _configuration[key] = value;
            }

            return Task.FromResult(true);
        }

        public Task<bool> SetMemoryLimitAsync(int memoryGb, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Setting memory limit to {Memory}GB", memoryGb);
            _configuration["memory"] = $"{memoryGb}GB";
            return Task.FromResult(true);
        }

        public Task<bool> SetCpuLimitAsync(int cpuCount, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock: Setting CPU limit to {Cpus} cores", cpuCount);
            _configuration["processors"] = cpuCount.ToString();
            return Task.FromResult(true);
        }
    }
}