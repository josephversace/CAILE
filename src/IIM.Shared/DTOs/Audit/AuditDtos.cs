using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Audit event DTO for list responses
    /// </summary>
    public record AuditEventDto(
        string Id,
        string EventType,
        string UserId,
        string EntityId,
        string EntityType,
        string Action,
        DateTimeOffset Timestamp,
        bool Success,
        string? Details = null
    );

    /// <summary>
    /// Detailed audit event DTO
    /// </summary>
    public record AuditEventDetailDto(
        string Id,
        string EventType,
        string UserId,
        string EntityId,
        string EntityType,
        string Action,
        string Details,
        string IpAddress,
        string UserAgent,
        DateTimeOffset Timestamp,
        bool Success,
        string? ErrorMessage,
        Dictionary<string, object>? AdditionalData
    );

    /// <summary>
    /// Audit log response
    /// </summary>
    public record AuditLogResponse(
        List<AuditEventDto> Logs,
        int TotalCount,
        bool HasMore
    );

    /// <summary>
    /// Entity audit history
    /// </summary>
    public record EntityAuditHistoryDto(
        string EntityType,
        string EntityId,
        List<AuditEventDto> Events,
        DateTimeOffset? FirstActivity,
        DateTimeOffset? LastActivity,
        int TotalEvents
    );

    /// <summary>
    /// Audit statistics
    /// </summary>
    public record AuditStatisticsDto(
        int TotalEvents,
        int SuccessfulEvents,
        int FailedEvents,
        int UniqueUsers,
        Dictionary<string, int> EventsByType,
        Dictionary<string, int> EventsByAction,
        List<UserActivityDto> MostActiveUsers,
        DateRangeDto Period
    );

    /// <summary>
    /// User activity summary
    /// </summary>
    public record UserActivityDto(
        string UserId,
        int EventCount
    );

    /// <summary>
    /// Date range
    /// </summary>
    public record DateRangeDto(
        DateTime StartDate,
        DateTime EndDate
    );

    /// <summary>
    /// Export audit logs request
    /// </summary>
    public record ExportAuditLogsRequest(
        DateTime? StartDate,
        DateTime? EndDate,
        string? EntityType,
        string? UserId,
        string? EventType,
        string? Format = "json",
        int? MaxRecords = 10000
    );

    /// <summary>
    /// Export audit logs response
    /// </summary>
    public record ExportAuditLogsResponse(
        string FileName,
        string Data,
        int RecordCount,
        string Format
    );


}