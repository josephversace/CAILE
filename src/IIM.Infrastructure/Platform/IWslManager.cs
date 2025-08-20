// ============================================
// File: src/IIM.Infrastructure/Platform/IWslManager.cs
// Purpose: Interface for WSL2 management operations
// Author: IIM Platform Team
// Created: 2024
// ============================================

using System.Threading;
using System.Threading.Tasks;
using IIM.Infrastructure.Platform.Models;

namespace IIM.Infrastructure.Platform
{
    /// <summary>
    /// Interface for managing WSL2 installation, configuration, and Docker services.
    /// Provides methods for setting up and maintaining the IIM platform's WSL environment.
    /// </summary>
    public interface IWslManager
    {
        // ========================================
        // Core WSL Operations
        // ========================================

        /// <summary>
        /// Gets the current WSL status including installation state and available distributions.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>WSL status information</returns>
        Task<WslStatus> GetStatusAsync(CancellationToken ct = default);

        /// <summary>
        /// Checks if WSL is enabled on the system.
        /// </summary>
        /// <returns>True if WSL is enabled</returns>
        Task<bool> IsWslEnabled();

        /// <summary>
        /// Enables WSL on the system. Requires administrator privileges or generates scripts for IT.
        /// </summary>
        /// <returns>True if WSL was successfully enabled</returns>
        Task<bool> EnableWsl();

        // ========================================
        // Distribution Management
        // ========================================

        /// <summary>
        /// Ensures a WSL distribution is installed and configured.
        /// </summary>
        /// <param name="distroName">Name of the distribution (default: IIM-Ubuntu)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Distribution information</returns>
        Task<WslDistro> EnsureDistroAsync(string distroName = "IIM-Ubuntu", CancellationToken ct = default);

        /// <summary>
        /// Checks if a specific distribution exists.
        /// </summary>
        /// <param name="distroName">Name of the distribution</param>
        /// <returns>True if the distribution exists</returns>
        Task<bool> DistroExists(string distroName = "IIM-Ubuntu");

        /// <summary>
        /// Installs a WSL distribution from a tar.gz file.
        /// </summary>
        /// <param name="distroPath">Path to the distribution tar.gz file</param>
        /// <param name="installName">Name for the installed distribution</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if installation was successful</returns>
        Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default);

        // ========================================
        // Service Management
        // ========================================

        /// <summary>
        /// Starts all IIM services (Qdrant, PostgreSQL, MinIO, MCP).
        /// </summary>
        /// <returns>True if all services started successfully</returns>
        Task<bool> StartIim();

