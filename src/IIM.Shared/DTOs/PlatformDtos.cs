
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs;

// WSL Management DTOs
public record WslStatusDto(
    bool IsInstalled,
    bool IsWsl2,
    string? Version,
    string? KernelVersion,
    bool VirtualMachinePlatform,
    bool HyperV,
    bool HasIimDistro,
    bool IsReady,
    string Message
);

public record WslDistroDto(
    string Name,
    int Version,
    string State,
    string? DefaultUser,
    string? BasePath
);

public record WslNetworkInfoDto(
    string DistroName,
    string? WslIpAddress,
    string? WindowsHostIp,
    bool IsConnected,
    Dictionary<string, string> ServiceEndpoints
);

public record WslHealthCheckDto(
    bool IsHealthy,
    bool WslReady,
    bool DistroRunning,
    bool ServicesHealthy,
    bool NetworkConnected,
    List<string> Issues,
    DateTimeOffset CheckedAt
);

// Service Management DTOs
public record ServiceStatusDto(
    string Name,
    string State,
    bool IsHealthy,
    string? Endpoint,
    string? Version,
    long MemoryUsage,
    double CpuUsage,
    DateTimeOffset? StartedAt,
    string? Message
);

public record ServiceConfigDto(
    string Name,
    string Type,
    string? DockerImage,
    int Port,
    string? HealthEndpoint,
    string StartupCommand,
    string? WorkingDirectory,
    long RequiredMemoryMb,
    string Priority,
    Dictionary<string, string>? Environment
);

public record ServiceListResponse(
    Dictionary<string, ServiceStatusDto> Services,
    int TotalServices,
    int RunningServices,
    int HealthyServices,
    DateTimeOffset CheckedAt
);

// Evidence Configuration DTOs
public record EvidenceConfigurationDto(
    string StorePath,
    bool EnableEncryption,
    bool RequireDualControl,
    int MaxFileSizeMb,
    List<string>? AllowedFileTypes,
    Dictionary<string, object>? SecuritySettings
);