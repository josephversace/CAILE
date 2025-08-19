using System;

namespace IIM.Core.Models;

public class FileMetadata
{
    public string FilePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}
