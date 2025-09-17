using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.IO;

namespace IIM.Shared.Models.Core
{
    public class VirtualFile
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public long FileSize { get; set; }
        public FileUploadStatus Status { get; set; }
        public string StoredFileHash { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CollectedBy { get; set; } = string.Empty;
        public DateTimeOffset CollectionDate { get; set; } = DateTimeOffset.UtcNow;
        public string CollectedLocation { get; set; } = string.Empty;
        public Dictionary<string, string> CustomMetadata { get; set; } = new();
        public List<ChainOfCustodyEntry> ChainOfCustody { get; set; } = new();
        public List<ProcessedFile> ProcessedVersions { get; set; } = new();

        public DataSensitivityLevel DataSensitivity { get; set; }
        public List<string>? Tags { get; set; }

        public string Description { get; set; } = string.Empty; 
    }

    // Supporting classes
    public record AIInsight(string Text, int Confidence);
}

