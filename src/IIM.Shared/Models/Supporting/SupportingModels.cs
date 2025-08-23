using System;
using System.Collections.Generic;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Model configuration for AI models
    /// </summary>
    public class ModelConfiguration
    {
        public string ModelId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
        public bool AutoLoad { get; set; } = true;
        public int Priority { get; set; } = 1;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ModelId) &&
                   !string.IsNullOrWhiteSpace(Provider);
        }
    }

    /// <summary>
    /// Attachment for messages and queries
    /// </summary>
    public class Attachment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public AttachmentType Type { get; set; }
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool IsImage()
        {
            return MimeType.StartsWith("image/");
        }

        public bool IsDocument()
        {
            return MimeType.Contains("pdf") ||
                   MimeType.Contains("document") ||
                   MimeType.Contains("text");
        }

        public bool IsAudio()
        {
            return MimeType.StartsWith("audio/");
        }

        public bool IsVideo()
        {
            return MimeType.StartsWith("video/");
        }
    }

    /// <summary>
    /// Tool execution result
    /// </summary>
    public class ToolResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ToolName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public double? Confidence { get; set; }
        public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;
        public TimeSpan ExecutionTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool IsCompleted()
        {
            return Status == "Completed" || Status == "Failed";
        }

        public void MarkAsCompleted(object? data = null)
        {
            Status = "Completed";
            Success = true;
            Data = data;
        }

        public void MarkAsFailed(string errorMessage)
        {
            Status = "Failed";
            Success = false;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Citation reference
    /// </summary>
    public class Citation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SourceId { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public string? Location { get; set; }
        public double Relevance { get; set; }
        public string? Source { get; set; }
        public string? Url { get; set; }
        public string? Author { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public DateTimeOffset AccessedAt { get; set; } = DateTimeOffset.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool IsHighRelevance()
        {
            return Relevance >= 0.8;
        }
    }

    /// <summary>
    /// Visualization configuration
    /// </summary>
    public class Visualization
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public VisualizationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public object Data { get; set; } = new { };
        public Dictionary<string, object> Options { get; set; } = new();
        public string RenderFormat { get; set; } = "default";

        public bool RequiresInteractivity()
        {
            return Type == VisualizationType.Graph ||
                   Type == VisualizationType.Timeline ||
                   Type == VisualizationType.Map;
        }
    }

    /// <summary>
    /// Chain of custody entry
    /// </summary>
    public class ChainOfCustodyEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public string PreviousHash { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool ValidateHash()
        {
            // Implementation would verify the hash
            return !string.IsNullOrEmpty(Hash);
        }
    }

    /// <summary>
    /// Finding from investigation
    /// </summary>
    public class Finding
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FindingSeverity Severity { get; set; }
        public double Confidence { get; set; }
        public List<string> SupportingEvidenceIds { get; set; } = new();
        public List<string> RelatedEntityIds { get; set; } = new();
        public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool IsCritical()
        {
            return Severity == FindingSeverity.Critical;
        }

        public bool IsHighConfidence()
        {
            return Confidence >= 0.8;
        }
    }

    /// <summary>
    /// Processed evidence version
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
        public Dictionary<string, object> ProcessingResults { get; set; } = new();
        public TimeSpan ProcessingDuration { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public bool IsValid()
        {
            return Success && !string.IsNullOrEmpty(ProcessedHash);
        }
    }
}