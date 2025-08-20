// ============================================
// File: src/IIM.Infrastructure/Platform/Models/WslModels.cs
// Purpose: WSL-related model classes and enums
// Author: IIM Platform Team
// Created: 2024
// ============================================

using System;
using System.Collections.Generic;

namespace IIM.Infrastructure.Platform.Models
{
    /// <summary>
    /// WSL status information containing installation state and readiness
    /// </summary>
    public sealed class WslStatus
    {
        /// <summary>
        /// Indicates if WSL is installed on the system
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Indicates if WSL2 (vs WSL1) is available
        /// </summary>
        public bool IsWsl2 { get; set; }

        /// <summary>
        /// WSL version string (e.g., "2.0.0")
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// WSL kernel version string
        /// </summary>
        public string? KernelVersion { get; set; }

        /// <summary>
        /// Indicates if Virtual Machine Platform is enabled
        /// </summary>
        public bool VirtualMachinePlatform { get; set; }

        /// <summary>
        /// Indicates if Hyper-V is enabled (required for WSL2)
        /// </summary>
        public bool HyperV { get; set; }

        /// <summary>
        /// List of installed WSL distributions
        /// </summary>
        public List<WslDistro> InstalledDistros { get; set; } = new();

        /// <summary>
        /// Indicates if the IIM-specific distro is installed
        /// </summary>
        public bool HasIimDistro { get; set; }

        /// <summary>
        /// Overall readiness status for IIM operations
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Human-readable status message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when this status was captured
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// WSL distribution information
    /// </summary>
    public sealed class WslDistro
    {
        /// <summary>
        /// Name of the distribution (e.g., "IIM-Ubuntu", "IIM-Kali")
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Current state of the distribution
        /// </summary>
        public WslDistroState State { get; set; }

        /// <summary>
        /// WSL version for this distro ("1" or "2")
        /// </summary>
        public string? Version { get; init; }

        /// <summary>
        /// IP address of the distro if running
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Indicates if this is the default WSL distro
        /// </summary>
        public bool IsDefault { get; init; }

        /// <summary>
        /// Path where the distro is installed
        /// </summary>
        public string? InstallPath { get; set; }

        /// <summary>
        /// Size of the distro in bytes
        /// </summary>
        public long? SizeBytes { get; set; }
    }

    /// <summary>
    /// WSL network configuration and connectivity information
    /// </summary>
    public sealed class WslNetworkInfo
    {
        /// <summary>
        /// Name of the distribution
        /// </summary>
        public required string DistroName { get; init; }

        /// <summary>
        /// IP address of the WSL instance
        /// </summary>
        public string? WslIpAddress { get; set; }

        /// <summary>
        /// IP address of the Windows host as seen from WSL
        /// </summary>
        public string? WindowsHostIp { get; set; }

        /// <summary>
        /// Windows network interface for WSL
        /// </summary>
        public string? WindowsWslInterface { get; set; }

        /// <summary>
        /// Service endpoints with their URLs
        /// </summary>
        public Dictionary<string, string> ServiceEndpoints { get; set; } = new();

        /// <summary>
        /// Indicates if network connectivity is established
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Error message if connectivity failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Network latency in milliseconds
        /// </summary>
        public double? LatencyMs { get; set; }
    }

    /// <summary>
    /// Service health check result for individual services
    /// </summary>
    public sealed class ServiceHealthCheck
    {
        /// <summary>
        /// Name of the service (e.g., "qdrant", "postgres")
        /// </summary>
        public required string ServiceName { get; init; }

        /// <summary>
        /// Overall health status
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Detailed health information
        /// </summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// Service response time in milliseconds
        /// </summary>
        public double? ResponseTimeMs { get; set; }

        /// <summary>
        /// Service version if available
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Port number the service is running on
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Memory usage in bytes if available
        /// </summary>
        public long? MemoryUsageBytes { get; set; }
    }

    /// <summary>
    /// Overall health check result for WSL and all services
    /// </summary>
    public sealed class HealthCheckResult
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// WSL readiness status
        /// </summary>
        public bool WslReady { get; set; }

        /// <summary>
        /// Distribution running status
        /// </summary>
        public bool DistroRunning { get; set; }

        /// <summary>
        /// All services healthy status
        /// </summary>
        public bool ServicesHealthy { get; set; }

        /// <summary>
        /// Network connectivity status
        /// </summary>
        public bool NetworkConnected { get; set; }

