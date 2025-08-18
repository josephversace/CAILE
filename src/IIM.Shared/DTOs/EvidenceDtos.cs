
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs;
// Request DTOs
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

// Response DTOs
public record EvidenceResponseDto(
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
    EvidenceMetadataDto Metadata,
    List<ChainOfCustodyEntryDto>? ChainOfCustody = null,
    List<ProcessedEvidenceDto>? ProcessedVersions = null
);

public record EvidenceMetadataDto(
    string CaseNumber,
    string CollectedBy,
    DateTimeOffset CollectionDate,
    string? CollectionLocation,
    string? DeviceSource,
    string? Description,
    Dictionary<string, string>? CustomFields
);

public record ChainOfCustodyReportDto(
    string EvidenceId,
    string OriginalFileName,
    string CaseNumber,
    List<ChainOfCustodyEntryDto> ChainEntries,
    List<ProcessedEvidenceDto> ProcessedVersions,
    bool IntegrityValid,
    string Signature,
    DateTimeOffset GeneratedAt,
    string GeneratedBy
);

public record ChainOfCustodyEntryDto(
    string Id,
    DateTimeOffset Timestamp,
    string Action,
    string Actor,
    string Details,
    string Hash,
    string PreviousHash
);

public record ProcessedEvidenceDto(
    string Id,
    string OriginalEvidenceId,
    string ProcessingType,
    DateTimeOffset ProcessedTimestamp,
    string ProcessedBy,
    string ProcessedHash,
    string StoragePath,
    Dictionary<string, object>? ProcessingResults
);

public record EvidenceExportDto(
    string ExportId,
    string EvidenceId,
    string ExportPath,
    List<string> Files,
    bool IntegrityValid,
    DateTimeOffset ExportedAt,
    string ExportedBy
);

public record EvidenceListResponse(
    List<EvidenceSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EvidenceSummaryDto(
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
