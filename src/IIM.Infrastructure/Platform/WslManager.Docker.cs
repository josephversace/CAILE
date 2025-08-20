// ============================================
// File: src/IIM.Infrastructure/Platform/WslManager.Docker.cs
// Purpose: Docker service management in WSL
// Author: IIM Platform Team
// Created: 2024
// ============================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Infrastructure.Platform.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Platform
{
    /// <summary>
    /// Docker service management operations
    /// </summary>
    public sealed partial class WslManager
    {
        /// <summary>
        /// Starts all IIM services (Qdrant, PostgreSQL, MinIO, MCP).
        /// </summary>
        public async Task<bool> StartIim()
        {
            try
            {
                _logger.LogInformation("Starting IIM services");

                // Ensure WSL is ready
                var status = await GetStatusAsync();
                if (!status.IsReady)
                {
                    _logger.LogError("WSL is not ready: {Message}", status.Message);
                    return false;
                }

                // Get the Ubuntu distro
                var distro = status.InstalledDistros.FirstOrDefault(d => d.Name == UBUNTU_DISTRO);
                if (distro == null)
                {
                    _logger.LogError("IIM-Ubuntu distro not found");
                    return false;
                }

                // Start services
                return await StartServicesAsync(distro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start IIM services");
                return false;
            }
        }

        /// <summary>
        /// Starts Docker services in the specified distribution.
        /// </summary>
        public async Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Starting services in {Distro}", distro.Name);

                // Ensure Docker is running
                if (!await EnsureDockerReadyAsync(ct))
                {
                    _logger.LogError("Docker is not ready");
                    return false;
                }

                // Create network if it doesn't exist
                await CreateDockerNetworkAsync("iim-network", ct);

                // Start each service
                var services = GetServiceConfigurations();
                var success = true;

                foreach (var service in services)
                {
                    try
                    {
                        var containerId = await StartDockerContainerAsync(service, ct);
                        if (string.IsNullOrEmpty(containerId))
                        {
                            _logger.LogError("Failed to start {Service}", service.Name);
                            success = false;
                        }
                        else
                        {
                            _logger.LogInformation("Started {Service} with ID {ContainerId}",
                                service.Name, containerId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to start {Service}", service.Name);
                        success = false;
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start services");
                return false;
            }
        }

        /// <summary>
        /// Stops all IIM services.
        /// </summary>
        public async Task<bool> StopServicesAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Stopping IIM services");

                var services = new[] { "qdrant", "postgres", "minio", "mcp-server" };
                var success = true;

                foreach (var service in services)
                {
                    if (!await StopDockerContainerAsync($"iim-{service}", ct))
                    {
                        success = false;
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop services");
                return false;
            }
        }

        /// <summary>
        /// Restarts a specific service.
        /// </summary>
        public async Task<bool> RestartServiceAsync(string serviceName, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Restarting service {Service}", serviceName);

                // Stop the service
                await StopDockerContainerAsync($"iim-{serviceName}", ct);

                // Wait a moment
                await Task.Delay(2000, ct);

                // Start the service
                var config = GetServiceConfiguration(serviceName);
                if (config != null)
                {
                    var containerId = await StartDockerContainerAsync(config, ct);
                    return !string.IsNullOrEmpty(containerId);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart service {Service}", serviceName);
                return false;
            }
        }

        /// <summary>
        /// Ensures Docker is installed and running in WSL.
        /// </summary>
        public async Task<bool> EnsureDockerReadyAsync(CancellationToken ct = default)
        {
            try
            {
                // Check if Docker is installed
                var checkResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    "which docker", ct);

                if (checkResult.ExitCode != 0)
                {
                    _logger.LogInformation("Docker not found, installing...");
                    await InstallDockerAsync(ct);
                }

                // Start Docker if not running
                var statusResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    "systemctl is-active docker || service docker status", ct);

                if (statusResult.ExitCode != 0)
                {
                    _logger.LogInformation("Starting Docker service...");
                    await ExecuteCommandAsync(UBUNTU_DISTRO,
                        "service docker start", ct);

                    // Wait for Docker to be ready
                    await Task.Delay(3000, ct);
                }

                // Verify Docker is working
                var versionResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    "docker version", ct);

                return versionResult.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure Docker is ready");
                return false;
            }
        }

        /// <summary>
        /// Starts a Docker container with the specified configuration.
        /// </summary>
        public async Task<string?> StartDockerContainerAsync(DockerServiceConfig config, CancellationToken ct = default)
        {
            try
            {
                // Check if container already exists
                var checkResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    $"docker ps -a --filter name=iim-{config.Name} --format '{{{{.Names}}}}'", ct);

                if (!string.IsNullOrWhiteSpace(checkResult.StandardOutput))
                {
                    // Container exists, remove it first
                    await ExecuteCommandAsync(UBUNTU_DISTRO,
                        $"docker rm -f iim-{config.Name}", ct);
                }

                // Build docker run command
                var runCommand = BuildDockerRunCommand(config);

                // Start container
                var result = await ExecuteCommandAsync(UBUNTU_DISTRO, runCommand, ct);

                if (result.ExitCode == 0)
                {
                    var containerId = result.StandardOutput.Trim();
                    _logger.LogInformation("Started container {Name} with ID {Id}",
                        config.Name, containerId);
                    return containerId;
                }

                _logger.LogError("Failed to start container {Name}: {Error}",
                    config.Name, result.StandardError);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start container {Name}", config.Name);
                return null;
            }
        }

        /// <summary>
        /// Stops a Docker container.
        /// </summary>
        public async Task<bool> StopDockerContainerAsync(string containerName, CancellationToken ct = default)
        {
            try
            {
                var result = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    $"docker stop {containerName}", ct);

                if (result.ExitCode == 0)
                {
                    _logger.LogInformation("Stopped container {Name}", containerName);
                    return true;
                }

                _logger.LogWarning("Failed to stop container {Name}: {Error}",
                    containerName, result.StandardError);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop container {Name}", containerName);
                return false;
            }
        }

        /// <summary>
        /// Gets the status of Docker containers.
        /// </summary>
        public async Task<Dictionary<string, string>> GetDockerContainerStatusAsync(CancellationToken ct = default)
        {
            var status = new Dictionary<string, string>();

            try
            {
                var result = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    "docker ps -a --filter label=iim --format '{{.Names}}:{{.Status}}'", ct);

                if (result.ExitCode == 0)
                {
                    var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(':', 2);
                        if (parts.Length == 2)
                        {
                            status[parts[0]] = parts[1];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get container status");
            }

            return status;
        }

        // ========================================
        // Private Docker Helper Methods
        // ========================================

        /// <summary>
        /// Installs Docker in the WSL distribution.
        /// </summary>
        private async Task InstallDockerAsync(CancellationToken ct)
        {
            var installScript = @"
                apt-get update
                apt-get install -y ca-certificates curl gnupg lsb-release
                curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
                echo 'deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable' | tee /etc/apt/sources.list.d/docker.list > /dev/null
                apt-get update
                apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
                usermod -aG docker $USER
                service docker start
            ";

            await ExecuteCommandAsync(UBUNTU_DISTRO, installScript, ct);
            _logger.LogInformation("Docker installed successfully");
        }

        /// <summary>
        /// Creates a Docker network if it doesn't exist.
        /// </summary>
        private async Task CreateDockerNetworkAsync(string networkName, CancellationToken ct)
        {
            var checkResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                $"docker network ls --filter name={networkName} --format '{{{{.Name}}}}'", ct);

            if (string.IsNullOrWhiteSpace(checkResult.StandardOutput))
            {
                await ExecuteCommandAsync(UBUNTU_DISTRO,
                    $"docker network create {networkName}", ct);
                _logger.LogInformation("Created Docker network {Network}", networkName);
            }
        }

        /// <summary>
        /// Builds the Docker run command from configuration.
        /// </summary>
        private string BuildDockerRunCommand(DockerServiceConfig config)
        {
            var sb = new StringBuilder();
            sb.Append($"docker run -d --name iim-{config.Name}");
            sb.Append($" --network {config.NetworkName}");
            sb.Append($" --restart {config.RestartPolicy}");
            sb.Append(" --label iim=true");

            // Add port mappings
            foreach (var (hostPort, containerPort) in config.Ports)
            {
                sb.Append($" -p {hostPort}:{containerPort}");
            }

            // Add environment variables
            foreach (var (key, value) in config.Environment)
            {
                sb.Append($" -e {key}={value}");
            }

            // Add volume mappings
            foreach (var (hostPath, containerPath) in config.Volumes)
            {
                sb.Append($" -v {hostPath}:{containerPath}");
            }

            // Add resource limits
            if (config.MemoryLimit.HasValue)
            {
                sb.Append($" --memory {config.MemoryLimit}");
            }

            if (!string.IsNullOrEmpty(config.CpuLimit))
            {
                sb.Append($" --cpus {config.CpuLimit}");
            }

            // Add health check if specified
            if (!string.IsNullOrEmpty(config.HealthCheckCommand))
            {
                sb.Append($" --health-cmd '{config.HealthCheckCommand}'");
                sb.Append(" --health-interval 30s");
                sb.Append(" --health-timeout 10s");
                sb.Append(" --health-retries 3");
            }

            sb.Append($" {config.Image}");
            return sb.ToString();
        }

        /// <summary>
        /// Gets service configurations for all IIM services.
        /// </summary>
        private List<DockerServiceConfig> GetServiceConfigurations()
        {
            return new List<DockerServiceConfig>
            {
                // Qdrant Vector Database
                new DockerServiceConfig
                {
                    Name = "qdrant",
                    Image = "qdrant/qdrant:latest",
                    Ports = new Dictionary<int, int> { [6333] = 6333, [6334] = 6334 },
                    Environment = new Dictionary<string, string>
                    {
                        ["QDRANT__SERVICE__HTTP_PORT"] = "6333",
                        ["QDRANT__SERVICE__GRPC_PORT"] = "6334"
                    },
                    Volumes = new Dictionary<string, string>
                    {
                        ["/var/lib/iim/qdrant"] = "/qdrant/storage"
                    },
                    MemoryLimit = 4L * 1024 * 1024 * 1024, // 4GB
                    HealthCheckCommand = "curl -f http://localhost:6333/health || exit 1"
                },

                // PostgreSQL Database
                new DockerServiceConfig
                {
                    Name = "postgres",
                    Image = "postgres:15-alpine",
                    Ports = new Dictionary<int, int> { [5432] = 5432 },
                    Environment = new Dictionary<string, string>
                    {
                        ["POSTGRES_DB"] = "iim",
                        ["POSTGRES_USER"] = "iim_user",
                        ["POSTGRES_PASSWORD"] = "iim_secure_password",
                        ["PGDATA"] = "/var/lib/postgresql/data/pgdata"
                    },
                    Volumes = new Dictionary<string, string>
                    {
                        ["/var/lib/iim/postgres"] = "/var/lib/postgresql/data"
                    },
                    MemoryLimit = 2L * 1024 * 1024 * 1024, // 2GB
                    HealthCheckCommand = "pg_isready -U iim_user || exit 1"
                },

                // MinIO Object Storage
                new DockerServiceConfig
                {
                    Name = "minio",
                    Image = "minio/minio:latest",
                    Ports = new Dictionary<int, int> { [9000] = 9000, [9001] = 9001 },
                    Environment = new Dictionary<string, string>
                    {
                        ["MINIO_ROOT_USER"] = "iim_admin",
                        ["MINIO_ROOT_PASSWORD"] = "iim_secure_password",
                        ["MINIO_BROWSER_REDIRECT_URL"] = "http://localhost:9001"
                    },
                    Volumes = new Dictionary<string, string>
                    {
                        ["/var/lib/iim/minio"] = "/data"
                    },
                    MemoryLimit = 2L * 1024 * 1024 * 1024, // 2GB
                    HealthCheckCommand = "curl -f http://localhost:9000/minio/health/live || exit 1"
                },

                // MCP Server (Forensic Tools)
                new DockerServiceConfig
                {
                    Name = "mcp-server",
                    Image = "iim/mcp-server:latest",
                    Ports = new Dictionary<int, int> { [3000] = 3000 },
                    Environment = new Dictionary<string, string>
                    {
                        ["NODE_ENV"] = "production",
                        ["PORT"] = "3000"
                    },
                    Volumes = new Dictionary<string, string>
                    {
                        ["/var/lib/iim/mcp"] = "/app/data"
                    },
                    MemoryLimit = 1L * 1024 * 1024 * 1024, // 1GB
                    HealthCheckCommand = "curl -f http://localhost:3000/health || exit 1"
                }
            };
        }

        /// <summary>
        /// Gets configuration for a specific service.
        /// </summary>
        private DockerServiceConfig? GetServiceConfiguration(string serviceName)
        {
            return GetServiceConfigurations().FirstOrDefault(s =>
                s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }
    }
}