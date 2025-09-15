using IIM.Shared.Enums;
using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core;

/// <summary>
/// Represents a virtual reference or "link" to a StoredFile within a specific workspace.
/// This entity contains all the context-specific metadata for a file instance.
/// </summary>
public class VirtualFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// Foreign key linking this virtual file to its actual content in the StoredFile table.
    /// </summary>
    public string StoredFileHash { get; set; } = string.Empty;
    public StoredFile StoredFile { get; set; } = null!;

    /// <summary>
    /// The user-visible name of the file in this specific context (e.g., "report.docx").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The virtual path of the file within its workspace (e.g., "/reports/financials/").
    /// The root is always "/".
    /// </summary>
    public string Path { get; set; } = "/";

    public FileUploadStatus Status { get; set; }

    // Timestamps
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Forensic and context-specific metadata
    public string CreatedBy { get; set; } = string.Empty;
    public string CollectedBy { get; set; } = string.Empty;
    public string CollectedLocation { get; set; } = string.Empty;
    public DateTimeOffset CollectionDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// A flexible dictionary to store any additional, source-specific metadata 
    /// (e.g., "EmailSender", "EmailSubject") that doesn't fit in the core model.
    /// </summary>
    public Dictionary<string, string> CustomMetadata { get; set; } = new();

    // Chain of custody for this specific virtual instance.
    public List<ChainOfCustodyEntry> ChainOfCustody { get; set; } = new();
}