        /// <summary>
        /// Starts Docker services in the specified distribution.
        /// </summary>
        /// <param name="distro">Distribution to start services in</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if services started successfully</returns>
        Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default);

        /// <summary>
        /// Stops all IIM services.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if all services stopped successfully</returns>
        Task<bool> StopServicesAsync(CancellationToken ct = default);

        /// <summary>
        /// Restarts a specific service.
        /// </summary>
        /// <param name="serviceName">Name of the service to restart</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if service restarted successfully</returns>
        Task<bool> RestartServiceAsync(string serviceName, CancellationToken ct = default);

        // ========================================
        // Network and Connectivity
        // ========================================

        /// <summary>
        /// Gets network information for a WSL distribution.
        /// </summary>
        /// <param name="distroName">Name of the distribution</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Network configuration information</returns>
        Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default);

        /// <summary>
        /// Installs Tor and configures proxy settings for WSL.
        /// </summary>
        /// <param name="windowsProxyPath">Windows path to proxy.env file</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Task completion</returns>
        Task InstallTorAndApplyProxyAsync(string windowsProxyPath, CancellationToken ct = default);

        /// <summary>
        /// Configures proxy settings for WSL distributions.
        /// </summary>
        /// <param name="config">Proxy configuration</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if proxy configured successfully</returns>
        Task<bool> ConfigureProxyAsync(ProxyConfig config, CancellationToken ct = default);

        // ========================================
        // Health Monitoring
        // ========================================

        /// <summary>
        /// Performs a comprehensive health check of WSL and all services.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Health check results</returns>
        Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default);

        /// <summary>
        /// Checks the health of a specific service.
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Service health check result</returns>
        Task<ServiceHealthCheck> CheckServiceHealthAsync(string serviceName, CancellationToken ct = default);

        // ========================================
        // File Operations
        // ========================================

        /// <summary>
        /// Synchronizes files from Windows to WSL.
        /// </summary>
        /// <param name="windowsPath">Windows source path</param>
        /// <param name="wslPath">WSL destination path</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if sync was successful</returns>
        Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default);

        /// <summary>
        /// Synchronizes files with advanced options.
        /// </summary>
        /// <param name="config">File sync configuration</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if sync was successful</returns>
        Task<bool> SyncFilesAsync(FileSyncConfig config, CancellationToken ct = default);

        /// <summary>
        /// Copies a file from Windows to WSL.
        /// </summary>
        /// <param name="windowsFilePath">Windows file path</param>
        /// <param name="wslFilePath">WSL destination path</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if copy was successful</returns>
        Task<bool> CopyFileToWslAsync(string windowsFilePath, string wslFilePath, CancellationToken ct = default);

        /// <summary>
        /// Copies a file from WSL to Windows.
        /// </summary>
        /// <param name="wslFilePath">WSL file path</param>
        /// <param name="windowsFilePath">Windows destination path</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if copy was successful</returns>
        Task<bool> CopyFileFromWslAsync(string wslFilePath, string windowsFilePath, CancellationToken ct = default);

        // ========================================
        // Command Execution
        // ========================================

        /// <summary>
        /// Executes a command in the default WSL distribution.
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Command execution result</returns>
        Task<CommandResult> ExecuteCommandAsync(string command, CancellationToken ct = default);

        /// <summary>
        /// Executes a command in a specific WSL distribution.
        /// </summary>
        /// <param name="distroName">Name of the distribution</param>
        /// <param name="command">Command to execute</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Command execution result</returns>
        Task<CommandResult> ExecuteCommandAsync(string distroName, string command, CancellationToken ct = default);

        /// <summary>
        /// Executes a command with streaming output.
        /// </summary>
        /// <param name="distroName">Name of the distribution</param>
        /// <param name="command">Command to execute</param>
        /// <param name="outputCallback">Callback for output lines</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Command exit code</returns>
        Task<int> ExecuteCommandWithStreamingAsync(
            string distroName,
            string command,
            Action<string> outputCallback,
            CancellationToken ct = default);

        // ========================================
        // Docker Management
        // ========================================

        /// <summary>
        /// Ensures Docker is installed and running in WSL.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if Docker is ready</returns>
        Task<bool> EnsureDockerReadyAsync(CancellationToken ct = default);

        /// <summary>
        /// Starts a Docker container with the specified configuration.
        /// </summary>
        /// <param name="config">Docker service configuration</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Container ID if successful</returns>
        Task<string?> StartDockerContainerAsync(DockerServiceConfig config, CancellationToken ct = default);

        /// <summary>
        /// Stops a Docker container.
        /// </summary>
        /// <param name="containerName">Name or ID of the container</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if container stopped successfully</returns>
        Task<bool> StopDockerContainerAsync(string containerName, CancellationToken ct = default);

        /// <summary>
        /// Gets the status of Docker containers.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Dictionary of container names and their status</returns>
        Task<Dictionary<string, string>> GetDockerContainerStatusAsync(CancellationToken ct = default);

        // ========================================
        // Maintenance and Cleanup
        // ========================================

        /// <summary>
        /// Cleans up temporary files and caches in WSL.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Amount of space freed in bytes</returns>
        Task<long> CleanupAsync(CancellationToken ct = default);

        /// <summary>
        /// Exports a WSL distribution to a tar file.
        /// </summary>
        /// <param name="distroName">Name of the distribution</param>
        /// <param name="exportPath">Path for the exported tar file</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if export was successful</returns>
        Task<bool> ExportDistroAsync(string distroName, string exportPath, CancellationToken ct = default);

        /// <summary>
        /// Imports a WSL distribution from a tar file.
        /// </summary>
        /// <param name="tarPath">Path to the tar file</param>
        /// <param name="distroName">Name for the imported distribution</param>
        /// <param name="installPath">Installation path</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if import was successful</returns>
        Task<bool> ImportDistroAsync(string tarPath, string distroName, string installPath, CancellationToken ct = default);

        /// <summary>
        /// Unregisters and removes a WSL distribution.
        /// </summary>
        /// <param name="distroName">Name of the distribution</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if removal was successful</returns>
        Task<bool> RemoveDistroAsync(string distroName, CancellationToken ct = default);

        // ========================================
        // Configuration
        // ========================================

        /// <summary>
        /// Gets the current WSL configuration.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Configuration as key-value pairs</returns>
        Task<Dictionary<string, string>> GetConfigurationAsync(CancellationToken ct = default);

        /// <summary>
        /// Updates WSL configuration settings.
        /// </summary>
        /// <param name="settings">Configuration settings to update</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if configuration updated successfully</returns>
        Task<bool> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken ct = default);

        /// <summary>
        /// Sets memory limit for WSL2.
        /// </summary>
        /// <param name="memoryGb">Memory limit in GB</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if memory limit set successfully</returns>
        Task<bool> SetMemoryLimitAsync(int memoryGb, CancellationToken ct = default);

        /// <summary>
        /// Sets CPU limit for WSL2.
        /// </summary>
        /// <param name="cpuCount">Number of CPUs to allocate</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if CPU limit set successfully</returns>
        Task<bool> SetCpuLimitAsync(int cpuCount, CancellationToken ct = default);
    }
}