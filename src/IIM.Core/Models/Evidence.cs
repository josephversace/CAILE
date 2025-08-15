using System;
using System.Collections.Generic;

namespace IIM.Core.Models
{
    /// <summary>
    /// Main Evidence entity - represents a piece of digital evidence
    /// </summary>
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

    /// <summary>
    /// Evidence metadata - collection information
    /// </summary>
    public class EvidenceMetadata
    {
        public string CaseNumber { get; set; } = string.Empty;
        public string CollectedBy { get; set; } = string.Empty;
        public DateTimeOffset CollectionDate { get; set; } = DateTimeOffset.UtcNow;
        public string CollectionLocation { get; set; } = string.Empty;
        public string DeviceSource { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Chain of custody entry - tracks evidence handling
    /// </summary>
    public class ChainOfCustodyEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Officer { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public string PreviousHash { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Processed evidence - derived/processed versions
    /// </summary>
    public class ProcessedEvidence
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string OriginalEvidenceId { get; set; } = string.Empty;
        public string ProcessingType { get; set; } = string.Empty;
        public DateTimeOffset ProcessedTimestamp { get; set; } = DateTimeOffset.UtcNow;
        public string ProcessedBy { get; set; } = string.Empty;
        public string ProcessedHash { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public Dictionary<string, object> ProcessingResults { get; set; } = new();
    }

    /// <summary>
    /// Chain of Custody Report - for export/display
    /// </summary>
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

        // Additional fields for law enforcement compliance
        public DateTime IngestTimestamp { get; set; } = DateTime.UtcNow;
        public string MachineIdentifier { get; set; } = Environment.MachineName;
        public Dictionary<string, string> OriginalHashes { get; set; } = new();
        public List<AuditLogEntry> AuditLog { get; set; } = new();
        public string PublicKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Audit log entry
    /// </summary>
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Event { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Evidence export package
    /// </summary>
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

   

    // Enums
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
}