        /// <summary>
        /// Individual service health checks
        /// </summary>
        public List<ServiceHealthCheck> ServiceChecks { get; set; } = new();

        /// <summary>
        /// List of issues found during health check
        /// </summary>
        public IEnumerable<string> Issues { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Timestamp of the health check
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Total time taken for health check in milliseconds
        /// </summary>
        public double ElapsedMs { get; set; }
    }

    /// <summary>
    /// Result of command execution in WSL
    /// </summary>
    public sealed class CommandResult
    {
        /// <summary>
        /// Exit code of the command (0 = success)
        /// </summary>
        public int ExitCode { get; init; }

        /// <summary>
        /// Standard output from the command
        /// </summary>
        public string StandardOutput { get; init; } = string.Empty;

        /// <summary>
        /// Standard error output from the command
        /// </summary>
        public string StandardError { get; init; } = string.Empty;

        /// <summary>
        /// Time taken to execute the command
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Indicates if the command was successful (ExitCode == 0)
        /// </summary>
        public bool IsSuccess => ExitCode == 0;
    }

    /// <summary>
    /// WSL installation options
    /// </summary>
    public sealed class WslInstallOptions
    {
        /// <summary>
        /// Install path for WSL distributions
        /// </summary>
        public string InstallPath { get; set; } = string.Empty;

        /// <summary>
        /// Whether to install Ubuntu distro
        /// </summary>
        public bool InstallUbuntu { get; set; } = true;

        /// <summary>
        /// Whether to install Kali distro
        /// </summary>
        public bool InstallKali { get; set; } = false;

        /// <summary>
        /// Whether to use WSL2 (recommended)
        /// </summary>
        public bool UseWsl2 { get; set; } = true;

        /// <summary>
        /// Whether to set as default distro
        /// </summary>
        public bool SetAsDefault { get; set; } = true;

        /// <summary>
        /// Custom distro download URL if provided
        /// </summary>
        public string? CustomDistroUrl { get; set; }
    }

    /// <summary>
    /// Docker service configuration
    /// </summary>
    public sealed class DockerServiceConfig
    {
        /// <summary>
        /// Service name (e.g., "qdrant", "postgres")
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Docker image name with tag
        /// </summary>
        public required string Image { get; init; }

        /// <summary>
        /// Port mappings (host:container)
        /// </summary>
        public Dictionary<int, int> Ports { get; set; } = new();

        /// <summary>
        /// Environment variables
        /// </summary>
        public Dictionary<string, string> Environment { get; set; } = new();

        /// <summary>
        /// Volume mappings (host:container)
        /// </summary>
        public Dictionary<string, string> Volumes { get; set; } = new();

        /// <summary>
        /// Container restart policy
        /// </summary>
        public string RestartPolicy { get; set; } = "unless-stopped";

        /// <summary>
        /// Memory limit in bytes
        /// </summary>
        public long? MemoryLimit { get; set; }

        /// <summary>
        /// CPU limit (e.g., "0.5" for half a CPU)
        /// </summary>
        public string? CpuLimit { get; set; }

        /// <summary>
        /// Health check command
        /// </summary>
        public string? HealthCheckCommand { get; set; }

        /// <summary>
        /// Network name to join
        /// </summary>
        public string NetworkName { get; set; } = "iim-network";
    }

    /// <summary>
    /// File sync operation configuration
    /// </summary>
    public sealed class FileSyncConfig
    {
        /// <summary>
        /// Source path on Windows
        /// </summary>
        public required string WindowsPath { get; init; }

        /// <summary>
        /// Destination path in WSL
        /// </summary>
        public required string WslPath { get; init; }

        /// <summary>
        /// File patterns to include (e.g., "*.pdf", "*.docx")
        /// </summary>
        public List<string> IncludePatterns { get; set; } = new();

        /// <summary>
        /// File patterns to exclude
        /// </summary>
        public List<string> ExcludePatterns { get; set; } = new();

        /// <summary>
        /// Whether to sync recursively
        /// </summary>
        public bool Recursive { get; set; } = true;

        /// <summary>
        /// Whether to delete files in destination that don't exist in source
        /// </summary>
        public bool DeleteOrphaned { get; set; } = false;

        /// <summary>
        /// Whether to preserve file permissions
        /// </summary>
        public bool PreservePermissions { get; set; } = true;

