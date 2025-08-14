using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Core.Models;


public class Evidence
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CaseId { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public EvidenceType Type { get; set; }
    public EvidenceStatus Status { get; set; }

    // Integrity
    public Dictionary<string, string> Hashes { get; set; } = new();
    public string Signature { get; set; } = string.Empty;
    public bool IntegrityValid { get; set; } = true;

    // Metadata
    public EvidenceMetadata Metadata { get; set; } = new();
    public DateTimeOffset IngestTimestamp { get; set; } = DateTimeOffset.UtcNow;
    public string StoragePath { get; set; } = string.Empty;

    // Chain of Custody
    public List<ChainOfCustodyEntry> ChainOfCustody { get; set; } = new();
    public List<ProcessedEvidence> ProcessedVersions { get; set; } = new();

    // Analysis
    public List<AnalysisResult> Analyses { get; set; } = new();
    public Dictionary<string, object> ExtractedData { get; set; } = new();
}

public class EvidenceMetadata
{
    public string CaseNumber { get; set; } = string.Empty;
    public string CollectedBy { get; set; } = string.Empty;
    public DateTimeOffset CollectionDate { get; set; }
    public string CollectionLocation { get; set; } = string.Empty;
    public string DeviceSource { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> CustomFields { get; set; } = new();
}

public class ChainOfCustodyEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string PreviousHash { get; set; } = string.Empty;
}

public class ProcessedEvidence
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OriginalEvidenceId { get; set; } = string.Empty;
    public string ProcessingType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedTimestamp { get; set; }
    public string ProcessedBy { get; set; } = string.Empty;
    public string ProcessedHash { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public Dictionary<string, object> ProcessingResults { get; set; } = new();
}

public class ChainOfCustodyReport
{
    public string EvidenceId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public List<ChainOfCustodyEntry> ChainEntries { get; set; } = new();
    public List<ProcessedEvidence> ProcessedVersions { get; set; } = new();
    public bool IntegrityValid { get; set; }
    public string Signature { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;
}

public class EvidenceExport
{
    public string ExportId { get; set; } = Guid.NewGuid().ToString("N");
    public string EvidenceId { get; set; } = string.Empty;
    public string ExportPath { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
    public bool IntegrityValid { get; set; }
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ExportedBy { get; set; } = string.Empty;
}

public enum EvidenceType
{
    Document,
    Image,
    Video,
    Audio,
    Email,
    Database,
    DiskImage,
    MemoryDump,
    NetworkCapture,
    LogFile,
    Archive,
    Other
}

public enum EvidenceStatus
{
    Pending,
    Ingested,
    Processing,
    Analyzed,
    Verified,
    Compromised,
    Archived
}

