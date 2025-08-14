using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Core.Platform;

/// <summary>
/// Interface for orchestrating services within WSL2
/// </summary>
public interface IWslServiceOrchestrator
{
    Task<ServiceStatus> GetServiceStatusAsync(string serviceName, CancellationToken ct = default);
    Task<bool> StartServiceAsync(string serviceName, ServiceConfig? config = null, CancellationToken ct = default);
    Task<bool> StopServiceAsync(string serviceName, CancellationToken ct = default);
    Task<bool> RestartServiceAsync(string serviceName, CancellationToken ct = default);
    Task<Dictionary<string, ServiceStatus>> GetAllServicesStatusAsync(CancellationToken ct = default);
    Task<bool> EnsureAllServicesAsync(CancellationToken ct = default);
}

/// <summary>
/// Orchestrates service lifecycle within WSL2 distributions
/// Manages Docker containers and Python services for AI workloads
/// </summary>
public sealed class WslServiceOrchestrator : IWslServiceOrchestrator, IHostedService, IDisposable
{
    private readonly ILogger<WslServiceOrchestrator> _logger;
    private readonly IWslManager _wslManager;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, ServiceInstance> _services = new();
    private readonly Timer _healthCheckTimer;
    private readonly SemaphoreSlim _serviceLock = new(1, 1);

    // Service configuration definitions
    private readonly Dictionary<string, ServiceConfig> _serviceConfigs = new()
    {
        ["qdrant"] = new ServiceConfig
        {
            Name = "qdrant",
            Type = ServiceType.Docker,
            DockerImage = "qdrant/qdrant:v1.12.4",
            Port = 6333,
            HealthEndpoint = "/",
            StartupCommand = "docker run -d --name qdrant -p 6333:6333 -v $HOME/qdrant:/qdrant/storage --restart always qdrant/qdrant:v1.12.4",
            RequiredMemoryMb = 2048,
            Priority = ServicePriority.Critical
        },
        ["embed"] = new ServiceConfig
        {
            Name = "embed",
            Type = ServiceType.Python,
            Port = 8081,
            HealthEndpoint = "/health",
            StartupCommand = "cd /opt/iim && python3 -m uvicorn embed_service:app --host 0.0.0.0 --port 8081",
            WorkingDirectory = "/opt/iim",
            RequiredMemoryMb = 1024,
            Priority = ServicePriority.Critical
        },
        ["ollama"] = new ServiceConfig
        {
            Name = "ollama",
            Type = ServiceType.Docker,
            DockerImage = "ollama/ollama:latest",
            Port = 11434,
            HealthEndpoint = "/api/tags",
            StartupCommand = "docker run -d --name ollama -p 11434:11434 -v ollama:/root/.ollama --restart always ollama/ollama:latest",
            RequiredMemoryMb = 4096,
            Priority = ServicePriority.High
        }
    };

    /// <summary>
    /// Initializes the service orchestrator
    /// </summary>
    public WslServiceOrchestrator(
        ILogger<WslServiceOrchestrator> logger,
        IWslManager wslManager,
        IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _wslManager = wslManager;
        _httpClient = httpFactory.CreateClient("wsl-services");
        _healthCheckTimer = new Timer(PerformHealthCheck, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Starts the orchestrator as a hosted service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting WSL Service Orchestrator");

        try
        {
            // Ensure WSL is ready
            var wslStatus = await _wslManager.GetStatusAsync(cancellationToken);
            if (!wslStatus.IsReady)
            {
                _logger.LogWarning("WSL not ready, attempting to initialize");
                // Fix: Pass distro name and cancellation token correctly
                await _wslManager.EnsureDistroAsync("IIM-Ubuntu", cancellationToken);
            }

            // Start critical services
            await EnsureAllServicesAsync(cancellationToken);

            // Start health monitoring
            _healthCheckTimer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger.LogInformation("WSL Service Orchestrator started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WSL Service Orchestrator");
            throw;
        }
    }

    /// <summary>
    /// Stops the orchestrator
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping WSL Service Orchestrator");

        _healthCheckTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        // Gracefully stop non-critical services
        foreach (var service in _services.Values.Where(s => s.Config.Priority != ServicePriority.Critical))
        {
            await StopServiceAsync(service.Config.Name, cancellationToken);
        }

        _logger.LogInformation("WSL Service Orchestrator stopped");
    }

