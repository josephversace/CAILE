namespace IIM.Shared.Dtos;

public record CreateCaseRequest(
    string CaseNumber,
    string Name,
    string Type,
    string Description,
    string LeadInvestigator,
    List<string>? TeamMembers = null,
    string? Classification = null,
    Dictionary<string, object>? Metadata = null
);

public record UpdateCaseRequest(
    string? Name = null,
    string? Description = null,
    string? Status = null,
    string? LeadInvestigator = null,
    List<string>? TeamMembers = null,
    string? Classification = null,
    Dictionary<string, object>? Metadata = null
);

// Response 
public record CaseResponse(
    string Id,
    string CaseNumber,
    string Name,
    string Type,
    string Status,
    string Description,
    string LeadInvestigator,
    List<string> TeamMembers,
    string Classification,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int EvidenceCount,
    int SessionCount,
    int ReportCount,
    Dictionary<string, object> Metadata
);

public record CaseListResponse(
    List<CaseSummary> Cases,
    int TotalCount,
    int Page,
    int PageSize
);

public record CaseSummary(
    string Id,
    string CaseNumber,
    string Name,
    string Type,
    string Status,
    string Classification,
    DateTimeOffset UpdatedAt,
    int EvidenceCount,
    int ActiveSessions
);


public record UpdateCaseRequest(
    string? Name = null,
    string? Description = null,
    string? Status = null,
    string? LeadInvestigator = null,
    List<string>? TeamMembers = null,
    string? Classification = null,
    Dictionary<string, object>? Metadata = null
);

// Response 
public record CaseResponse(
    string Id,
    string CaseNumber,
    string Name,
    string Type,
    string Status,
    string Description,
    string LeadInvestigator,
    List<string> TeamMembers,
    string Classification,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int EvidenceCount,
    int SessionCount,
    int ReportCount,
    Dictionary<string, object> Metadata
);

public record CaseListResponse(
    List<CaseSummary> Cases,
    int TotalCount,
    int Page,
    int PageSize
);

public record CaseSummary(
    string Id,
    string CaseNumber,
    string Name,
    string Type,
    string Status,
    string Classification,
    DateTimeOffset UpdatedAt,
    int EvidenceCount,
    int ActiveSessions
);


public record CaseResponse(
    string Id,
    string CaseNumber,
    string Name,
    string Type,
    string Status,
    string Description,
    string LeadInvestigator,
    List<string> TeamMembers,
    string Classification,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int EvidenceCount,
    int SessionCount,
    int ReportCount,
    Dictionary<string, object> Metadata
);

public record CaseListResponse(
    List<CaseSummary> Cases,
    int TotalCount,
    int Page,
    int PageSize
);

public record CaseSummary(
    string Id,
    string CaseNumber,
    string Name,
    string Type,
    string Status,
    string Classification,
    DateTimeOffset UpdatedAt,
    int EvidenceCount,
    int ActiveSessions
);


public record CaseListResponse(
    List<CaseSummary> Cases,
    int TotalCount,
    int Page,
    int PageSize
);

public record CaseSummary(
    string Id,
    string CaseNumber,
    string Name,
    string Type,
    string Status,
    string Classification,
    DateTimeOffset UpdatedAt,
    int EvidenceCount,
    int ActiveSessions
);


public record CaseSummary(
    string Id,
    string CaseNumber,
    string Name,
    string Type,
    string Status,
    string Classification,
    DateTimeOffset UpdatedAt,
    int EvidenceCount,
    int ActiveSessions
);


public record TimeRange(
    DateTimeOffset Start,
    DateTimeOffset End
);

public record GeoLocation(
    double Latitude,
    double Longitude,
    double? Altitude = null,
    double? Accuracy = null,
    string? Address = null,
    string? Description = null
);

// Common Response 
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

// SignalR Hub 
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

// WebSocket 
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record GeoLocation(
    double Latitude,
    double Longitude,
    double? Altitude = null,
    double? Accuracy = null,
    string? Address = null,
    string? Description = null
);

// Common Response 
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

// SignalR Hub 
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

// WebSocket 
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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

// SignalR Hub 
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

// WebSocket 
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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

// SignalR Hub 
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

// WebSocket 
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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

// SignalR Hub 
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

// WebSocket 
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record ValidationErrorResponse(
    string Type,
    string Title,
    int Status,
    Dictionary<string, List<string>> Errors,
    string Instance
);

// SignalR Hub 
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

// WebSocket 
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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

// WebSocket 
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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

// WebSocket 
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record ProgressUpdate(
    string OperationId,
    double Progress,
    string? Status = null,
    string? Message = null
);

// WebSocket 
public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record StreamMessage(
    string Id,
    string Type,
    object Content,
    bool IsComplete = false
);

// Export/Import 
public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record ExportPackage(
    string Type,
    string Version,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    object Data,
    Dictionary<string, object>? Metadata
);

public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record ImportResult(
    bool Success,
    int ItemsImported,
    List<string> Warnings,
    List<string> Errors
);

// File Sync 
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record FileSyncResponse(
    bool Success,
    int FilesSynced,
    long BytesTransferred,
    TimeSpan Duration
);

// Processing Request 
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record ProcessingResponse(
    string ProcessedId,
    string OriginalId,
    string ProcessingType,
    bool Success,
    Dictionary<string, object>? Results
);

