using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Platform;

/// <summary>
/// Mock implementation of IWslManager for testing
/// </summary>
public sealed class MockWslManager : IWslManager
{
    private readonly ILogger<MockWslManager> _logger;
    private bool _isEnabled = true;
    private bool _distroExists = true;
    private bool _isRunning = false;

    public MockWslManager(ILogger<MockWslManager> logger)
    {
        _logger = logger;
    }

    public Task<WslStatus> GetStatusAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new WslStatus
        {
            IsInstalled = _isEnabled,
            IsWsl2 = _isEnabled,
            Version = "2.0.0",
            KernelVersion = "5.15.0",
            VirtualMachinePlatform = true,
            HyperV = true,
            HasIimDistro = _distroExists,
            IsReady = _isEnabled && _distroExists,
            Message = "Mock WSL status",
            InstalledDistros = _distroExists ? new List<WslDistro>
            {
                new WslDistro
                {
                    Name = "IIM-Ubuntu",
                    State = _isRunning ? WslDistroState.Running : WslDistroState.Stopped,
                    Version = "2"
                }
            } : new List<WslDistro>()
        });
    }

    public Task<WslDistro> EnsureDistroAsync(string distroName = "IIM-Ubuntu", CancellationToken ct = default)
    {
        _distroExists = true;
        _isRunning = true;
        return Task.FromResult(new WslDistro
        {
            Name = distroName,
            State = WslDistroState.Running,
            Version = "2",
            IpAddress = "172.24.1.2"
        });
    }

    public Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default)
    {
        _logger.LogInformation("Mock: Starting services in {Distro}", distro.Name);
        return Task.FromResult(true);
    }

    public Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default)
    {
        return Task.FromResult(new WslNetworkInfo
        {
            DistroName = distroName,
            WslIpAddress = "172.24.1.2",
            WindowsHostIp = "172.24.1.1",
            WindowsWslInterface = "vEthernet (WSL)",
            IsConnected = true,
            ServiceEndpoints = new Dictionary<string, string>
            {
                ["qdrant"] = "http://172.24.1.2:6333",
                ["embed"] = "http://172.24.1.2:8081",
                ["ollama"] = "http://172.24.1.2:11434",
                ["jupyterlab"] = "http://172.24.1.2:8888"
            }
        });
    }

    public Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default)
    {
        _logger.LogInformation("Mock: Syncing files from {Windows} to {Wsl}", windowsPath, wslPath);
        return Task.FromResult(true);
    }

    public Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default)
    {
        _logger.LogInformation("Mock: Installing distro {Name} from {Path}", installName, distroPath);
        _distroExists = true;
        return Task.FromResult(true);
    }

    public Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new HealthCheckResult
        {
            IsHealthy = _isEnabled && _distroExists && _isRunning,
            WslReady = _isEnabled,
            DistroRunning = _isRunning,
            ServicesHealthy = _isRunning,
            NetworkConnected = _isRunning,
            Issues = new List<string>(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public Task<bool> IsWslEnabled()
    {
        return Task.FromResult(_isEnabled);
    }

    public Task<bool> EnableWsl()
    {
        _logger.LogInformation("Mock: Enabling WSL");
        _isEnabled = true;
        return Task.FromResult(true);
    }

    public Task<bool> DistroExists(string distroName = "IIM-Ubuntu")
    {
        return Task.FromResult(_distroExists);
    }

    public Task<bool> StartIim()
    {
        _logger.LogInformation("Mock: Starting IIM");
        _isRunning = true;
        return Task.FromResult(true);
    }
}