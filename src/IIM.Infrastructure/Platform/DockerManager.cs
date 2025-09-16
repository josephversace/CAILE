using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace IIM.Infrastructure.Platform
{
    public class DockerManager : IWslManager
    {
        private readonly ILogger<DockerManager> _logger;
        private readonly string _composeFile;

        public DockerManager(ILogger<DockerManager> logger)
        {
            _logger = logger;
            _composeFile = Environment.GetEnvironmentVariable("DOCKER_COMPOSE_FILE") ?? "docker-compose.yml";
        }

        // Core service management - ESSENTIAL for data router
        public async Task<bool> StartIim()
        {
            _logger.LogInformation("Starting IIM services via Docker Compose");
            var result = await ExecuteDockerComposeAsync("up -d");
            return result.IsSuccess;
        }

        public async Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default)
        {
            _logger.LogInformation("Starting services for distro: {DistroName}", distro.Name);
            var result = await ExecuteDockerComposeAsync("up -d");
            return result.IsSuccess;
        }

        public async Task<bool> StopServicesAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Stopping all IIM services");
            var result = await ExecuteDockerComposeAsync("down");
            return result.IsSuccess;
        }

        public async Task<bool> RestartServiceAsync(string serviceName, CancellationToken ct = default)
        {
            _logger.LogInformation("Restarting service: {ServiceName}", serviceName);
            var result = await ExecuteDockerComposeAsync($"restart {serviceName}");
            return result.IsSuccess;
        }

        // Health checking - ESSENTIAL for monitoring
        public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
        {
            _logger.LogDebug("Performing Docker health check");

            var dockerCheck = await ExecuteDockerAsync("version");
            if (!dockerCheck.IsSuccess)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = "Docker is not available",
                    CheckedAt = DateTime.UtcNow
                };
            }

            var composeCheck = await ExecuteDockerComposeAsync("ps --services");
            return new HealthCheckResult
            {
                IsHealthy = composeCheck.IsSuccess,
                Message = composeCheck.IsSuccess ? "All services healthy" : "Some services are down",
                CheckedAt = DateTime.UtcNow
            };
        }

        public async Task<ServiceHealthCheck> CheckServiceHealthAsync(string serviceName, CancellationToken ct = default)
        {
            _logger.LogDebug("Checking health of service: {ServiceName}", serviceName);

            var result = await ExecuteDockerComposeAsync($"ps {serviceName}");
            var isRunning = result.IsSuccess && result.StandardOutput.Contains("Up");

            return new ServiceHealthCheck
            {
                ServiceName = serviceName,
                IsHealthy = isRunning,
                Status = isRunning ? "Running" : "Stopped",
                LastChecked = DateTime.UtcNow,
                Details = result.StandardOutput
            };
        }

        // Status and configuration - ESSENTIAL for monitoring
        public async Task<WslStatus> GetStatusAsync(CancellationToken ct = default)
        {
            var composeResult = await ExecuteDockerComposeAsync("ps --format json");
            var dockerResult = await ExecuteDockerAsync("info --format json");

            return new WslStatus
            {
                IsRunning = composeResult.IsSuccess,
                Services = await ParseDockerComposeServicesAsync(composeResult.StandardOutput),
                LastChecked = DateTime.UtcNow
            };
        }

        public async Task<Dictionary<string, string>> GetDockerContainerStatusAsync(CancellationToken ct = default)
        {
            var result = await ExecuteDockerComposeAsync("ps --format json");
            var statuses = new Dictionary<string, string>();

            if (result.IsSuccess && !string.IsNullOrEmpty(result.StandardOutput))
            {
                try
                {
                    var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var container = JsonSerializer.Deserialize<DockerContainer>(line);
                        if (container != null)
                        {
                            statuses[container.Service] = container.State;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Docker container status JSON");
                }
            }

            return statuses;
        }

        // Command execution - ESSENTIAL for operations
        public async Task<CommandResult> ExecuteCommandAsync(string command, CancellationToken ct = default)
        {
            return await ExecuteDockerAsync(command);
        }

        public async Task<CommandResult> ExecuteCommandAsync(string distroName, string command, CancellationToken ct = default)
        {
            // For Docker, execute commands in the specified service container
            return await ExecuteDockerComposeAsync($"exec {distroName} {command}");
        }

        public async Task<int> ExecuteCommandWithStreamingAsync(string distroName, string command, Action<string> outputCallback, CancellationToken ct = default)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker-compose",
                        Arguments = $"-f {_composeFile} exec {distroName} {command}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        outputCallback(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();

                await process.WaitForExitAsync(ct);
                return process.ExitCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing streaming command: {Command}", command);
                return -1;
            }
        }

        // Docker-specific operations - ESSENTIAL for container management
        public async Task<bool> EnsureDockerReadyAsync(CancellationToken ct = default)
        {
            var result = await ExecuteDockerAsync("version");
            if (!result.IsSuccess)
            {
                _logger.LogError("Docker is not available: {Error}", result.ErrorMessage);
                return false;
            }

            var composeResult = await ExecuteAsync("docker-compose", "--version");
            if (!composeResult.IsSuccess)
            {
                _logger.LogError("Docker Compose is not available: {Error}", composeResult.ErrorMessage);
                return false;
            }

            return true;
        }

        public async Task<string?> StartDockerContainerAsync(DockerServiceConfig config, CancellationToken ct = default)
        {
            var args = $"run -d --name {config.ServiceName}";

            foreach (var port in config.Ports)
                args += $" -p {port}";

            foreach (var volume in config.Volumes)
                args += $" -v {volume}";

            foreach (var env in config.Environment)
                args += $" -e {env.Key}={env.Value}";

            args += $" {config.ImageName}";

            var result = await ExecuteDockerAsync(args);
            return result.IsSuccess ? result.StandardOutput.Trim() : null;
        }

        public async Task<bool> StopDockerContainerAsync(string containerName, CancellationToken ct = default)
        {
            var result = await ExecuteDockerAsync($"stop {containerName}");
            return result.IsSuccess;
        }

        // Simplified implementations for non-essential methods
        public async Task<bool> IsWslEnabled() => true; // Docker is always "enabled" if installed

        public async Task<bool> EnableWsl() => true; // No-op for Docker

        public async Task<bool> DistroExists(string distroName = "IIM-Ubuntu") => true; // Containers exist

        public async Task<WslDistro> EnsureDistroAsync(string distroName = "IIM-Ubuntu", CancellationToken ct = default)
        {
            return new WslDistro { Name = distroName, IsDefault = true, State = "Running" };
        }

        public async Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default)
        {
            return new WslNetworkInfo
            {
                IpAddress = "localhost",
                Gateway = "localhost",
                NetworkName = "bridge"
            };
        }

        public async Task<Dictionary<string, string>> GetConfigurationAsync(CancellationToken ct = default)
        {
            return new Dictionary<string, string>
            {
                ["ComposeFile"] = _composeFile,
                ["Platform"] = "Docker"
            };
        }

        public async Task<bool> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken ct = default)
        {
            // Configuration updates would be handled through docker-compose.yml changes
            return true;
        }

        public async Task<long> CleanupAsync(CancellationToken ct = default)
        {
            var result = await ExecuteDockerAsync("system prune -f --volumes");
            return result.IsSuccess ? 0 : -1;
        }

        // File operations - minimal implementation
        public async Task<bool> CopyFileToWslAsync(string windowsFilePath, string wslFilePath, CancellationToken ct = default)
        {
            var result = await ExecuteAsync("cp", $"{windowsFilePath} {wslFilePath}");
            return result.IsSuccess;
        }

        public async Task<bool> CopyFileFromWslAsync(string wslFilePath, string windowsFilePath, CancellationToken ct = default)
        {
            var result = await ExecuteAsync("cp", $"{wslFilePath} {windowsFilePath}");
            return result.IsSuccess;
        }

        public async Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default)
        {
            var result = await ExecuteAsync("rsync", $"-av {windowsPath}/ {wslPath}/");
            return result.IsSuccess;
        }

        public async Task<bool> SyncFilesAsync(FileSyncConfig config, CancellationToken ct = default)
        {
            return await SyncFilesAsync(config.SourcePath, config.TargetPath);
        }

        // Not applicable for Docker deployment
        public async Task<bool> ConfigureProxyAsync(ProxyConfig config, CancellationToken ct = default) => true;
        public async Task InstallTorAndApplyProxyAsync(string windowsProxyPath, CancellationToken ct = default) => await Task.CompletedTask;
        public async Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default) => true;
        public async Task<bool> ImportDistroAsync(string tarPath, string distroName, string installPath, CancellationToken ct = default) => true;
        public async Task<bool> ExportDistroAsync(string distroName, string exportPath, CancellationToken ct = default) => true;
        public async Task<bool> RemoveDistroAsync(string distroName, CancellationToken ct = default) => true;
        public async Task<bool> SetMemoryLimitAsync(int memoryGb, CancellationToken ct = default) => true;
        public async Task<bool> SetCpuLimitAsync(int cpuCount, CancellationToken ct = default) => true;

        // Helper methods
        private async Task<CommandResult> ExecuteDockerAsync(string arguments)
        {
            return await ExecuteAsync("docker", arguments);
        }

        private async Task<CommandResult> ExecuteDockerComposeAsync(string arguments)
        {
            return await ExecuteAsync("docker-compose", $"-f {_composeFile} {arguments}");
        }

        private async Task<CommandResult> ExecuteAsync(string fileName, string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                return new CommandResult
                {
                    IsSuccess = process.ExitCode == 0,
                    StandardOutput = output,
                    ErrorMessage = error,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute command: {FileName} {Arguments}", fileName, arguments);
                return new CommandResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ExitCode = -1
                };
            }
        }

        private async Task<List<string>> ParseDockerComposeServicesAsync(string output)
        {
            var services = new List<string>();

            if (string.IsNullOrEmpty(output))
                return services;

            try
            {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var container = JsonSerializer.Deserialize<DockerContainer>(line);
                    if (container != null)
                    {
                        services.Add(container.Service);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Docker services JSON");
            }

            return services;
        }
    }

    // Helper class for JSON parsing
    internal class DockerContainer
    {
        public string Service { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}