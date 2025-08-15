

using System;
using System.Collections.Generic;

namespace IIM.Core.Models;
// WSL Management Models
public class WslStatus
{
    public bool IsInstalled { get; set; }
    public bool IsWsl2 { get; set; }
    public string? Version { get; set; }
    public string? KernelVersion { get; set; }
    public bool VirtualMachinePlatform { get; set; }
    public bool HyperV { get; set; }
    public bool HasIimDistro { get; set; }
    public bool IsReady { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class WslDistro
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string State { get; set; } = string.Empty;
    public string? DefaultUser { get; set; }
    public string? BasePath { get; set; }
}

public class WslNetworkInfo
{
    public string DistroName { get; set; } = string.Empty;
    public string? WslIpAddress { get; set; }
    public string? WindowsHostIp { get; set; }
    public bool IsConnected { get; set; }
    public Dictionary<string, string> ServiceEndpoints { get; set; } = new();
}

public class WslHealthCheck
{
    public bool IsHealthy { get; set; }
    public bool WslReady { get; set; }
    public bool DistroRunning { get; set; }
    public bool ServicesHealthy { get; set; }
    public bool NetworkConnected { get; set; }
    public List<string> Issues { get; set; } = new();
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
}

// Service Management
public class ServiceStatus
{
    public string Name { get; set; } = string.Empty;
    public ServiceState State { get; set; }
    public bool IsHealthy { get; set; }
    public string? Endpoint { get; set; }
    public string? Version { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public string? Message { get; set; }
}

public class ServiceConfig
{
    public string Name { get; set; } = string.Empty;
    public ServiceType Type { get; set; }
    public string? DockerImage { get; set; }
    public int Port { get; set; }
    public string? HealthEndpoint { get; set; }
    public string StartupCommand { get; set; } = string.Empty;
    public string? WorkingDirectory { get; set; }
    public long RequiredMemoryMb { get; set; }
    public ServicePriority Priority { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new();
}

public enum ServiceState
{
    NotFound,
    Stopped,
    Starting,
    Running,
    Stopping,
    Error,
    Degraded
}

public enum ServiceType
{
    Docker,
    Python,
    Binary,
    SystemService
}

public enum ServicePriority
{
    Critical,
    High,
    Normal,
    Low
}

