using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IIM.Shared.Models.Core;

/// <summary>
/// Represents the physical, deduplicated content of a file. 
/// This entity is identified by its content hash and stores intrinsic, immutable properties.
/// </summary>
public class StoredFile
{
    /// <summary>
    /// The primary key, which is the SHA256 hash of the file's content.
    /// </summary>
    [Key]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// The size of the file in bytes. Stored for performance.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// The MIME type of the file. Stored for performance.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// The intrinsic, content-based classification tags for this file (e.g., PII, PHI).
    /// This is a many-to-many relationship.
    /// </summary>
    public ICollection<ClassificationTag> ClassificationTags { get; set; } = new List<ClassificationTag>();

    /// <summary>
    /// Navigation property for all the virtual files that reference this content.
    /// </summary>
    public ICollection<VirtualFile> VirtualFiles { get; set; } = new List<VirtualFile>();
}
