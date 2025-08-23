using IIM.Shared.Enums;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Represents digital evidence with chain of custody tracking.
    /// Extended with optional properties for enhanced functionality.
    /// </summary>
    public class Evidence
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseId { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public EvidenceType Type { get; set; }
        public EvidenceStatus Status { get; set; }
        public string Hash { get; set; } = string.Empty;
        public string HashAlgorithm { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public DateTimeOffset IngestTimestamp { get; set; } = DateTimeOffset.UtcNow;
        public EvidenceMetadata Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }

        // New optional properties for compatibility
        public string? FileType { get; set; }  // MIME type
        public string? FileName { get; set; }  // Alias for OriginalFileName
        public DateTimeOffset? UploadedAt { get; set; }  // Alias for IngestTimestamp
        public DateTimeOffset? UpdatedAt { get; set; }  // Last update timestamp
        public string? UploadedBy { get; set; }  // User who uploaded
        public bool? IntegrityValid { get; set; }  // Hash verification status
        public string? Signature { get; set; }  // Digital signature
        public List<ChainOfCustodyEntry>? ChainOfCustody { get; set; }
        public List<ProcessedEvidence>? ProcessedVersions { get; set; }
        public Dictionary<string, string>? Hashes { get; set; }  // Multiple hash types

        // Computed properties for convenience
        public string GetFileType() => FileType ?? DetermineFileType(OriginalFileName);
        public DateTimeOffset GetUploadedAt() => UploadedAt ?? IngestTimestamp;
        public DateTimeOffset GetUpdatedAt() => UpdatedAt ?? IngestTimestamp;

        private string DetermineFileType(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" or ".docx" => "application/msword",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream"
            };
        }
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

        public string? SessionId { get; set; }
    }

    public class EvidenceContext
    {
        public string CaseId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
    }


    /// <summary>
    /// Processed version of evidence (OCR, transcription, etc.).
    /// </summary>
    public class ProcessedEvidence
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OriginalEvidenceId { get; set; } = string.Empty;
        public string ProcessingType { get; set; } = string.Empty;
        public DateTimeOffset ProcessedTimestamp { get; set; } = DateTimeOffset.UtcNow;
        public string ProcessedBy { get; set; } = string.Empty;
        public string ProcessedHash { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public Dictionary<string, object>? ProcessingResults { get; set; }
        public TimeSpan? ProcessingDuration { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
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
        public List<AuditEvent> AuditLog { get; set; } = new();
        public string PublicKey { get; set; } = string.Empty;
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


}
