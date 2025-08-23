using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// GPU information response
    /// </summary>
    public record GpuInfoResponse(
        string Vendor,
        string Provider,
        int VramGb,
        double AvailableMemoryGb,
        double UsedMemoryGb,
        int LoadedModels,
        Dictionary<string, object> Models
    );

    /// <summary>
    /// System statistics response
    /// </summary>
    public record SystemStatsResponse(
        object Pipeline,
        object Orchestrator,
        DateTimeOffset Timestamp
    );

    /// <summary>
    /// System status response DTO
    /// </summary>
    public record SystemStatusResponse(
        string Status,
        bool IsHealthy,
        double CpuUsage,
        double MemoryUsage,
        long MemoryTotal,
        long MemoryAvailable,
        long DiskSpaceTotal,
        long DiskSpaceAvailable,
        Dictionary<string, ServiceStatusInfo> Services,
        DateTimeOffset LastChecked,
        string Version,
        TimeSpan Uptime,
        string HostName,
        string OperatingSystem
    );

    /// <summary>
    /// Service status information
    /// </summary>
    public record ServiceStatusInfo(
        string Name,
        bool IsRunning,
        string Status,
        DateTimeOffset LastHeartbeat,
        int? ProcessId,
        long? MemoryUsage,
        double? CpuUsage,
        TimeSpan? Uptime
    );

    /// <summary>
    /// WSL status response DTO
    /// </summary>
    public record WslStatusResponse(
        bool IsRunning,
        bool IsInstalled,
        string Distribution,
        string Version,
        int WslVersion,
        long MemoryUsage,
        long DiskUsage,
        Dictionary<string, bool> Features,
        List<string> RunningServices,
        string DefaultUser,
        string MountPath
    );

    /// <summary>
    /// WSL health response
    /// </summary>
    public record WslHealthResponse(
        bool IsHealthy,
        List<string> Issues,
        DateTimeOffset CheckedAt
    );

    /// <summary>
    /// Settings response DTO
    /// </summary>
    public record SettingsResponse(
        string Id,
        string ProfileName,
        Dictionary<string, object> General,
        Dictionary<string, object> Models,
        Dictionary<string, object> Storage,
        Dictionary<string, object> Security,
        Dictionary<string, object> Notifications,
        Dictionary<string, object> Api,
        Dictionary<string, object> Ui,
        DateTimeOffset LastModified,
        string ModifiedBy,
        int Version
    );

    /// <summary>
    /// Generic service operation response
    /// </summary>
    public record ServiceOperationResponse(
        bool Success,
        string Message,
        string? ServiceName = null,
        string? Status = null,
        Dictionary<string, object>? Data = null
    );

    /// <summary>
    /// Service status list response
    /// </summary>
    public record ServiceStatusListResponse(
        List<ServiceStatusInfo> Services,
        int TotalCount,
        int RunningCount,
        int StoppedCount
    );

    /// <summary>
    /// Update settings request DTO
    /// </summary>
    public record UpdateSettingsRequest(
        Dictionary<string, object>? General = null,
        Dictionary<string, object>? Models = null,
        Dictionary<string, object>? Storage = null,
        Dictionary<string, object>? Security = null,
        Dictionary<string, object>? Notifications = null,
        Dictionary<string, object>? Api = null,
        Dictionary<string, object>? Ui = null
    );

    /// <summary>
    /// Test connection response DTO
    /// </summary>
    public record TestConnectionResponse(
        bool Success,
        string Message,
        string Endpoint,
        TimeSpan ResponseTime,
        int? StatusCode,
        Dictionary<string, object>? Details,
        DateTimeOffset TestedAt,
        string? ErrorDetails
    );
}