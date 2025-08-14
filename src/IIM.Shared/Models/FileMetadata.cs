using System;

namespace IIM.Shared.Models
{
    public class FileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Hash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string MimeType { get; set; } = string.Empty;
    }
}
