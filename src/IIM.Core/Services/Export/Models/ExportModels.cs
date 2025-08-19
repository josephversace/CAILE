using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Core.Services.Export.Models;


    /// <summary>
    /// Configuration for exporting investigation data
    /// </summary>
    public class ExportConfiguration
    {
        public ExportFormat Format { get; set; }
        public ExportOptions Options { get; set; } = new();
        public string? TemplateName { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Template for export formatting
    /// </summary>
    public class ExportTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public string? HeaderTemplate { get; set; }
        public string? BodyTemplate { get; set; }
        public string? FooterTemplate { get; set; }
        public Dictionary<string, object> DefaultOptions { get; set; } = new();
        public bool IsSystemTemplate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of an export operation
    /// </summary>
    public class ExportOperation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EntityType { get; set; } = string.Empty; // Response, Report, Case, etc.
        public string EntityId { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public ExportStatus Status { get; set; } = ExportStatus.Pending;
        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

public enum ExportStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}
