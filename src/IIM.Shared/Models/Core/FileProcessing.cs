using System;

namespace IIM.Shared.Models.Core
{
    /// <summary>
    /// Represents a version of a file that has been created by a processing tool or plugin.
    /// </summary>
    public class ProcessedFile
    {
        public Guid Id { get; set; }
        public Guid OriginalVirtualFileId { get; set; }
        public string ProcessingType { get; set; } = string.Empty; // e.g., "TRANSCRIPTION", "TRANSLATION"
        public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
        public string ProcessedBy { get; set; } = string.Empty;
        public string StoredFileHash { get; set; } = string.Empty; // The hash of the *new* processed content
    }
}