        /// <summary>
        /// Progress callback for sync operations
        /// </summary>
        public Action<FileSyncProgress>? ProgressCallback { get; set; }
    }

    /// <summary>
    /// File sync progress information
    /// </summary>
    public sealed class FileSyncProgress
    {
        /// <summary>
        /// Current file being synced
        /// </summary>
        public string CurrentFile { get; set; } = string.Empty;

        /// <summary>
        /// Total files to sync
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Files completed
        /// </summary>
        public int FilesCompleted { get; set; }

        /// <summary>
        /// Total bytes to transfer
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Bytes transferred
        /// </summary>
        public long BytesTransferred { get; set; }

        /// <summary>
        /// Transfer speed in bytes per second
        /// </summary>
        public double SpeedBps { get; set; }

        /// <summary>
        /// Estimated time remaining
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public double ProgressPercent { get; set; }
    }

    /// <summary>
    /// Proxy configuration for WSL
    /// </summary>
    public sealed class ProxyConfig
    {
        /// <summary>
        /// HTTP proxy URL
        /// </summary>
        public string? HttpProxy { get; set; }

        /// <summary>
        /// HTTPS proxy URL
        /// </summary>
        public string? HttpsProxy { get; set; }

        /// <summary>
        /// SOCKS proxy URL (for Tor)
        /// </summary>
        public string? SocksProxy { get; set; }

        /// <summary>
        /// No proxy list (comma-separated)
        /// </summary>
        public string? NoProxy { get; set; }

        /// <summary>
        /// Whether to install and configure Tor
        /// </summary>
        public bool EnableTor { get; set; }

        /// <summary>
        /// Tor SOCKS port
        /// </summary>
        public int TorPort { get; set; } = 9050;

        /// <summary>
        /// Tor control port
        /// </summary>
        public int TorControlPort { get; set; } = 9051;
    }

    /// <summary>
    /// WSL distro state enumeration
    /// </summary>
    public enum WslDistroState
    {
        /// <summary>
        /// State is unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// Distribution is running
        /// </summary>
        Running,

        /// <summary>
        /// Distribution is stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// Distribution is currently installing
        /// </summary>
        Installing,

        /// <summary>
        /// Distribution is in error state
        /// </summary>
        Error,

        /// <summary>
        /// Distribution is converting (WSL1 to WSL2)
        /// </summary>
        Converting,

        /// <summary>
        /// Distribution is being imported
        /// </summary>
        Importing
    }

    /// <summary>
    /// Service type enumeration for WSL services
    /// </summary>
    public enum WslServiceType
    {
        /// <summary>
        /// Qdrant vector database
        /// </summary>
        Qdrant,

        /// <summary>
        /// PostgreSQL database
        /// </summary>
        PostgreSQL,

        /// <summary>
        /// MinIO object storage
        /// </summary>
        MinIO,

        /// <summary>
        /// MCP server for forensic tools
        /// </summary>
        McpServer,

        /// <summary>
        /// Docker daemon
        /// </summary>
        Docker,

        /// <summary>
        /// Custom service
        /// </summary>
        Custom
    }

    /// <summary>
    /// WSL feature requirements
    /// </summary>
    public sealed class WslRequirements
    {
        /// <summary>
        /// Minimum Windows build number required
        /// </summary>
        public int MinimumWindowsBuild { get; set; } = 19041;

        /// <summary>
        /// Required Windows edition (e.g., "Professional", "Enterprise")
        /// </summary>
        public List<string> SupportedWindowsEditions { get; set; } = new()
        {
            "Professional",
            "Enterprise",
            "Education",
            "Pro for Workstations"
        };

        /// <summary>
        /// Required CPU features
        /// </summary>
        public List<string> RequiredCpuFeatures { get; set; } = new()
        {
            "VT-x/AMD-V",
            "SLAT"
        };

        /// <summary>
        /// Minimum RAM in GB
        /// </summary>
        public int MinimumRamGb { get; set; } = 8;

        /// <summary>
        /// Minimum free disk space in GB
        /// </summary>
        public int MinimumDiskSpaceGb { get; set; } = 20;

        /// <summary>
        /// Check if current system meets requirements
        /// </summary>
        public bool CheckRequirements(out List<string> missingRequirements)
        {
            missingRequirements = new List<string>();

            // This would contain actual requirement checking logic
            // For now, returning placeholder

            return missingRequirements.Count == 0;
        }
    }
}