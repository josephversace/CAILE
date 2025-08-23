using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Request DTO for evidence ingestion
    /// </summary>
    public record EvidenceIngestRequest(
        string CaseNumber,
        string CollectedBy,
        string? CollectionLocation = null,
        string? DeviceSource = null,
        string? Description = null,
        DateTimeOffset? CollectionDate = null,
        Dictionary<string, string>? CustomFields = null
    );

    /// <summary>
    /// Request DTO for evidence processing
    /// </summary>
    public record EvidenceProcessRequest(
        string ProcessingType,
        Dictionary<string, object>? Parameters = null
    );

    /// <summary>
    /// Request DTO for evidence export
    /// </summary>
    public record EvidenceExportRequest(
        string? ExportPath = null,
        bool IncludeProcessedVersions = true,
        bool GenerateVerificationScript = true,
        string? Format = "default"
    );

    /// <summary>
    /// Request DTO for initiating evidence upload with deduplication check
    /// </summary>
    public record InitiateEvidenceUploadRequest(
        string CaseNumber,
        string FileName,
        long FileSize,
        string FileHash,
        string HashAlgorithm = "SHA256",
        string? ContentType = null,
        string CollectedBy = "",
        DateTimeOffset? CollectionDate = null,
        string? CollectionLocation = null,
        string? DeviceSource = null,
        string? Description = null,
        Dictionary<string, string>? CustomFields = null
    );

    /// <summary>
    /// Request DTO for confirming evidence upload completion
    /// </summary>
    public record ConfirmEvidenceUploadRequest(
        string EvidenceId,
        string StoragePath,
        bool VerifyIntegrity = true
    );

    /// <summary>
    /// Request DTO for evidence search
    /// </summary>
    public record SearchEvidenceRequest(
        string? CaseId = null,
        string? Type = null,
        string? Status = null,
        DateTimeOffset? CollectedAfter = null,
        DateTimeOffset? CollectedBefore = null,
        string? SearchTerm = null,
        int Page = 1,
        int PageSize = 20
    );
}