// Audit 
public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record AuditLogEntry(
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
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record AuditLogResponse(
    List<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    TimeRange? TimeRange
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


public record ExportResult(
    bool Success,
    string? FilePath,
    byte[]? Data,
    long FileSize,
    string? ErrorMessage,
    Dictionary<string, object>? Metadata
);

#endregion


    public record EmbeddingRequest(
        string Text,
        string? Model = null
    );

    /// <summary>
    /// Request  for batch text embedding generation
    /// </summary>
    /// <param name="Texts">List of texts to embed</param>
    /// <param name="Model">Optional model name</param>
    public record BatchEmbeddingRequest(
        List<string> Texts,
        string? Model = null
    );

    /// <summary>
    /// Request  for image embedding generation
    /// </summary>
    /// <param name="ImageData">Base64 encoded image data</param>
    /// <param name="Model">Optional model name (default: CLIP)</param>
    public record ImageEmbeddingRequest(
        string ImageData,
        string? Model = null
    );

    /// <summary>
    /// Request  for multi-modal embedding generation
    /// </summary>
    /// <param name="Text">Optional text input</param>
    /// <param name="ImageData">Optional base64 encoded image data</param>
    /// <param name="Model">Optional model name</param>
    public record MultiModalEmbeddingRequest(
        string? Text,
        string? ImageData,
        string? Model = null
    );

    /// <summary>
    /// Response  for embedding generation
    /// </summary>
    /// <param name="Embedding">Generated embedding vector</param>
    /// <param name="Dimensions">Number of dimensions in the embedding</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="ProcessingTime">Time taken to generate embedding</param>
    public record EmbeddingResponse(
        float[] Embedding,
        int Dimensions,
        string Model,
        TimeSpan ProcessingTime
    );

    /// <summary>
    /// Response  for batch embedding generation
    /// </summary>
    /// <param name="Embeddings">List of generated embedding vectors</param>
    /// <param name="Count">Number of embeddings generated</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="TotalProcessingTime">Total time for batch processing</param>
    public record BatchEmbeddingResponse(
        List<float[]> Embeddings,
        int Count,
        string Model,
        TimeSpan TotalProcessingTime
    );

    /// <summary>
    /// Information about an available embedding model
    /// </summary>
    /// <param name="Name">Model name</param>
    /// <param name="Type">Model type (text, image, multimodal)</param>
    /// <param name="Dimensions">Output dimensions</param>
    /// <param name="IsLoaded">Whether model is currently loaded</param>
    /// <param name="MemoryUsage">Memory usage in bytes</param>
    /// <param name="SupportedInputTypes">List of supported input types</param>
    public record EmbeddingModelInfo(
        string Name,
        string Type,
        int Dimensions,
        bool IsLoaded,
        long MemoryUsage,
        List<string> SupportedInputTypes
    );

    /// <summary>
    /// Response  for available models query
    /// </summary>
    /// <param name="Models">List of available embedding models</param>
    public record EmbeddingModelsResponse(
        List<EmbeddingModelInfo> Models
    );

    /// <summary>
    /// Service information and statistics
    /// </summary>
    /// <param name="Status">Service health status</param>
    /// <param name="LoadedModels">Currently loaded models</param>
    /// <param name="TotalMemoryUsage">Total memory used by loaded models</param>
    /// <param name="RequestsProcessed">Total requests processed</param>
    /// <param name="AverageLatencyMs">Average processing latency in milliseconds</param>
    /// <param name="Version">Service version</param>
    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request  for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record BatchEmbeddingRequest(
        List<string> Texts,
        string? Model = null
    );

    /// <summary>
    /// Request  for image embedding generation
    /// </summary>
    /// <param name="ImageData">Base64 encoded image data</param>
    /// <param name="Model">Optional model name (default: CLIP)</param>
    public record ImageEmbeddingRequest(
        string ImageData,
        string? Model = null
    );

    /// <summary>
    /// Request  for multi-modal embedding generation
    /// </summary>
    /// <param name="Text">Optional text input</param>
    /// <param name="ImageData">Optional base64 encoded image data</param>
    /// <param name="Model">Optional model name</param>
    public record MultiModalEmbeddingRequest(
        string? Text,
        string? ImageData,
        string? Model = null
    );

    /// <summary>
    /// Response  for embedding generation
    /// </summary>
    /// <param name="Embedding">Generated embedding vector</param>
    /// <param name="Dimensions">Number of dimensions in the embedding</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="ProcessingTime">Time taken to generate embedding</param>
    public record EmbeddingResponse(
        float[] Embedding,
        int Dimensions,
        string Model,
        TimeSpan ProcessingTime
    );

    /// <summary>
    /// Response  for batch embedding generation
    /// </summary>
    /// <param name="Embeddings">List of generated embedding vectors</param>
    /// <param name="Count">Number of embeddings generated</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="TotalProcessingTime">Total time for batch processing</param>
    public record BatchEmbeddingResponse(
        List<float[]> Embeddings,
        int Count,
        string Model,
        TimeSpan TotalProcessingTime
    );

    /// <summary>
    /// Information about an available embedding model
    /// </summary>
    /// <param name="Name">Model name</param>
    /// <param name="Type">Model type (text, image, multimodal)</param>
    /// <param name="Dimensions">Output dimensions</param>
    /// <param name="IsLoaded">Whether model is currently loaded</param>
    /// <param name="MemoryUsage">Memory usage in bytes</param>
    /// <param name="SupportedInputTypes">List of supported input types</param>
    public record EmbeddingModelInfo(
        string Name,
        string Type,
        int Dimensions,
        bool IsLoaded,
        long MemoryUsage,
        List<string> SupportedInputTypes
    );

    /// <summary>
    /// Response  for available models query
    /// </summary>
    /// <param name="Models">List of available embedding models</param>
    public record EmbeddingModelsResponse(
        List<EmbeddingModelInfo> Models
    );

    /// <summary>
    /// Service information and statistics
    /// </summary>
    /// <param name="Status">Service health status</param>
    /// <param name="LoadedModels">Currently loaded models</param>
    /// <param name="TotalMemoryUsage">Total memory used by loaded models</param>
    /// <param name="RequestsProcessed">Total requests processed</param>
    /// <param name="AverageLatencyMs">Average processing latency in milliseconds</param>
    /// <param name="Version">Service version</param>
    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request  for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record ImageEmbeddingRequest(
        string ImageData,
        string? Model = null
    );

    /// <summary>
    /// Request  for multi-modal embedding generation
    /// </summary>
    /// <param name="Text">Optional text input</param>
    /// <param name="ImageData">Optional base64 encoded image data</param>
    /// <param name="Model">Optional model name</param>
    public record MultiModalEmbeddingRequest(
        string? Text,
        string? ImageData,
        string? Model = null
    );

    /// <summary>
    /// Response  for embedding generation
    /// </summary>
    /// <param name="Embedding">Generated embedding vector</param>
    /// <param name="Dimensions">Number of dimensions in the embedding</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="ProcessingTime">Time taken to generate embedding</param>
    public record EmbeddingResponse(
        float[] Embedding,
        int Dimensions,
        string Model,
        TimeSpan ProcessingTime
    );

    /// <summary>
    /// Response  for batch embedding generation
    /// </summary>
    /// <param name="Embeddings">List of generated embedding vectors</param>
    /// <param name="Count">Number of embeddings generated</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="TotalProcessingTime">Total time for batch processing</param>
    public record BatchEmbeddingResponse(
        List<float[]> Embeddings,
        int Count,
        string Model,
        TimeSpan TotalProcessingTime
    );

    /// <summary>
    /// Information about an available embedding model
    /// </summary>
    /// <param name="Name">Model name</param>
    /// <param name="Type">Model type (text, image, multimodal)</param>
    /// <param name="Dimensions">Output dimensions</param>
    /// <param name="IsLoaded">Whether model is currently loaded</param>
    /// <param name="MemoryUsage">Memory usage in bytes</param>
    /// <param name="SupportedInputTypes">List of supported input types</param>
    public record EmbeddingModelInfo(
        string Name,
        string Type,
        int Dimensions,
        bool IsLoaded,
        long MemoryUsage,
        List<string> SupportedInputTypes
    );

    /// <summary>
    /// Response  for available models query
    /// </summary>
    /// <param name="Models">List of available embedding models</param>
    public record EmbeddingModelsResponse(
        List<EmbeddingModelInfo> Models
    );

    /// <summary>
    /// Service information and statistics
    /// </summary>
    /// <param name="Status">Service health status</param>
    /// <param name="LoadedModels">Currently loaded models</param>
    /// <param name="TotalMemoryUsage">Total memory used by loaded models</param>
    /// <param name="RequestsProcessed">Total requests processed</param>
    /// <param name="AverageLatencyMs">Average processing latency in milliseconds</param>
    /// <param name="Version">Service version</param>
    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request  for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record MultiModalEmbeddingRequest(
        string? Text,
        string? ImageData,
        string? Model = null
    );

    /// <summary>
    /// Response  for embedding generation
    /// </summary>
    /// <param name="Embedding">Generated embedding vector</param>
    /// <param name="Dimensions">Number of dimensions in the embedding</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="ProcessingTime">Time taken to generate embedding</param>
    public record EmbeddingResponse(
        float[] Embedding,
        int Dimensions,
        string Model,
        TimeSpan ProcessingTime
    );

    /// <summary>
    /// Response  for batch embedding generation
    /// </summary>
    /// <param name="Embeddings">List of generated embedding vectors</param>
    /// <param name="Count">Number of embeddings generated</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="TotalProcessingTime">Total time for batch processing</param>
    public record BatchEmbeddingResponse(
        List<float[]> Embeddings,
        int Count,
        string Model,
        TimeSpan TotalProcessingTime
    );

    /// <summary>
    /// Information about an available embedding model
    /// </summary>
    /// <param name="Name">Model name</param>
    /// <param name="Type">Model type (text, image, multimodal)</param>
    /// <param name="Dimensions">Output dimensions</param>
    /// <param name="IsLoaded">Whether model is currently loaded</param>
    /// <param name="MemoryUsage">Memory usage in bytes</param>
    /// <param name="SupportedInputTypes">List of supported input types</param>
    public record EmbeddingModelInfo(
        string Name,
        string Type,
        int Dimensions,
        bool IsLoaded,
        long MemoryUsage,
        List<string> SupportedInputTypes
    );

    /// <summary>
    /// Response  for available models query
    /// </summary>
    /// <param name="Models">List of available embedding models</param>
    public record EmbeddingModelsResponse(
        List<EmbeddingModelInfo> Models
    );

    /// <summary>
    /// Service information and statistics
    /// </summary>
    /// <param name="Status">Service health status</param>
    /// <param name="LoadedModels">Currently loaded models</param>
    /// <param name="TotalMemoryUsage">Total memory used by loaded models</param>
    /// <param name="RequestsProcessed">Total requests processed</param>
    /// <param name="AverageLatencyMs">Average processing latency in milliseconds</param>
    /// <param name="Version">Service version</param>
    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request  for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record EmbeddingResponse(
        float[] Embedding,
        int Dimensions,
        string Model,
        TimeSpan ProcessingTime
    );

    /// <summary>
    /// Response  for batch embedding generation
    /// </summary>
    /// <param name="Embeddings">List of generated embedding vectors</param>
    /// <param name="Count">Number of embeddings generated</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="TotalProcessingTime">Total time for batch processing</param>
    public record BatchEmbeddingResponse(
        List<float[]> Embeddings,
        int Count,
        string Model,
        TimeSpan TotalProcessingTime
    );

    /// <summary>
    /// Information about an available embedding model
    /// </summary>
    /// <param name="Name">Model name</param>
    /// <param name="Type">Model type (text, image, multimodal)</param>
    /// <param name="Dimensions">Output dimensions</param>
    /// <param name="IsLoaded">Whether model is currently loaded</param>
    /// <param name="MemoryUsage">Memory usage in bytes</param>
    /// <param name="SupportedInputTypes">List of supported input types</param>
    public record EmbeddingModelInfo(
        string Name,
        string Type,
        int Dimensions,
        bool IsLoaded,
        long MemoryUsage,
        List<string> SupportedInputTypes
    );

    /// <summary>
    /// Response  for available models query
    /// </summary>
    /// <param name="Models">List of available embedding models</param>
    public record EmbeddingModelsResponse(
        List<EmbeddingModelInfo> Models
    );

    /// <summary>
    /// Service information and statistics
    /// </summary>
    /// <param name="Status">Service health status</param>
    /// <param name="LoadedModels">Currently loaded models</param>
    /// <param name="TotalMemoryUsage">Total memory used by loaded models</param>
    /// <param name="RequestsProcessed">Total requests processed</param>
    /// <param name="AverageLatencyMs">Average processing latency in milliseconds</param>
    /// <param name="Version">Service version</param>
    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request  for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record BatchEmbeddingResponse(
        List<float[]> Embeddings,
        int Count,
        string Model,
        TimeSpan TotalProcessingTime
    );

    /// <summary>
    /// Information about an available embedding model
    /// </summary>
    /// <param name="Name">Model name</param>
    /// <param name="Type">Model type (text, image, multimodal)</param>
    /// <param name="Dimensions">Output dimensions</param>
    /// <param name="IsLoaded">Whether model is currently loaded</param>
    /// <param name="MemoryUsage">Memory usage in bytes</param>
    /// <param name="SupportedInputTypes">List of supported input types</param>
    public record EmbeddingModelInfo(
        string Name,
        string Type,
        int Dimensions,
        bool IsLoaded,
        long MemoryUsage,
        List<string> SupportedInputTypes
    );

    /// <summary>
    /// Response  for available models query
    /// </summary>
    /// <param name="Models">List of available embedding models</param>
    public record EmbeddingModelsResponse(
        List<EmbeddingModelInfo> Models
    );

    /// <summary>
    /// Service information and statistics
    /// </summary>
    /// <param name="Status">Service health status</param>
    /// <param name="LoadedModels">Currently loaded models</param>
    /// <param name="TotalMemoryUsage">Total memory used by loaded models</param>
    /// <param name="RequestsProcessed">Total requests processed</param>
    /// <param name="AverageLatencyMs">Average processing latency in milliseconds</param>
    /// <param name="Version">Service version</param>
    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request  for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record EmbeddingModelInfo(
        string Name,
        string Type,
        int Dimensions,
        bool IsLoaded,
        long MemoryUsage,
        List<string> SupportedInputTypes
    );

    /// <summary>
    /// Response  for available models query
    /// </summary>
    /// <param name="Models">List of available embedding models</param>
    public record EmbeddingModelsResponse(
        List<EmbeddingModelInfo> Models
    );

    /// <summary>
    /// Service information and statistics
    /// </summary>
    /// <param name="Status">Service health status</param>
    /// <param name="LoadedModels">Currently loaded models</param>
    /// <param name="TotalMemoryUsage">Total memory used by loaded models</param>
    /// <param name="RequestsProcessed">Total requests processed</param>
    /// <param name="AverageLatencyMs">Average processing latency in milliseconds</param>
    /// <param name="Version">Service version</param>
    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request  for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record EmbeddingModelsResponse(
        List<EmbeddingModelInfo> Models
    );

    /// <summary>
    /// Service information and statistics
    /// </summary>
    /// <param name="Status">Service health status</param>
    /// <param name="LoadedModels">Currently loaded models</param>
    /// <param name="TotalMemoryUsage">Total memory used by loaded models</param>
    /// <param name="RequestsProcessed">Total requests processed</param>
    /// <param name="AverageLatencyMs">Average processing latency in milliseconds</param>
    /// <param name="Version">Service version</param>
    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request  for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request  for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request  for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response  for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );



public record Entity(
    string Id,
    string Name,
    string Type,
    Dictionary<string, object> Properties,
    List<string>? Aliases,
    List<Relationship>? Relationships,
    List<string>? AssociatedCaseIds,
    double RiskScore,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    Dictionary<string, object>? Attributes
);

public record Relationship(
    string Id,
    string SourceEntityId,
    string TargetEntityId,
    string Type,
    double Strength,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    Dictionary<string, object>? Properties
);

public record EntityListResponse(
    List<Entity> Entities,
    int TotalCount,
    int Page,
    int PageSize
);


public record Relationship(
    string Id,
    string SourceEntityId,
    string TargetEntityId,
    string Type,
    double Strength,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    Dictionary<string, object>? Properties
);

public record EntityListResponse(
    List<Entity> Entities,
    int TotalCount,
    int Page,
    int PageSize
);


public record EntityListResponse(
    List<Entity> Entities,
    int TotalCount,
    int Page,
    int PageSize
);


public record EvidenceIngestRequest(
    string CaseNumber,
    string CollectedBy,
    string? CollectionLocation = null,
    string? DeviceSource = null,
    string? Description = null,
    DateTimeOffset? CollectionDate = null,
    Dictionary<string, string>? CustomFields = null
);

public record EvidenceProcessRequest(
    string ProcessingType,
    Dictionary<string, object>? Parameters = null
);

public record EvidenceExportRequest(
    string? ExportPath = null,
    bool IncludeProcessedVersions = true,
    bool GenerateVerificationScript = true,
    string? Format = "default"
);

// Response 
public record EvidenceResponse(
    string Id,
    string CaseId,
    string CaseNumber,
    string OriginalFileName,
    long FileSize,
    string Type,
    string Status,
    bool IntegrityValid,
    Dictionary<string, string> Hashes,
    string Signature,
    DateTimeOffset IngestTimestamp,
    string StoragePath,
    EvidenceMetadata Metadata,
    List<ChainOfCustodyEntry>? ChainOfCustody = null,
    List<ProcessedEvidence>? ProcessedVersions = null
);

public record EvidenceMetadata(
    string CaseNumber,
    string CollectedBy,
    DateTimeOffset CollectionDate,
    string? CollectionLocation,
    string? DeviceSource,
    string? Description,
    Dictionary<string, string>? CustomFields
);

public record ChainOfCustodyReport(
    string EvidenceId,
    string OriginalFileName,
    string CaseNumber,
    List<ChainOfCustodyEntry> ChainEntries,
    List<ProcessedEvidence> ProcessedVersions,
    bool IntegrityValid,
    string Signature,
    DateTimeOffset GeneratedAt,
    string GeneratedBy
);

public record ChainOfCustodyEntry(
    string Id,
    DateTimeOffset Timestamp,
    string Action,
    string Actor,
    string Details,
    string Hash,
    string PreviousHash
);

public record ProcessedEvidence(
    string Id,
    string OriginalEvidenceId,
    string ProcessingType,
    DateTimeOffset ProcessedTimestamp,
    string ProcessedBy,
    string ProcessedHash,
    string StoragePath,
    Dictionary<string, object>? ProcessingResults
);

public record EvidenceExport(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record EvidenceProcessRequest(
    string ProcessingType,
    Dictionary<string, object>? Parameters = null
);

public record EvidenceExportRequest(
    string? ExportPath = null,
    bool IncludeProcessedVersions = true,
    bool GenerateVerificationScript = true,
    string? Format = "default"
);

// Response 
public record EvidenceResponse(
    string Id,
    string CaseId,
    string CaseNumber,
    string OriginalFileName,
    long FileSize,
    string Type,
    string Status,
    bool IntegrityValid,
    Dictionary<string, string> Hashes,
    string Signature,
    DateTimeOffset IngestTimestamp,
    string StoragePath,
    EvidenceMetadata Metadata,
    List<ChainOfCustodyEntry>? ChainOfCustody = null,
    List<ProcessedEvidence>? ProcessedVersions = null
);

public record EvidenceMetadata(
    string CaseNumber,
    string CollectedBy,
    DateTimeOffset CollectionDate,
    string? CollectionLocation,
    string? DeviceSource,
    string? Description,
    Dictionary<string, string>? CustomFields
);

public record ChainOfCustodyReport(
    string EvidenceId,
    string OriginalFileName,
    string CaseNumber,
    List<ChainOfCustodyEntry> ChainEntries,
    List<ProcessedEvidence> ProcessedVersions,
    bool IntegrityValid,
    string Signature,
    DateTimeOffset GeneratedAt,
    string GeneratedBy
);

public record ChainOfCustodyEntry(
    string Id,
    DateTimeOffset Timestamp,
    string Action,
    string Actor,
    string Details,
    string Hash,
    string PreviousHash
);

public record ProcessedEvidence(
    string Id,
    string OriginalEvidenceId,
    string ProcessingType,
    DateTimeOffset ProcessedTimestamp,
    string ProcessedBy,
    string ProcessedHash,
    string StoragePath,
    Dictionary<string, object>? ProcessingResults
);

public record EvidenceExport(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record EvidenceExportRequest(
    string? ExportPath = null,
    bool IncludeProcessedVersions = true,
    bool GenerateVerificationScript = true,
    string? Format = "default"
);

// Response 
public record EvidenceResponse(
    string Id,
    string CaseId,
    string CaseNumber,
    string OriginalFileName,
    long FileSize,
    string Type,
    string Status,
    bool IntegrityValid,
    Dictionary<string, string> Hashes,
    string Signature,
    DateTimeOffset IngestTimestamp,
    string StoragePath,
    EvidenceMetadata Metadata,
    List<ChainOfCustodyEntry>? ChainOfCustody = null,
    List<ProcessedEvidence>? ProcessedVersions = null
);

public record EvidenceMetadata(
    string CaseNumber,
    string CollectedBy,
    DateTimeOffset CollectionDate,
    string? CollectionLocation,
    string? DeviceSource,
    string? Description,
    Dictionary<string, string>? CustomFields
);

public record ChainOfCustodyReport(
    string EvidenceId,
    string OriginalFileName,
    string CaseNumber,
    List<ChainOfCustodyEntry> ChainEntries,
    List<ProcessedEvidence> ProcessedVersions,
    bool IntegrityValid,
    string Signature,
    DateTimeOffset GeneratedAt,
    string GeneratedBy
);

public record ChainOfCustodyEntry(
    string Id,
    DateTimeOffset Timestamp,
    string Action,
    string Actor,
    string Details,
    string Hash,
    string PreviousHash
);

public record ProcessedEvidence(
    string Id,
    string OriginalEvidenceId,
    string ProcessingType,
    DateTimeOffset ProcessedTimestamp,
    string ProcessedBy,
    string ProcessedHash,
    string StoragePath,
    Dictionary<string, object>? ProcessingResults
);

public record EvidenceExport(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record EvidenceResponse(
    string Id,
    string CaseId,
    string CaseNumber,
    string OriginalFileName,
    long FileSize,
    string Type,
    string Status,
    bool IntegrityValid,
    Dictionary<string, string> Hashes,
    string Signature,
    DateTimeOffset IngestTimestamp,
    string StoragePath,
    EvidenceMetadata Metadata,
    List<ChainOfCustodyEntry>? ChainOfCustody = null,
    List<ProcessedEvidence>? ProcessedVersions = null
);

public record EvidenceMetadata(
    string CaseNumber,
    string CollectedBy,
    DateTimeOffset CollectionDate,
    string? CollectionLocation,
    string? DeviceSource,
    string? Description,
    Dictionary<string, string>? CustomFields
);

public record ChainOfCustodyReport(
    string EvidenceId,
    string OriginalFileName,
    string CaseNumber,
    List<ChainOfCustodyEntry> ChainEntries,
    List<ProcessedEvidence> ProcessedVersions,
    bool IntegrityValid,
    string Signature,
    DateTimeOffset GeneratedAt,
    string GeneratedBy
);

public record ChainOfCustodyEntry(
    string Id,
    DateTimeOffset Timestamp,
    string Action,
    string Actor,
    string Details,
    string Hash,
    string PreviousHash
);

public record ProcessedEvidence(
    string Id,
    string OriginalEvidenceId,
    string ProcessingType,
    DateTimeOffset ProcessedTimestamp,
    string ProcessedBy,
    string ProcessedHash,
    string StoragePath,
    Dictionary<string, object>? ProcessingResults
);

public record EvidenceExport(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record EvidenceMetadata(
    string CaseNumber,
    string CollectedBy,
    DateTimeOffset CollectionDate,
    string? CollectionLocation,
    string? DeviceSource,
    string? Description,
    Dictionary<string, string>? CustomFields
);

public record ChainOfCustodyReport(
    string EvidenceId,
    string OriginalFileName,
    string CaseNumber,
    List<ChainOfCustodyEntry> ChainEntries,
    List<ProcessedEvidence> ProcessedVersions,
    bool IntegrityValid,
    string Signature,
    DateTimeOffset GeneratedAt,
    string GeneratedBy
);

public record ChainOfCustodyEntry(
    string Id,
    DateTimeOffset Timestamp,
    string Action,
    string Actor,
    string Details,
    string Hash,
    string PreviousHash
);

public record ProcessedEvidence(
    string Id,
    string OriginalEvidenceId,
    string ProcessingType,
    DateTimeOffset ProcessedTimestamp,
    string ProcessedBy,
    string ProcessedHash,
    string StoragePath,
    Dictionary<string, object>? ProcessingResults
);

public record EvidenceExport(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record ChainOfCustodyReport(
    string EvidenceId,
    string OriginalFileName,
    string CaseNumber,
    List<ChainOfCustodyEntry> ChainEntries,
    List<ProcessedEvidence> ProcessedVersions,
    bool IntegrityValid,
    string Signature,
    DateTimeOffset GeneratedAt,
    string GeneratedBy
);

public record ChainOfCustodyEntry(
    string Id,
    DateTimeOffset Timestamp,
    string Action,
    string Actor,
    string Details,
    string Hash,
    string PreviousHash
);

public record ProcessedEvidence(
    string Id,
    string OriginalEvidenceId,
    string ProcessingType,
    DateTimeOffset ProcessedTimestamp,
    string ProcessedBy,
    string ProcessedHash,
    string StoragePath,
    Dictionary<string, object>? ProcessingResults
);

public record EvidenceExport(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record ChainOfCustodyEntry(
    string Id,
    DateTimeOffset Timestamp,
    string Action,
    string Actor,
    string Details,
    string Hash,
    string PreviousHash
);

public record ProcessedEvidence(
    string Id,
    string OriginalEvidenceId,
    string ProcessingType,
    DateTimeOffset ProcessedTimestamp,
    string ProcessedBy,
    string ProcessedHash,
    string StoragePath,
    Dictionary<string, object>? ProcessingResults
);

public record EvidenceExport(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record ProcessedEvidence(
    string Id,
    string OriginalEvidenceId,
    string ProcessingType,
    DateTimeOffset ProcessedTimestamp,
    string ProcessedBy,
    string ProcessedHash,
    string StoragePath,
    Dictionary<string, object>? ProcessingResults
);

public record EvidenceExport(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record EvidenceExport(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record EvidenceListResponse(
    List<EvidenceSummary> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


public record EvidenceSummary(
    string Id,
    string OriginalFileName,
    string CaseNumber,
    string Type,
    string Status,
    long FileSize,
    bool IntegrityValid,
    DateTimeOffset IngestTimestamp,
    int ChainLength,
    int ProcessedVersionsCount
);


    public class InitiateEvidenceUploadRequest
    {
        /// <summary>
        /// SHA-256 hash of the file computed by client
        /// </summary>
        public string FileHash { get; set; } = string.Empty;

        /// <summary>
        /// Original filename
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// MIME type of the file
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Evidence metadata including case number, collector, etc.
        /// </summary>
        public EvidenceMetadata Metadata { get; set; } = new();
    }


    public class InitiateEvidenceUploadResponse
    {
        /// <summary>
        /// Unique evidence ID for this upload
        /// </summary>
        public string EvidenceId { get; set; } = string.Empty;

        /// <summary>
        /// Status of the upload request
        /// </summary>
        public EvidenceUploadStatus Status { get; set; }

        /// <summary>
        /// Pre-signed URL for direct upload to MinIO (null if duplicate)
        /// </summary>
        public string? UploadUrl { get; set; }

        /// <summary>
        /// Expiration time for the upload URL
        /// </summary>
        public DateTimeOffset? UploadUrlExpires { get; set; }

        /// <summary>
        /// If duplicate, reference to existing evidence
        /// </summary>
        public string? DuplicateEvidenceId { get; set; }

        /// <summary>
        /// If duplicate, details about original upload
        /// </summary>
        public DuplicateInfo? DuplicateInfo { get; set; }

        /// <summary>
        /// Additional headers required for upload (if any)
        /// </summary>
        public Dictionary<string, string>? RequiredHeaders { get; set; }
    }


    public class DuplicateInfo
    {
        /// <summary>
        /// Original evidence ID
        /// </summary>
        public string OriginalEvidenceId { get; set; } = string.Empty;

        /// <summary>
        /// When the original was uploaded
        /// </summary>
        public DateTimeOffset OriginalUploadDate { get; set; }

        /// <summary>
        /// Who uploaded the original
        /// </summary>
        public string OriginalUploadedBy { get; set; } = string.Empty;

        /// <summary>
        /// Case number of original upload
        /// </summary>
        public string OriginalCaseNumber { get; set; } = string.Empty;

        /// <summary>
        /// How many times this file has been seen
        /// </summary>
        public int DuplicateCount { get; set; }
    }


    public class ConfirmEvidenceUploadRequest
    {
        /// <summary>
        /// Evidence ID from initiate response
        /// </summary>
        public string EvidenceId { get; set; } = string.Empty;

        /// <summary>
        /// ETag returned by MinIO after upload
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// Client can re-confirm the hash
        /// </summary>
        public string? ClientHash { get; set; }
    }


    public class ConfirmEvidenceUploadResponse
    {
        /// <summary>
        /// Whether the upload was successfully verified
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Final status of the evidence
        /// </summary>
        public EvidenceStatus Status { get; set; }

        /// <summary>
        /// Error message if verification failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Server-computed hash for verification
        /// </summary>
        public string? ServerHash { get; set; }

        /// <summary>
        /// Whether hashes matched (if verification was performed)
        /// </summary>
        public bool? HashesMatch { get; set; }
    }


public record CreateSessionRequest(
    string CaseId,
    string Title,
    string InvestigationType,
    Dictionary<string, ModelConfig>? Models = null,
    List<string>? Enableols = null,
    SessionContext? Context = null
);

public record InvestigationQuery(
    string SessionId,
    string Text,
    List<Attachment>? Attachments = null,
    Dictionary<string, object>? Parameters = null,
    List<string>? Requesteols = null
);

public record SessionContext(
    string CaseId,
    List<string>? RelevantEvidenceIds = null,
    Dictionary<string, object>? Variables = null,
    TimeRange? FocusTimeRange = null
);

public record ModelConfig(
    string ModelId,
    string Provider,
    string Type,
    Dictionary<string, object>? Parameters = null
);

public record Attachment(
    string FileName,
    string ContentType,
    long Size,
    string Type,
    string? Base64Content = null,
    string? Url = null
);

// Response 
public record InvestigationResponse(
    string Id,
    string SessionId,
    string QueryId,
    string Message,
    RAGSearchResult? RAGResults,
    List<TranscriptionResult>? Transcriptions,
    List<ImageAnalysisResult>? ImageAnalyses,
    List<ToolResult> ToolResults,
    List<Citation> Citations,
    List<string> EvidenceIds,
    List<string> EntityIds,
    double Confidence,
    string? FineTuneJobId,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null
);

public record InvestigationMessage(
    string Id,
    string Role,
    string Content,
    List<Attachment>? Attachments,
    List<ToolResult>? ToolResults,
    List<Citation>? Citations,
    DateTimeOffset Timestamp,
    string? ModelUsed,
    Dictionary<string, object>? Metadata
);

public record TranscriptionResult(
     string Id,
     string EvidenceId,
     string Text,
     string Language,
     double Confidence,
     List<TranscriptionSegment> Segments,
     Dictionary<string, object>? Metadata,
     // Add missing properties
     TimeSpan? Duration = null,
     int? AudioFileId = null
 );

public record TranscriptionSegment(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record InvestigationQuery(
    string SessionId,
    string Text,
    List<Attachment>? Attachments = null,
    Dictionary<string, object>? Parameters = null,
    List<string>? Requesteols = null
);

public record SessionContext(
    string CaseId,
    List<string>? RelevantEvidenceIds = null,
    Dictionary<string, object>? Variables = null,
    TimeRange? FocusTimeRange = null
);

public record ModelConfig(
    string ModelId,
    string Provider,
    string Type,
    Dictionary<string, object>? Parameters = null
);

public record Attachment(
    string FileName,
    string ContentType,
    long Size,
    string Type,
    string? Base64Content = null,
    string? Url = null
);

// Response 
public record InvestigationResponse(
    string Id,
    string SessionId,
    string QueryId,
    string Message,
    RAGSearchResult? RAGResults,
    List<TranscriptionResult>? Transcriptions,
    List<ImageAnalysisResult>? ImageAnalyses,
    List<ToolResult> ToolResults,
    List<Citation> Citations,
    List<string> EvidenceIds,
    List<string> EntityIds,
    double Confidence,
    string? FineTuneJobId,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null
);

public record InvestigationMessage(
    string Id,
    string Role,
    string Content,
    List<Attachment>? Attachments,
    List<ToolResult>? ToolResults,
    List<Citation>? Citations,
    DateTimeOffset Timestamp,
    string? ModelUsed,
    Dictionary<string, object>? Metadata
);

public record TranscriptionResult(
     string Id,
     string EvidenceId,
     string Text,
     string Language,
     double Confidence,
     List<TranscriptionSegment> Segments,
     Dictionary<string, object>? Metadata,
     // Add missing properties
     TimeSpan? Duration = null,
     int? AudioFileId = null
 );

public record TranscriptionSegment(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record SessionContext(
    string CaseId,
    List<string>? RelevantEvidenceIds = null,
    Dictionary<string, object>? Variables = null,
    TimeRange? FocusTimeRange = null
);

public record ModelConfig(
    string ModelId,
    string Provider,
    string Type,
    Dictionary<string, object>? Parameters = null
);

public record Attachment(
    string FileName,
    string ContentType,
    long Size,
    string Type,
    string? Base64Content = null,
    string? Url = null
);

// Response 
public record InvestigationResponse(
    string Id,
    string SessionId,
    string QueryId,
    string Message,
    RAGSearchResult? RAGResults,
    List<TranscriptionResult>? Transcriptions,
    List<ImageAnalysisResult>? ImageAnalyses,
    List<ToolResult> ToolResults,
    List<Citation> Citations,
    List<string> EvidenceIds,
    List<string> EntityIds,
    double Confidence,
    string? FineTuneJobId,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null
);

public record InvestigationMessage(
    string Id,
    string Role,
    string Content,
    List<Attachment>? Attachments,
    List<ToolResult>? ToolResults,
    List<Citation>? Citations,
    DateTimeOffset Timestamp,
    string? ModelUsed,
    Dictionary<string, object>? Metadata
);

public record TranscriptionResult(
     string Id,
     string EvidenceId,
     string Text,
     string Language,
     double Confidence,
     List<TranscriptionSegment> Segments,
     Dictionary<string, object>? Metadata,
     // Add missing properties
     TimeSpan? Duration = null,
     int? AudioFileId = null
 );

public record TranscriptionSegment(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record ModelConfig(
    string ModelId,
    string Provider,
    string Type,
    Dictionary<string, object>? Parameters = null
);

public record Attachment(
    string FileName,
    string ContentType,
    long Size,
    string Type,
    string? Base64Content = null,
    string? Url = null
);

// Response 
public record InvestigationResponse(
    string Id,
    string SessionId,
    string QueryId,
    string Message,
    RAGSearchResult? RAGResults,
    List<TranscriptionResult>? Transcriptions,
    List<ImageAnalysisResult>? ImageAnalyses,
    List<ToolResult> ToolResults,
    List<Citation> Citations,
    List<string> EvidenceIds,
    List<string> EntityIds,
    double Confidence,
    string? FineTuneJobId,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null
);

public record InvestigationMessage(
    string Id,
    string Role,
    string Content,
    List<Attachment>? Attachments,
    List<ToolResult>? ToolResults,
    List<Citation>? Citations,
    DateTimeOffset Timestamp,
    string? ModelUsed,
    Dictionary<string, object>? Metadata
);

public record TranscriptionResult(
     string Id,
     string EvidenceId,
     string Text,
     string Language,
     double Confidence,
     List<TranscriptionSegment> Segments,
     Dictionary<string, object>? Metadata,
     // Add missing properties
     TimeSpan? Duration = null,
     int? AudioFileId = null
 );

public record TranscriptionSegment(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record Attachment(
    string FileName,
    string ContentType,
    long Size,
    string Type,
    string? Base64Content = null,
    string? Url = null
);

// Response 
public record InvestigationResponse(
    string Id,
    string SessionId,
    string QueryId,
    string Message,
    RAGSearchResult? RAGResults,
    List<TranscriptionResult>? Transcriptions,
    List<ImageAnalysisResult>? ImageAnalyses,
    List<ToolResult> ToolResults,
    List<Citation> Citations,
    List<string> EvidenceIds,
    List<string> EntityIds,
    double Confidence,
    string? FineTuneJobId,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null
);

public record InvestigationMessage(
    string Id,
    string Role,
    string Content,
    List<Attachment>? Attachments,
    List<ToolResult>? ToolResults,
    List<Citation>? Citations,
    DateTimeOffset Timestamp,
    string? ModelUsed,
    Dictionary<string, object>? Metadata
);

public record TranscriptionResult(
     string Id,
     string EvidenceId,
     string Text,
     string Language,
     double Confidence,
     List<TranscriptionSegment> Segments,
     Dictionary<string, object>? Metadata,
     // Add missing properties
     TimeSpan? Duration = null,
     int? AudioFileId = null
 );

public record TranscriptionSegment(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record InvestigationResponse(
    string Id,
    string SessionId,
    string QueryId,
    string Message,
    RAGSearchResult? RAGResults,
    List<TranscriptionResult>? Transcriptions,
    List<ImageAnalysisResult>? ImageAnalyses,
    List<ToolResult> ToolResults,
    List<Citation> Citations,
    List<string> EvidenceIds,
    List<string> EntityIds,
    double Confidence,
    string? FineTuneJobId,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null
);

public record InvestigationMessage(
    string Id,
    string Role,
    string Content,
    List<Attachment>? Attachments,
    List<ToolResult>? ToolResults,
    List<Citation>? Citations,
    DateTimeOffset Timestamp,
    string? ModelUsed,
    Dictionary<string, object>? Metadata
);

public record TranscriptionResult(
     string Id,
     string EvidenceId,
     string Text,
     string Language,
     double Confidence,
     List<TranscriptionSegment> Segments,
     Dictionary<string, object>? Metadata,
     // Add missing properties
     TimeSpan? Duration = null,
     int? AudioFileId = null
 );

public record TranscriptionSegment(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record InvestigationMessage(
    string Id,
    string Role,
    string Content,
    List<Attachment>? Attachments,
    List<ToolResult>? ToolResults,
    List<Citation>? Citations,
    DateTimeOffset Timestamp,
    string? ModelUsed,
    Dictionary<string, object>? Metadata
);

public record TranscriptionResult(
     string Id,
     string EvidenceId,
     string Text,
     string Language,
     double Confidence,
     List<TranscriptionSegment> Segments,
     Dictionary<string, object>? Metadata,
     // Add missing properties
     TimeSpan? Duration = null,
     int? AudioFileId = null
 );

public record TranscriptionSegment(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record TranscriptionResult(
     string Id,
     string EvidenceId,
     string Text,
     string Language,
     double Confidence,
     List<TranscriptionSegment> Segments,
     Dictionary<string, object>? Metadata,
     // Add missing properties
     TimeSpan? Duration = null,
     int? AudioFileId = null
 );

public record TranscriptionSegment(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record TranscriptionSegment(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record ImageAnalysisResult(
    string Id,
    string EvidenceId,
    List<DetectedObject> Objects,
    List<DetectedFace> Faces,
    List<string> Tags,
    List<SimilarImage> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record DetectedObject(
    string Label,
    double Confidence,
    BoundingBox BoundingBox
);

public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record DetectedFace(
    string Id,
    BoundingBox BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record BoundingBox(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record SimilarImage(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record ToolResult(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record Citation(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record Visualization(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);


public record ModelLoadRequest(
    string ModelId,
    string? ModelPath = null,
    string? Provider = null,
    Dictionary<string, object>? Options = null
);

public record ModelConfigurationRequest(
    string ModelId,
    Dictionary<string, object> Parameters
);

public record InferenceRequest(
    string ModelId,
    object Input,
    Dictionary<string, object>? Parameters = null,
    HashSet<string>? Tags = null,
    int Priority = 1,
    bool Stream = false


);




// Response 
public record ModelInfo(
    string Id,
    string Name,
    string Type,
    string Provider,
    string Status,
    long MemoryUsage,
    string? LoadedPath,
    DateTimeOffset? LoadedAt,
    ModelCapabilities Capabilities,
    Dictionary<string, object>? Metadata = null
);

public record ModelCapabilities(
    int MaxContextLength,
    List<string> SupportedLanguages,
    List<string>? SpecialFeatures,
    bool SupportsStreaming,
    bool SupportsFineTuning,
    bool SupportsMultiModal,
    Dictionary<string, object>? CustomCapabilities = null
);

public record ModelListResponse(
    List<ModelInfo> Models,
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedCount,
    int AvailableCount
);



public record InferenceResponse(
    string ModelId,
    object Output,
    TimeSpan InferenceTime,
    int? TokensUsed = null,
    Dictionary<string, object>? Metadata = null
);



public record InferencePipelineStats(
    long TotalRequests,
    long CompletedRequests,
    long FailedRequests,
    int PendingRequests,
    int ActiveRequests,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    Dictionary<string, long> RequestsByModel
);

public record OrchestratorStats(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStats> Models
);

public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record ModelConfigurationRequest(
    string ModelId,
    Dictionary<string, object> Parameters
);

public record InferenceRequest(
    string ModelId,
    object Input,
    Dictionary<string, object>? Parameters = null,
    HashSet<string>? Tags = null,
    int Priority = 1,
    bool Stream = false


);




// Response 
public record ModelInfo(
    string Id,
    string Name,
    string Type,
    string Provider,
    string Status,
    long MemoryUsage,
    string? LoadedPath,
    DateTimeOffset? LoadedAt,
    ModelCapabilities Capabilities,
    Dictionary<string, object>? Metadata = null
);

public record ModelCapabilities(
    int MaxContextLength,
    List<string> SupportedLanguages,
    List<string>? SpecialFeatures,
    bool SupportsStreaming,
    bool SupportsFineTuning,
    bool SupportsMultiModal,
    Dictionary<string, object>? CustomCapabilities = null
);

public record ModelListResponse(
    List<ModelInfo> Models,
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedCount,
    int AvailableCount
);



public record InferenceResponse(
    string ModelId,
    object Output,
    TimeSpan InferenceTime,
    int? TokensUsed = null,
    Dictionary<string, object>? Metadata = null
);



public record InferencePipelineStats(
    long TotalRequests,
    long CompletedRequests,
    long FailedRequests,
    int PendingRequests,
    int ActiveRequests,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    Dictionary<string, long> RequestsByModel
);

public record OrchestratorStats(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStats> Models
);

public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record InferenceRequest(
    string ModelId,
    object Input,
    Dictionary<string, object>? Parameters = null,
    HashSet<string>? Tags = null,
    int Priority = 1,
    bool Stream = false


);




// Response 
public record ModelInfo(
    string Id,
    string Name,
    string Type,
    string Provider,
    string Status,
    long MemoryUsage,
    string? LoadedPath,
    DateTimeOffset? LoadedAt,
    ModelCapabilities Capabilities,
    Dictionary<string, object>? Metadata = null
);

public record ModelCapabilities(
    int MaxContextLength,
    List<string> SupportedLanguages,
    List<string>? SpecialFeatures,
    bool SupportsStreaming,
    bool SupportsFineTuning,
    bool SupportsMultiModal,
    Dictionary<string, object>? CustomCapabilities = null
);

public record ModelListResponse(
    List<ModelInfo> Models,
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedCount,
    int AvailableCount
);



public record InferenceResponse(
    string ModelId,
    object Output,
    TimeSpan InferenceTime,
    int? TokensUsed = null,
    Dictionary<string, object>? Metadata = null
);



public record InferencePipelineStats(
    long TotalRequests,
    long CompletedRequests,
    long FailedRequests,
    int PendingRequests,
    int ActiveRequests,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    Dictionary<string, long> RequestsByModel
);

public record OrchestratorStats(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStats> Models
);

public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record ModelInfo(
    string Id,
    string Name,
    string Type,
    string Provider,
    string Status,
    long MemoryUsage,
    string? LoadedPath,
    DateTimeOffset? LoadedAt,
    ModelCapabilities Capabilities,
    Dictionary<string, object>? Metadata = null
);

public record ModelCapabilities(
    int MaxContextLength,
    List<string> SupportedLanguages,
    List<string>? SpecialFeatures,
    bool SupportsStreaming,
    bool SupportsFineTuning,
    bool SupportsMultiModal,
    Dictionary<string, object>? CustomCapabilities = null
);

public record ModelListResponse(
    List<ModelInfo> Models,
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedCount,
    int AvailableCount
);



public record InferenceResponse(
    string ModelId,
    object Output,
    TimeSpan InferenceTime,
    int? TokensUsed = null,
    Dictionary<string, object>? Metadata = null
);



public record InferencePipelineStats(
    long TotalRequests,
    long CompletedRequests,
    long FailedRequests,
    int PendingRequests,
    int ActiveRequests,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    Dictionary<string, long> RequestsByModel
);

public record OrchestratorStats(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStats> Models
);

public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record ModelCapabilities(
    int MaxContextLength,
    List<string> SupportedLanguages,
    List<string>? SpecialFeatures,
    bool SupportsStreaming,
    bool SupportsFineTuning,
    bool SupportsMultiModal,
    Dictionary<string, object>? CustomCapabilities = null
);

public record ModelListResponse(
    List<ModelInfo> Models,
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedCount,
    int AvailableCount
);



public record InferenceResponse(
    string ModelId,
    object Output,
    TimeSpan InferenceTime,
    int? TokensUsed = null,
    Dictionary<string, object>? Metadata = null
);



public record InferencePipelineStats(
    long TotalRequests,
    long CompletedRequests,
    long FailedRequests,
    int PendingRequests,
    int ActiveRequests,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    Dictionary<string, long> RequestsByModel
);

public record OrchestratorStats(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStats> Models
);

public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record ModelListResponse(
    List<ModelInfo> Models,
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedCount,
    int AvailableCount
);



public record InferenceResponse(
    string ModelId,
    object Output,
    TimeSpan InferenceTime,
    int? TokensUsed = null,
    Dictionary<string, object>? Metadata = null
);



public record InferencePipelineStats(
    long TotalRequests,
    long CompletedRequests,
    long FailedRequests,
    int PendingRequests,
    int ActiveRequests,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    Dictionary<string, long> RequestsByModel
);

public record OrchestratorStats(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStats> Models
);

public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record InferenceResponse(
    string ModelId,
    object Output,
    TimeSpan InferenceTime,
    int? TokensUsed = null,
    Dictionary<string, object>? Metadata = null
);



public record InferencePipelineStats(
    long TotalRequests,
    long CompletedRequests,
    long FailedRequests,
    int PendingRequests,
    int ActiveRequests,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    Dictionary<string, long> RequestsByModel
);

public record OrchestratorStats(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStats> Models
);

public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record InferencePipelineStats(
    long TotalRequests,
    long CompletedRequests,
    long FailedRequests,
    int PendingRequests,
    int ActiveRequests,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    Dictionary<string, long> RequestsByModel
);

public record OrchestratorStats(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStats> Models
);

public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record OrchestratorStats(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStats> Models
);

public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record ModelStats(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record FineTuneStatus(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);


public record WslStatus(
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

public record WslDistro(
    string Name,
    int Version,
    string State,
    string? DefaultUser,
    string? BasePath
);

public record WslNetworkInfo(
    string DistroName,
    string? WslIpAddress,
    string? WindowsHostIp,
    bool IsConnected,
    Dictionary<string, string> ServiceEndpoints
);

public record WslHealthCheck(
    bool IsHealthy,
    bool WslReady,
    bool DistroRunning,
    bool ServicesHealthy,
    bool NetworkConnected,
    List<string> Issues,
    DateTimeOffset CheckedAt
);

// Service Management 
public record ServiceStatus(
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

public record ServiceConfig(
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
    Dictionary<string, ServiceStatus> Services,
    int TotalServices,
    int RunningServices,
    int HealthyServices,
    DateTimeOffset CheckedAt
);

// Evidence Configuration 
public record EvidenceConfiguration(
    string StorePath,
    bool EnableEncryption,
    bool RequireDualControl,
    int MaxFileSizeMb,
    List<string>? AllowedFileTypes,
    Dictionary<string, object>? SecuritySettings
);


public record WslDistro(
    string Name,
    int Version,
    string State,
    string? DefaultUser,
    string? BasePath
);

public record WslNetworkInfo(
    string DistroName,
    string? WslIpAddress,
    string? WindowsHostIp,
    bool IsConnected,
    Dictionary<string, string> ServiceEndpoints
);

public record WslHealthCheck(
    bool IsHealthy,
    bool WslReady,
    bool DistroRunning,
    bool ServicesHealthy,
    bool NetworkConnected,
    List<string> Issues,
    DateTimeOffset CheckedAt
);

// Service Management 
public record ServiceStatus(
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

public record ServiceConfig(
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
    Dictionary<string, ServiceStatus> Services,
    int TotalServices,
    int RunningServices,
    int HealthyServices,
    DateTimeOffset CheckedAt
);

// Evidence Configuration 
public record EvidenceConfiguration(
    string StorePath,
    bool EnableEncryption,
    bool RequireDualControl,
    int MaxFileSizeMb,
    List<string>? AllowedFileTypes,
    Dictionary<string, object>? SecuritySettings
);


public record WslNetworkInfo(
    string DistroName,
    string? WslIpAddress,
    string? WindowsHostIp,
    bool IsConnected,
    Dictionary<string, string> ServiceEndpoints
);

public record WslHealthCheck(
    bool IsHealthy,
    bool WslReady,
    bool DistroRunning,
    bool ServicesHealthy,
    bool NetworkConnected,
    List<string> Issues,
    DateTimeOffset CheckedAt
);

// Service Management 
public record ServiceStatus(
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

public record ServiceConfig(
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
    Dictionary<string, ServiceStatus> Services,
    int TotalServices,
    int RunningServices,
    int HealthyServices,
    DateTimeOffset CheckedAt
);

// Evidence Configuration 
public record EvidenceConfiguration(
    string StorePath,
    bool EnableEncryption,
    bool RequireDualControl,
    int MaxFileSizeMb,
    List<string>? AllowedFileTypes,
    Dictionary<string, object>? SecuritySettings
);


public record WslHealthCheck(
    bool IsHealthy,
    bool WslReady,
    bool DistroRunning,
    bool ServicesHealthy,
    bool NetworkConnected,
    List<string> Issues,
    DateTimeOffset CheckedAt
);

// Service Management 
public record ServiceStatus(
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

public record ServiceConfig(
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
    Dictionary<string, ServiceStatus> Services,
    int TotalServices,
    int RunningServices,
    int HealthyServices,
    DateTimeOffset CheckedAt
);

// Evidence Configuration 
public record EvidenceConfiguration(
    string StorePath,
    bool EnableEncryption,
    bool RequireDualControl,
    int MaxFileSizeMb,
    List<string>? AllowedFileTypes,
    Dictionary<string, object>? SecuritySettings
);


public record ServiceStatus(
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

public record ServiceConfig(
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
    Dictionary<string, ServiceStatus> Services,
    int TotalServices,
    int RunningServices,
    int HealthyServices,
    DateTimeOffset CheckedAt
);

// Evidence Configuration 
public record EvidenceConfiguration(
    string StorePath,
    bool EnableEncryption,
    bool RequireDualControl,
    int MaxFileSizeMb,
    List<string>? AllowedFileTypes,
    Dictionary<string, object>? SecuritySettings
);


public record ServiceConfig(
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
    Dictionary<string, ServiceStatus> Services,
    int TotalServices,
    int RunningServices,
    int HealthyServices,
    DateTimeOffset CheckedAt
);

// Evidence Configuration 
public record EvidenceConfiguration(
    string StorePath,
    bool EnableEncryption,
    bool RequireDualControl,
    int MaxFileSizeMb,
    List<string>? AllowedFileTypes,
    Dictionary<string, object>? SecuritySettings
);


public record ServiceListResponse(
    Dictionary<string, ServiceStatus> Services,
    int TotalServices,
    int RunningServices,
    int HealthyServices,
    DateTimeOffset CheckedAt
);

// Evidence Configuration 
public record EvidenceConfiguration(
    string StorePath,
    bool EnableEncryption,
    bool RequireDualControl,
    int MaxFileSizeMb,
    List<string>? AllowedFileTypes,
    Dictionary<string, object>? SecuritySettings
);


public record EvidenceConfiguration(
    string StorePath,
    bool EnableEncryption,
    bool RequireDualControl,
    int MaxFileSizeMb,
    List<string>? AllowedFileTypes,
    Dictionary<string, object>? SecuritySettings
);


public class ProxyConfig
{
    /// <summary>
    /// Proxy type (e.g., "socks5h", "http", "https").
    /// </summary>
    public string ProxyType { get; set; } = "socks5h";
    /// <summary>
    /// Proxy host IP address or DNS name.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";
    /// <summary>
    /// Proxy port number.
    /// </summary>
    public int Port { get; set; } = 9050;
}


public record RAGIndexRequest(
    string CaseId,
    string Content,
    string? SourceId = null,
    string? SourceType = null,
    Dictionary<string, object>? Metadata = null,
    bool UseSentenceChunking = true
);

public record RAGSearchRequest(
    string Query,
    string? CaseId = null,
    int TopK = 5,
    double MinRelevance = 0.5,
    TimeRange? TimeRange = null,
    List<string>? FilterTags = null
);

public record RAGGenerateRequest(
    string Query,
    string CaseId,
    string? ModelId = null,
    int MaxTokens = 2048,
    double Temperature = 0.3,
    bool VerifyFactualAccuracy = true,
    string? SystemPrompt = null
);

// Response 
public record RAGSearchResult(
    List<RAGDocument> Documents,
    List<Entity> Entities,
    List<Relationship> Relationships,
    KnowledgeGraph? KnowledgeGraph,
    QueryUnderstanding QueryUnderstanding,
    List<string> SuggestedFollowUps,
    Dictionary<string, object>? CaseContext
);

public record RAGDocument(
    string Id,
    string Content,
    double Relevance,
    string? SourceId,
    string? SourceType,
    Dictionary<string, object>? Metadata,
    List<int>? ChunkIndices
);

public record KnowledgeGraph(
    List<GraphNode> Nodes,
    List<GraphEdge> Edges,
    Dictionary<string, object>? Properties
);

public record GraphNode(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties
);

public record GraphEdge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);

public record QueryUnderstanding(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record RAGSearchRequest(
    string Query,
    string? CaseId = null,
    int TopK = 5,
    double MinRelevance = 0.5,
    TimeRange? TimeRange = null,
    List<string>? FilterTags = null
);

public record RAGGenerateRequest(
    string Query,
    string CaseId,
    string? ModelId = null,
    int MaxTokens = 2048,
    double Temperature = 0.3,
    bool VerifyFactualAccuracy = true,
    string? SystemPrompt = null
);

// Response 
public record RAGSearchResult(
    List<RAGDocument> Documents,
    List<Entity> Entities,
    List<Relationship> Relationships,
    KnowledgeGraph? KnowledgeGraph,
    QueryUnderstanding QueryUnderstanding,
    List<string> SuggestedFollowUps,
    Dictionary<string, object>? CaseContext
);

public record RAGDocument(
    string Id,
    string Content,
    double Relevance,
    string? SourceId,
    string? SourceType,
    Dictionary<string, object>? Metadata,
    List<int>? ChunkIndices
);

public record KnowledgeGraph(
    List<GraphNode> Nodes,
    List<GraphEdge> Edges,
    Dictionary<string, object>? Properties
);

public record GraphNode(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties
);

public record GraphEdge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);

public record QueryUnderstanding(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record RAGGenerateRequest(
    string Query,
    string CaseId,
    string? ModelId = null,
    int MaxTokens = 2048,
    double Temperature = 0.3,
    bool VerifyFactualAccuracy = true,
    string? SystemPrompt = null
);

// Response 
public record RAGSearchResult(
    List<RAGDocument> Documents,
    List<Entity> Entities,
    List<Relationship> Relationships,
    KnowledgeGraph? KnowledgeGraph,
    QueryUnderstanding QueryUnderstanding,
    List<string> SuggestedFollowUps,
    Dictionary<string, object>? CaseContext
);

public record RAGDocument(
    string Id,
    string Content,
    double Relevance,
    string? SourceId,
    string? SourceType,
    Dictionary<string, object>? Metadata,
    List<int>? ChunkIndices
);

public record KnowledgeGraph(
    List<GraphNode> Nodes,
    List<GraphEdge> Edges,
    Dictionary<string, object>? Properties
);

public record GraphNode(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties
);

public record GraphEdge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);

public record QueryUnderstanding(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record RAGSearchResult(
    List<RAGDocument> Documents,
    List<Entity> Entities,
    List<Relationship> Relationships,
    KnowledgeGraph? KnowledgeGraph,
    QueryUnderstanding QueryUnderstanding,
    List<string> SuggestedFollowUps,
    Dictionary<string, object>? CaseContext
);

public record RAGDocument(
    string Id,
    string Content,
    double Relevance,
    string? SourceId,
    string? SourceType,
    Dictionary<string, object>? Metadata,
    List<int>? ChunkIndices
);

public record KnowledgeGraph(
    List<GraphNode> Nodes,
    List<GraphEdge> Edges,
    Dictionary<string, object>? Properties
);

public record GraphNode(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties
);

public record GraphEdge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);

public record QueryUnderstanding(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record RAGDocument(
    string Id,
    string Content,
    double Relevance,
    string? SourceId,
    string? SourceType,
    Dictionary<string, object>? Metadata,
    List<int>? ChunkIndices
);

public record KnowledgeGraph(
    List<GraphNode> Nodes,
    List<GraphEdge> Edges,
    Dictionary<string, object>? Properties
);

public record GraphNode(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties
);

public record GraphEdge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);

public record QueryUnderstanding(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record KnowledgeGraph(
    List<GraphNode> Nodes,
    List<GraphEdge> Edges,
    Dictionary<string, object>? Properties
);

public record GraphNode(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties
);

public record GraphEdge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);

public record QueryUnderstanding(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record GraphNode(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties
);

public record GraphEdge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);

public record QueryUnderstanding(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record GraphEdge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);

public record QueryUnderstanding(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record QueryUnderstanding(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record RAGGenerateResponse(
    string Answer,
    List<Citation> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);


public record GenerateReportRequest(
    string CaseId,
    string ReportType,
    string? Title = null,
    List<string>? SessionIds = null,
    List<string>? EvidenceIds = null,
    TimeRange? DateRange = null,
    Dictionary<string, object>? Options = null
);

public record ReportSectionRequest(
    string Title,
    string Content,
    int Order,
    List<string>? EvidenceReferences = null,
    List<Visualization>? Visualizations = null
);

// Response 
public record ReportResponse(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    string Content,
    List<ReportSection> Sections,
    List<string> EvidenceIds,
    List<Finding> Findings,
    List<Recommendation> Recommendations,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt,
    string? Submitte,
    Dictionary<string, object>? Metadata
);

public record ReportSection(
    string Id,
    string Title,
    string Content,
    int Order,
    List<string> EvidenceReferences,
    List<Visualization>? Visualizations
);

public record Finding(
    string Id,
    string Title,
    string Description,
    string Severity,
    double Confidence,
    List<string> SupportingEvidenceIds,
    List<string>? RelatedEntityIds,
    DateTimeOffset DiscoveredAt
);

public record Recommendation(
    string Id,
    string Title,
    string Description,
    string Priority,
    string Rationale,
    List<string> RelatedFindingIds
);

public record ReportListResponse(
    List<ReportSummary> Reports,
    int TotalCount,
    int Page,
    int PageSize
);

public record ReportSummary(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt
);


public record ReportSectionRequest(
    string Title,
    string Content,
    int Order,
    List<string>? EvidenceReferences = null,
    List<Visualization>? Visualizations = null
);

// Response 
public record ReportResponse(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    string Content,
    List<ReportSection> Sections,
    List<string> EvidenceIds,
    List<Finding> Findings,
    List<Recommendation> Recommendations,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt,
    string? Submitte,
    Dictionary<string, object>? Metadata
);

public record ReportSection(
    string Id,
    string Title,
    string Content,
    int Order,
    List<string> EvidenceReferences,
    List<Visualization>? Visualizations
);

public record Finding(
    string Id,
    string Title,
    string Description,
    string Severity,
    double Confidence,
    List<string> SupportingEvidenceIds,
    List<string>? RelatedEntityIds,
    DateTimeOffset DiscoveredAt
);

public record Recommendation(
    string Id,
    string Title,
    string Description,
    string Priority,
    string Rationale,
    List<string> RelatedFindingIds
);

public record ReportListResponse(
    List<ReportSummary> Reports,
    int TotalCount,
    int Page,
    int PageSize
);

public record ReportSummary(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt
);


public record ReportResponse(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    string Content,
    List<ReportSection> Sections,
    List<string> EvidenceIds,
    List<Finding> Findings,
    List<Recommendation> Recommendations,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt,
    string? Submitte,
    Dictionary<string, object>? Metadata
);

public record ReportSection(
    string Id,
    string Title,
    string Content,
    int Order,
    List<string> EvidenceReferences,
    List<Visualization>? Visualizations
);

public record Finding(
    string Id,
    string Title,
    string Description,
    string Severity,
    double Confidence,
    List<string> SupportingEvidenceIds,
    List<string>? RelatedEntityIds,
    DateTimeOffset DiscoveredAt
);

public record Recommendation(
    string Id,
    string Title,
    string Description,
    string Priority,
    string Rationale,
    List<string> RelatedFindingIds
);

public record ReportListResponse(
    List<ReportSummary> Reports,
    int TotalCount,
    int Page,
    int PageSize
);

public record ReportSummary(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt
);


public record ReportSection(
    string Id,
    string Title,
    string Content,
    int Order,
    List<string> EvidenceReferences,
    List<Visualization>? Visualizations
);

public record Finding(
    string Id,
    string Title,
    string Description,
    string Severity,
    double Confidence,
    List<string> SupportingEvidenceIds,
    List<string>? RelatedEntityIds,
    DateTimeOffset DiscoveredAt
);

public record Recommendation(
    string Id,
    string Title,
    string Description,
    string Priority,
    string Rationale,
    List<string> RelatedFindingIds
);

public record ReportListResponse(
    List<ReportSummary> Reports,
    int TotalCount,
    int Page,
    int PageSize
);

public record ReportSummary(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt
);


public record Finding(
    string Id,
    string Title,
    string Description,
    string Severity,
    double Confidence,
    List<string> SupportingEvidenceIds,
    List<string>? RelatedEntityIds,
    DateTimeOffset DiscoveredAt
);

public record Recommendation(
    string Id,
    string Title,
    string Description,
    string Priority,
    string Rationale,
    List<string> RelatedFindingIds
);

public record ReportListResponse(
    List<ReportSummary> Reports,
    int TotalCount,
    int Page,
    int PageSize
);

public record ReportSummary(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt
);


public record Recommendation(
    string Id,
    string Title,
    string Description,
    string Priority,
    string Rationale,
    List<string> RelatedFindingIds
);

public record ReportListResponse(
    List<ReportSummary> Reports,
    int TotalCount,
    int Page,
    int PageSize
);

public record ReportSummary(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt
);


public record ReportListResponse(
    List<ReportSummary> Reports,
    int TotalCount,
    int Page,
    int PageSize
);

public record ReportSummary(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt
);


public record ReportSummary(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt
);


    public class Settings
    {
        public string MinIOEndpoint { get; set; } = "localhost:9000";
        public string BucketName { get; set; } = "evidence";
        public bool VerifyHashOnUpload { get; set; } = true;
        public bool RequireAuth { get; set; } = false;
        public bool EncryptAtRest { get; set; } = false;
    }


public record ToolExecutionRequest(
    string ToolName,
    Dictionary<string, object> Parameters,
    string? CaseId = null,
    string? SessionId = null
);

public record OSINTRequest(
    string Target,
    List<string>? Sources = null,
    TimeRange? DateRange = null,
    int Depth = 2
);

public record TimelineRequest(
    string CaseId,
    TimeRange? DateRange = null,
    string Resolution = "hourly",
    bool IncludeInferences = true,
    bool HighlightAnomalies = true
);

public record NetworkAnalysisRequest(
    string CaseId,
    List<string>? EntityIds = null,
    int MaxDepth = 3,
    double MinConnectionStrength = 0.5
);

// Response 
public record ToolExecutionResponse(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
);

public record OSINTResult(
    Dictionary<string, List<OSINTFinding>> Findings,
    NetworkGraph? NetworkGraph = null,
    string IntelligenceSummary = "",
    List<string> Recommendations = null!
);

public record OSINTFinding(
    string Source,
    string Type,
    string Content,
    DateTimeOffset? Timestamp,
    double Relevance,
    Dictionary<string, object>? Metadata
);

public record TimelineResponse(
    List<TimelineEvent> Events,
    List<TimelinePattern> Patterns,
    List<TimelineAnomaly> Anomalies,
    List<CriticalPeriod> CriticalPeriods,
    object? VisualizationData
);

public record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocation? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record OSINTRequest(
    string Target,
    List<string>? Sources = null,
    TimeRange? DateRange = null,
    int Depth = 2
);

public record TimelineRequest(
    string CaseId,
    TimeRange? DateRange = null,
    string Resolution = "hourly",
    bool IncludeInferences = true,
    bool HighlightAnomalies = true
);

public record NetworkAnalysisRequest(
    string CaseId,
    List<string>? EntityIds = null,
    int MaxDepth = 3,
    double MinConnectionStrength = 0.5
);

// Response 
public record ToolExecutionResponse(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
);

public record OSINTResult(
    Dictionary<string, List<OSINTFinding>> Findings,
    NetworkGraph? NetworkGraph = null,
    string IntelligenceSummary = "",
    List<string> Recommendations = null!
);

public record OSINTFinding(
    string Source,
    string Type,
    string Content,
    DateTimeOffset? Timestamp,
    double Relevance,
    Dictionary<string, object>? Metadata
);

public record TimelineResponse(
    List<TimelineEvent> Events,
    List<TimelinePattern> Patterns,
    List<TimelineAnomaly> Anomalies,
    List<CriticalPeriod> CriticalPeriods,
    object? VisualizationData
);

public record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocation? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record TimelineRequest(
    string CaseId,
    TimeRange? DateRange = null,
    string Resolution = "hourly",
    bool IncludeInferences = true,
    bool HighlightAnomalies = true
);

public record NetworkAnalysisRequest(
    string CaseId,
    List<string>? EntityIds = null,
    int MaxDepth = 3,
    double MinConnectionStrength = 0.5
);

// Response 
public record ToolExecutionResponse(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
);

public record OSINTResult(
    Dictionary<string, List<OSINTFinding>> Findings,
    NetworkGraph? NetworkGraph = null,
    string IntelligenceSummary = "",
    List<string> Recommendations = null!
);

public record OSINTFinding(
    string Source,
    string Type,
    string Content,
    DateTimeOffset? Timestamp,
    double Relevance,
    Dictionary<string, object>? Metadata
);

public record TimelineResponse(
    List<TimelineEvent> Events,
    List<TimelinePattern> Patterns,
    List<TimelineAnomaly> Anomalies,
    List<CriticalPeriod> CriticalPeriods,
    object? VisualizationData
);

public record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocation? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record NetworkAnalysisRequest(
    string CaseId,
    List<string>? EntityIds = null,
    int MaxDepth = 3,
    double MinConnectionStrength = 0.5
);

// Response 
public record ToolExecutionResponse(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
);

public record OSINTResult(
    Dictionary<string, List<OSINTFinding>> Findings,
    NetworkGraph? NetworkGraph = null,
    string IntelligenceSummary = "",
    List<string> Recommendations = null!
);

public record OSINTFinding(
    string Source,
    string Type,
    string Content,
    DateTimeOffset? Timestamp,
    double Relevance,
    Dictionary<string, object>? Metadata
);

public record TimelineResponse(
    List<TimelineEvent> Events,
    List<TimelinePattern> Patterns,
    List<TimelineAnomaly> Anomalies,
    List<CriticalPeriod> CriticalPeriods,
    object? VisualizationData
);

public record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocation? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record ToolExecutionResponse(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<Visualization>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
);

public record OSINTResult(
    Dictionary<string, List<OSINTFinding>> Findings,
    NetworkGraph? NetworkGraph = null,
    string IntelligenceSummary = "",
    List<string> Recommendations = null!
);

public record OSINTFinding(
    string Source,
    string Type,
    string Content,
    DateTimeOffset? Timestamp,
    double Relevance,
    Dictionary<string, object>? Metadata
);

public record TimelineResponse(
    List<TimelineEvent> Events,
    List<TimelinePattern> Patterns,
    List<TimelineAnomaly> Anomalies,
    List<CriticalPeriod> CriticalPeriods,
    object? VisualizationData
);

public record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocation? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record OSINTResult(
    Dictionary<string, List<OSINTFinding>> Findings,
    NetworkGraph? NetworkGraph = null,
    string IntelligenceSummary = "",
    List<string> Recommendations = null!
);

public record OSINTFinding(
    string Source,
    string Type,
    string Content,
    DateTimeOffset? Timestamp,
    double Relevance,
    Dictionary<string, object>? Metadata
);

public record TimelineResponse(
    List<TimelineEvent> Events,
    List<TimelinePattern> Patterns,
    List<TimelineAnomaly> Anomalies,
    List<CriticalPeriod> CriticalPeriods,
    object? VisualizationData
);

public record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocation? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record OSINTFinding(
    string Source,
    string Type,
    string Content,
    DateTimeOffset? Timestamp,
    double Relevance,
    Dictionary<string, object>? Metadata
);

public record TimelineResponse(
    List<TimelineEvent> Events,
    List<TimelinePattern> Patterns,
    List<TimelineAnomaly> Anomalies,
    List<CriticalPeriod> CriticalPeriods,
    object? VisualizationData
);

public record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocation? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record TimelineResponse(
    List<TimelineEvent> Events,
    List<TimelinePattern> Patterns,
    List<TimelineAnomaly> Anomalies,
    List<CriticalPeriod> CriticalPeriods,
    object? VisualizationData
);

public record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocation? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocation? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record TimelinePattern(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record TimelineAnomaly(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record CriticalPeriod(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record NetworkGraph(
    List<Node> Nodes,
    List<Edge> Edges,
    Dictionary<string, object>? Metadata
);

public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record Node(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


public record Edge(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);


