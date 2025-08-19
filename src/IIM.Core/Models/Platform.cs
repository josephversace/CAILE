

using System;
using System.Collections.Generic;

using IIM.Shared.Enums;

namespace IIM.Core.Models;




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