    /// <summary>
    /// Gets the status of a specific service
    /// </summary>
    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, CancellationToken ct = default)
    {
        if (!_serviceConfigs.TryGetValue(serviceName, out var config))
        {
            return new ServiceStatus
            {
                Name = serviceName,
                State = ServiceState.NotFound,
                Message = "Service configuration not found"
            };
        }

        if (_services.TryGetValue(serviceName, out var instance))
        {
            return instance.Status;
        }

        // Check if running but not tracked
        var status = await CheckServiceStatusInWslAsync(config, ct);

        if (status.State == ServiceState.Running)
        {
            _services[serviceName] = new ServiceInstance
            {
                Config = config,
                Status = status,
                StartedAt = DateTimeOffset.UtcNow
            };
        }

        return status;
    }

    /// <summary>
    /// Starts a service with optional custom configuration
    /// </summary>
    public async Task<bool> StartServiceAsync(string serviceName, ServiceConfig? customConfig = null, CancellationToken ct = default)
    {
        await _serviceLock.WaitAsync(ct);
        try
        {
            var config = customConfig ?? (_serviceConfigs.TryGetValue(serviceName, out var c) ? c : null);
            if (config == null)
            {
                _logger.LogError("Service configuration not found for {Service}", serviceName);
                return false;
            }

            _logger.LogInformation("Starting service {Service}", serviceName);

            // Check if already running
            var currentStatus = await GetServiceStatusAsync(serviceName, ct);
            if (currentStatus.State == ServiceState.Running)
            {
                _logger.LogInformation("Service {Service} is already running", serviceName);
                return true;
            }

            // Ensure WSL network is ready
            var network = await _wslManager.GetNetworkInfoAsync("IIM-Ubuntu", ct);
            if (!network.IsConnected)
            {
                _logger.LogError("WSL network not ready");
                return false;
            }

            // Prepare and start service
            await PrepareServiceEnvironmentAsync(config, ct);

            bool started = config.Type switch
            {
                ServiceType.Docker => await StartDockerServiceAsync(config, network, ct),
                ServiceType.Python => await StartPythonServiceAsync(config, network, ct),
                ServiceType.Binary => await StartBinaryServiceAsync(config, network, ct),
                _ => false
            };

            if (started)
            {
                // Wait for service to be healthy
                var healthy = await WaitForServiceHealthyAsync(config, network, ct);

                if (healthy)
                {
                    var instance = new ServiceInstance
                    {
                        Config = config,
                        Status = new ServiceStatus
                        {
                            Name = serviceName,
                            State = ServiceState.Running,
                            IsHealthy = true,
                            Endpoint = $"http://{network.WslIpAddress}:{config.Port}",
                            Message = "Service started successfully"
                        },
                        StartedAt = DateTimeOffset.UtcNow,
                        ProcessId = await GetServicePidAsync(config, ct)
                    };

                    _services[serviceName] = instance;
                    _logger.LogInformation("Service {Service} started successfully at {Endpoint}",
                        serviceName, instance.Status.Endpoint);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Service {Service} started but not healthy", serviceName);
                    await StopServiceAsync(serviceName, ct);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start service {Service}", serviceName);
            return false;
        }
        finally
        {
            _serviceLock.Release();
        }
    }

    /// <summary>
    /// Stops a running service
    /// </summary>
    public async Task<bool> StopServiceAsync(string serviceName, CancellationToken ct = default)
    {
        await _serviceLock.WaitAsync(ct);
        try
        {
            if (!_services.TryGetValue(serviceName, out var instance))
            {
                _logger.LogWarning("Service {Service} not found in running services", serviceName);
                return true;
            }

            _logger.LogInformation("Stopping service {Service}", serviceName);

            bool stopped = instance.Config.Type switch
            {
                ServiceType.Docker => await StopDockerServiceAsync(instance.Config, ct),
                ServiceType.Python => await StopPythonServiceAsync(instance, ct),
                ServiceType.Binary => await StopBinaryServiceAsync(instance, ct),
                _ => false
            };

            if (stopped)
            {
                _services.TryRemove(serviceName, out _);
                _logger.LogInformation("Service {Service} stopped successfully", serviceName);
            }

            return stopped;
        }
        finally
        {
            _serviceLock.Release();
        }
    }

    /// <summary>
    /// Restarts a service
    /// </summary>
    public async Task<bool> RestartServiceAsync(string serviceName, CancellationToken ct = default)
    {
        _logger.LogInformation("Restarting service {Service}", serviceName);

        await StopServiceAsync(serviceName, ct);
        await Task.Delay(2000, ct);
        return await StartServiceAsync(serviceName, null, ct);
    }

    /// <summary>
    /// Gets status of all configured services
    /// </summary>
    public async Task<Dictionary<string, ServiceStatus>> GetAllServicesStatusAsync(CancellationToken ct = default)
    {
        var statuses = new Dictionary<string, ServiceStatus>();

        foreach (var serviceName in _serviceConfigs.Keys)
        {
            statuses[serviceName] = await GetServiceStatusAsync(serviceName, ct);
        }

        return statuses;
    }

    /// <summary>
    /// Ensures all critical services are running
    /// </summary>
    public async Task<bool> EnsureAllServicesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Ensuring all critical services are running");

        var results = new List<bool>();

        // Start services in priority order
        var sortedServices = _serviceConfigs
            .OrderByDescending(s => s.Value.Priority)
            .ToList();

        foreach (var (name, config) in sortedServices)
        {
            if (config.Priority >= ServicePriority.High)
            {
                var result = await StartServiceAsync(name, config, ct);
                results.Add(result);

                if (!result && config.Priority == ServicePriority.Critical)
                {
                    _logger.LogError("Failed to start critical service {Service}", name);
                    return false;
                }
            }
        }

        return results.All(r => r);
    }

    #region Private Helper Methods

    /// <summary>
    /// Prepares the environment for a service
    /// </summary>
    private async Task PrepareServiceEnvironmentAsync(ServiceConfig config, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(config.WorkingDirectory))
        {
            await ExecuteInWslAsync($"mkdir -p {config.WorkingDirectory}", ct);
        }

        if (config.Type == ServiceType.Docker && !string.IsNullOrEmpty(config.DockerImage))
        {
            await ExecuteInWslAsync($"docker pull {config.DockerImage}", ct);
        }
    }

    /// <summary>
    /// Starts a Docker-based service
    /// </summary>
    private async Task<bool> StartDockerServiceAsync(ServiceConfig config, WslNetworkInfo network, CancellationToken ct)
    {
        // Stop and remove existing container
        await ExecuteInWslAsync($"docker stop {config.Name} 2>/dev/null || true", ct);
        await ExecuteInWslAsync($"docker rm {config.Name} 2>/dev/null || true", ct);

        // Start new container
        var result = await ExecuteInWslAsync(config.StartupCommand, ct);
        return result.ExitCode == 0;
    }

    /// <summary>
    /// Starts a Python-based service
    /// </summary>
    private async Task<bool> StartPythonServiceAsync(ServiceConfig config, WslNetworkInfo network, CancellationToken ct)
    {
        // Kill existing process
        await ExecuteInWslAsync($"pkill -f '{config.Name}' || true", ct);

        // Start service
        var command = $@"
            cd {config.WorkingDirectory ?? "/opt/iim"}
            nohup {config.StartupCommand} > /var/log/{config.Name}.log 2>&1 &
            echo $!
        ";

        var result = await ExecuteInWslAsync(command, ct);
        return result.ExitCode == 0;
    }

    /// <summary>
    /// Starts a binary service
    /// </summary>
    private async Task<bool> StartBinaryServiceAsync(ServiceConfig config, WslNetworkInfo network, CancellationToken ct)
    {
        var result = await ExecuteInWslAsync($"nohup {config.StartupCommand} &", ct);
        return result.ExitCode == 0;
    }

    /// <summary>
    /// Stops a Docker service
    /// </summary>
    private async Task<bool> StopDockerServiceAsync(ServiceConfig config, CancellationToken ct)
    {
        var result = await ExecuteInWslAsync($"docker stop {config.Name}", ct);
        return result.ExitCode == 0;
    }

    /// <summary>
    /// Stops a Python service
    /// </summary>
    private async Task<bool> StopPythonServiceAsync(ServiceInstance instance, CancellationToken ct)
    {
        if (instance.ProcessId > 0)
        {
            var result = await ExecuteInWslAsync($"kill {instance.ProcessId}", ct);
            return result.ExitCode == 0;
        }

        var pkillResult = await ExecuteInWslAsync($"pkill -f '{instance.Config.Name}'", ct);
        return pkillResult.ExitCode == 0;
    }

    /// <summary>
    /// Stops a binary service
    /// </summary>
    private async Task<bool> StopBinaryServiceAsync(ServiceInstance instance, CancellationToken ct)
    {
        if (instance.ProcessId > 0)
        {
            var result = await ExecuteInWslAsync($"kill {instance.ProcessId}", ct);
            return result.ExitCode == 0;
        }
        return false;
    }

    /// <summary>
    /// Waits for a service to become healthy
    /// </summary>
    private async Task<bool> WaitForServiceHealthyAsync(ServiceConfig config, WslNetworkInfo network, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(network.WslIpAddress))
        {
            _logger.LogWarning("No WSL IP address available");
            return false;
        }

        var endpoint = $"http://{network.WslIpAddress}:{config.Port}{config.HealthEndpoint}";
        var maxRetries = 30;
        var delay = TimeSpan.FromSeconds(2);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var response = await _httpClient.GetAsync(endpoint, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Service {Service} is healthy", config.Name);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Health check attempt {Attempt} failed for {Service}: {Error}",
                    i + 1, config.Name, ex.Message);
            }

            await Task.Delay(delay, ct);
        }

        _logger.LogWarning("Service {Service} did not become healthy after {Retries} attempts",
            config.Name, maxRetries);
        return false;
    }

    /// <summary>
    /// Checks service status in WSL
    /// </summary>
    private async Task<ServiceStatus> CheckServiceStatusInWslAsync(ServiceConfig config, CancellationToken ct)
    {
        var status = new ServiceStatus
        {
            Name = config.Name,
            State = ServiceState.Stopped
        };

        try
        {
            if (config.Type == ServiceType.Docker)
            {
                var result = await ExecuteInWslAsync(
                    $"docker ps --filter name={config.Name} --format '{{{{.Status}}}}'", ct);
                if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                {
                    status.State = ServiceState.Running;

                    var network = await _wslManager.GetNetworkInfoAsync("IIM-Ubuntu", ct);
                    if (!string.IsNullOrEmpty(network.WslIpAddress))
                    {
                        status.Endpoint = $"http://{network.WslIpAddress}:{config.Port}";
                        status.IsHealthy = await CheckHealthAsync(status.Endpoint + config.HealthEndpoint, ct);
                    }
                }
            }
            else
            {
                var result = await ExecuteInWslAsync($"pgrep -f '{config.Name}'", ct);
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
                {
                    status.State = ServiceState.Running;

                    var network = await _wslManager.GetNetworkInfoAsync("IIM-Ubuntu", ct);
                    if (!string.IsNullOrEmpty(network.WslIpAddress))
                    {
                        status.Endpoint = $"http://{network.WslIpAddress}:{config.Port}";
                        status.IsHealthy = await CheckHealthAsync(status.Endpoint + config.HealthEndpoint, ct);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check status for service {Service}", config.Name);
            status.State = ServiceState.Error;
            status.Message = ex.Message;
        }

        return status;
    }

    /// <summary>
    /// Gets process ID of a service
    /// </summary>
    private async Task<int> GetServicePidAsync(ServiceConfig config, CancellationToken ct)
    {
        try
        {
            var result = await ExecuteInWslAsync($"pgrep -f '{config.Name}' | head -1", ct);
            if (int.TryParse(result.StandardOutput.Trim(), out var pid))
            {
                return pid;
            }
        }
        catch
        {
            // Ignore
        }
        return 0;
    }

    /// <summary>
    /// Checks if a service endpoint is healthy
    /// </summary>
    private async Task<bool> CheckHealthAsync(string endpoint, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var response = await _httpClient.GetAsync(endpoint, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Performs periodic health checks on services
    /// </summary>
    private async void PerformHealthCheck(object? state)
    {
        try
        {
            foreach (var service in _services.Values.ToList())
            {
                if (!string.IsNullOrEmpty(service.Status.Endpoint))
                {
                    var healthy = await CheckHealthAsync(
                        service.Status.Endpoint + service.Config.HealthEndpoint,
                        CancellationToken.None);

                    if (!healthy && service.Config.Priority >= ServicePriority.High)
                    {
                        _logger.LogWarning("Service {Service} is unhealthy, attempting restart",
                            service.Config.Name);
                        await RestartServiceAsync(service.Config.Name, CancellationToken.None);
                    }
                    else
                    {
                        service.Status.IsHealthy = healthy;
                        service.Status.LastHealthCheck = DateTimeOffset.UtcNow;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
        }
    }

    /// <summary>
    /// Executes a command in WSL
    /// </summary>
    private async Task<CommandResult> ExecuteInWslAsync(string command, CancellationToken ct)
    {
        // For cross-platform compatibility
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new CommandResult
            {
                ExitCode = 0,
                StandardOutput = "Mock output",
                StandardError = ""
            };
        }

        var psi = new ProcessStartInfo
        {
            FileName = "wsl",
            Arguments = $"-d IIM-Ubuntu -u root -- bash -c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to execute WSL command");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(ct);

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = output,
            StandardError = error
        };
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        _serviceLock?.Dispose();
    }

    #endregion
}

// Supporting types
public sealed class ServiceConfig
{
    public required string Name { get; init; }
    public required ServiceType Type { get; init; }
    public required int Port { get; init; }
    public string HealthEndpoint { get; init; } = "/health";
    public required string StartupCommand { get; init; }
    public string? WorkingDirectory { get; init; }
    public string? DockerImage { get; init; }
    public int RequiredMemoryMb { get; init; } = 512;
    public ServicePriority Priority { get; init; } = ServicePriority.Normal;
}

public sealed class ServiceInstance
{
    public required ServiceConfig Config { get; init; }
    public required ServiceStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; init; }
    public int ProcessId { get; set; }
}

public sealed class ServiceStatus
{
    public required string Name { get; init; }
    public ServiceState State { get; set; }
    public bool IsHealthy { get; set; }
    public string? Endpoint { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset? LastHealthCheck { get; set; }
}

public enum ServiceType
{
    Docker,
    Python,
    Binary
}

public enum ServiceState
{
    NotFound,
    Stopped,
    Starting,
    Running,
    Stopping,
    Error
}

public enum ServicePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}