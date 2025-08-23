using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{

    // <summary>
    /// File attachment with extended properties.
    /// </summary>
    public class Attachment
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public AttachmentType Type { get; set; }
        public string? StoragePath { get; set; }

        // New optional properties
        public string? Hash { get; set; }  // SHA-256 hash
        public string? ThumbnailPath { get; set; }  // For images/videos
        public DateTimeOffset? UploadedAt { get; set; }  // Upload timestamp
        public string? UploadedBy { get; set; }  // User who uploaded
        public ProcessingStatus? ProcessingStatus { get; set; }  // Analysis status
        public Dictionary<string, object>? ExtractedMetadata { get; set; }  // EXIF, etc.
        public string? PreviewUrl { get; set; }  // For quick preview
        public bool? IsProcessed { get; set; }  // Has been analyzed
        public System.IO.Stream? Stream { get; set; }  // File stream for upload
    }
}
