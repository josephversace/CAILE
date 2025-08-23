using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Response DTO containing evidence details
    /// </summary>
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

    /// <summary>
    /// Evidence metadata information
    /// </summary>
    public record EvidenceMetadata(
        string CaseNumber,
        string CollectedBy,
        DateTimeOffset CollectionDate,
        string? CollectionLocation,
        string? DeviceSource,
        string? Description,
        Dictionary<string, string>? CustomFields
    );

    /// <summary>
    /// Chain of custody entry for evidence
    /// </summary>
    public record ChainOfCustodyEntry(
        string Id,
        DateTimeOffset Timestamp,
        string Action,
        string Actor,
        string Details,
        string Hash,
        string PreviousHash,
        Dictionary<string, object>? Metadata
    );

    /// <summary>
    /// Processed evidence version information
    /// </summary>
    public record ProcessedEvidence(
        string Id,
        string OriginalEvidenceId,
        string ProcessingType,
        DateTimeOffset ProcessedTimestamp,
        string ProcessedBy,
        string ProcessedHash,
        string StoragePath,
        Dictionary<string, object>? ProcessingResults,
        TimeSpan? ProcessingDuration,
        bool Success,
        string? ErrorMessage
    );

    /// <summary>
    /// Response DTO for evidence upload initiation
    /// </summary>
    public record InitiateEvidenceUploadResponse(
        string EvidenceId,
        string UploadUrl,
        DateTimeOffset UrlExpiration,
        bool IsDuplicate,
        string? DuplicateEvidenceId,
        Dictionary<string, string>? Headers
    );

    /// <summary>
    /// Response DTO for evidence upload confirmation
    /// </summary>
    public record ConfirmEvidenceUploadResponse(
        string EvidenceId,
        bool Success,
        bool IntegrityValid,
        string? ErrorMessage,
        EvidenceResponse? Evidence
    );

    /// <summary>
    /// Response DTO for evidence list
    /// </summary>
    public record EvidenceListResponse(
        List<EvidenceResponse> Evidence,
        int TotalCount,
        int Page,
        int PageSize
    );
}