using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Models;
using IIM.Infrastructure.Platform;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of IWslManager for unit testing
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

        public Task<bool> DistroExists(string distroName = "IIM-Ubuntu")
        {
            throw new NotImplementedException();
        }

        public Task<bool> EnableWsl()
        {
            throw new NotImplementedException();
        }

        public Task<WslDistro> EnsureDistroAsync(string distroName = "IIM-Ubuntu", CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default)
        {
            throw new NotImplementedException();
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

        public Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsWslEnabled()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StartIim()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        // ... rest of the implementation stays the same ...
    }
}