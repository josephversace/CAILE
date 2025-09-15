using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
    /// <summary>
    /// Represents a single, immutable entry in a VirtualFile's chain of custody.
    /// </summary>
    public class ChainOfCustodyEntry
    {
        public Guid Id { get; set; }
        public Guid VirtualFileId { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string Action { get; set; } = string.Empty; // e.g., "INGESTED", "PROCESSED", "EXPORTED"
        public string Actor { get; set; } = string.Empty; // User ID or system process
        public string Details { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty; // Hash of the file at the time of the event
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Represents a DTO for generating a chain of custody report.
    /// </summary>
    public class ChainOfCustodyReport
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public Guid WorkspaceId { get; set; }
        public List<ChainOfCustodyEntry> ChainEntries { get; set; } = new();
        public bool IsIntegrityValid { get; set; }
    }

    /// <summary>
    /// Represents a DTO for file export operations.
    /// </summary>
    public class FileExport
    {
        public Guid FileId { get; set; }
        public string ExportPath { get; set; } = string.Empty;
        public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
        public string ExportedBy { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new();
        public bool IntegrityValid { get; set; }
    }
}
