using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    // ===== System Status Models =====

    /// <summary>
    /// System status information
    /// </summary>
    public class SystemStatus
    {
        public string Status { get; set; } = "Unknown";
        public bool IsHealthy { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public long MemoryTotal { get; set; }
        public long MemoryAvailable { get; set; }
        public long DiskSpaceTotal { get; set; }
        public long DiskSpaceAvailable { get; set; }
        public Dictionary<string, ServiceStatus> Services { get; set; } = new();
        public DateTimeOffset LastChecked { get; set; } = DateTimeOffset.UtcNow;
        public string Version { get; set; } = string.Empty;
        public TimeSpan Uptime { get; set; }
        public string HostName { get; set; } = Environment.MachineName;
        public string OperatingSystem { get; set; } = Environment.OSVersion.ToString();

        public double GetMemoryUsagePercentage()
        {
            return MemoryTotal > 0 ? ((double)(MemoryTotal - MemoryAvailable) / MemoryTotal) * 100 : 0;
        }

        public double GetDiskUsagePercentage()
        {
            return DiskSpaceTotal > 0 ? ((double)(DiskSpaceTotal - DiskSpaceAvailable) / DiskSpaceTotal) * 100 : 0;
        }
    }

    /// <summary>
    /// Individual service status
    /// </summary>
    public class ServiceStatus
    {
        public string Name { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset LastHeartbeat { get; set; }
        public int? ProcessId { get; set; }
        public long? MemoryUsage { get; set; }
        public double? CpuUsage { get; set; }
        public TimeSpan? Uptime { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        public bool IsHealthy()
        {
            return IsRunning &&
                   (DateTimeOffset.UtcNow - LastHeartbeat).TotalMinutes < 5;
        }
    }

    /// <summary>
    /// WSL status information
    /// </summary>
    public class WslStatus
    {
        public bool IsRunning { get; set; }
        public bool IsInstalled { get; set; }
        public string Distribution { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int WslVersion { get; set; } = 2;
        public long MemoryUsage { get; set; }
        public long DiskUsage { get; set; }
        public Dictionary<string, bool> Features { get; set; } = new();
        public List<string> RunningServices { get; set; } = new();
        public string DefaultUser { get; set; } = string.Empty;
        public string MountPath { get; set; } = "/mnt";

        public bool SupportsGpu()
        {
            return Features.ContainsKey("gpu") && Features["gpu"];
        }
    }

    /// <summary>
    /// Test connection result
    /// </summary>
    public class TestConnectionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public int? StatusCode { get; set; }
        public Dictionary<string, object>? Details { get; set; }
        public DateTimeOffset TestedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? ErrorDetails { get; set; }

        public static TestConnectionResult CreateSuccess(string endpoint, TimeSpan responseTime)
        {
            return new TestConnectionResult
            {
                Success = true,
                Endpoint = endpoint,
                ResponseTime = responseTime,
                Message = "Connection successful",
                StatusCode = 200
            };
        }

        public static TestConnectionResult CreateFailure(string endpoint, string error)
        {
            return new TestConnectionResult
            {
                Success = false,
                Endpoint = endpoint,
                Message = "Connection failed",
                ErrorDetails = error
            };
        }

    }
}
