
using IIM.Shared.Enums;
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs;

// Common DTOs
public record TimeRangeDto(
    DateTimeOffset Start,
    DateTimeOffset End
);

public record GeoLocationDto(
    double Latitude,
    double Longitude,
    double? Altitude = null,
    double? Accuracy = null,
    string? Address = null,
    string? Description = null
);

// Common Response DTOs
public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message = null,
    List<string>? Errors = null,
    Dictionary<string, object>? Metadata = null
);

public record PagedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNext,
    bool HasPrevious
);

public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance,
    Dictionary<string, object>? Extensions = null
);

public record ValidationErrorResponse(
    string Type,
    string Title,
    int Status,
    Dictionary<string, List<string>> Errors,
    string Instance
);

// SignalR Hub DTOs
public record HubMessage(
    string Type,
    object Payload,
    DateTimeOffset Timestamp,
    string? SenderId = null
);

public record CollaborationUpdate(
    string CaseId,
    string UserId,
    string Action,
    object? Data
);

public record ProgressUpdate(
    string OperationId,
    double Progress,
    string? Status = null,
    string? Message = null
);

// WebSocket DTOs
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import DTOs
public record ExportPackageDto(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResultDto(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync DTOs
public record FileSyncRequest(
    string WindowsPath,
    string WslPath
);

public record FileSyncResponse(
    bool Success,
    int FilesSynced,
    long BytesTransferred,
    TimeSpan Duration
);

// Processing Request DTOs
public record ProcessingRequest(
    string ProcessingType,
    Dictionary<string, object>? Parameters = null
);

public record ProcessingResponse(
    string ProcessedId,
    string OriginalId,
    string ProcessingType,
    bool Success,
    Dictionary<string, object>? Results
);

// Audit DTOs
public record AuditLogEntryDto(
    string Id,
    DateTimeOffset Timestamp,
    string Action,
    string Actor,
    string EntityType,
    string EntityId,
    Dictionary<string, object>? Changes,
    string? IpAddress,
    string? UserAgent
);

public record AuditLogResponse(
    List<AuditLogEntryDto> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRangeDto? TimeRange
);

#region Exports

public record ExportOptions(
    bool IncludeMetadata = true,
    bool IncludeChainOfCustody = true,
    bool IncludeHeaders = true,
    bool IncludeFooters = true,
    bool IncludeWatermark = true,
    bool IncludeCaseInfo = false,
    bool IncludeTimestamp = true,
    bool IncludeSignature = true,
    Dictionary<string, object>? CustomOptions = null
);

public record ExportResponseRequest(
    string ResponseId,
    ExportFormat Format,
    ExportOptions? Options = null,
    string? OutputPath = null
);

// Add if not already present
public record BatchExportRequest(
    List<string> ResponseIds,
    ExportFormat Format,
    ExportOptions? Options = null,
    bool CreateArchive = true
);

public record ExportResult(
    bool Success,
    string? FilePath,
    byte[]? Data,
    long FileSize,
    string? ErrorMessage,
    Dictionary<string, object>? Metadata
);

#endregion