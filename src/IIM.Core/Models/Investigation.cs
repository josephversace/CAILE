using System;
using System.Collections.Generic;
using System.IO;
using IIM.Shared.Enums;
using IIM.Shared.Models;

namespace IIM.Core.Models
{
    public class InvestigationSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = "New Investigation";
        public string Icon { get; set; } = "🕵️‍♂️";
        public InvestigationType Type { get; set; } = InvestigationType.GeneralInquiry;
        public List<InvestigationMessage> Messages { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, ModelConfiguration> Models { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = Environment.UserName;
        public InvestigationStatus Status { get; set; } = InvestigationStatus.Active;
        public List<Finding> Findings { get; set; } = new();
    }

    public class InvestigationMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<Attachment> Attachments { get; set; } = new();
        public List<ToolResult> ToolResults { get; set; } = new();
        public List<Citation> Citations { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? ModelUsed { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class InvestigationQuery
    {
        public string Text { get; set; } = string.Empty;
        public List<Attachment> Attachments { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }

    public class InvestigationResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<ToolResult> ToolResults { get; set; } = new();
        public List<Citation> Citations { get; set; } = new();
        public List<Evidence> RelatedEvidence { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        // Add display-specific properties to existing model
        public ResponseDisplayType DisplayType { get; set; } = ResponseDisplayType.Auto;
        public double? Confidence { get; set; }
        public ResponseVisualization? Visualization { get; set; }
        public Dictionary<string, object>? DisplayMetadata { get; set; }

        // Add these missing properties that ExportService needs:
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = Environment.UserName;
        public string Hash { get; set; } = string.Empty;

        // Add a helper property to map Message to Content for compatibility
        public string Content => Message;
    }

    public class ResponseVisualization
    {
        public VisualizationType Type { get; set; } = VisualizationType.Auto;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public object Data { get; set; } = new { };
        public Dictionary<string, object>? Options { get; set; }


        public string? ChartType { get; set; } // For Chart visualizations (bar, line, pie, etc.)
        public List<string>? Columns { get; set; } // For Table visualizations
        public string? GraphType { get; set; } // For Graph visualizations (network, tree, etc.)
        public string? MapType { get; set; } // For Map visualizations (heat, markers, etc.)
        public string? CustomTemplate { get; set; } // For Custom visualizations
    }

    public class CreateSessionRequest
    {
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = "New Investigation";
        public string InvestigationType { get; set; } = "GeneralInquiry";

        public CreateSessionRequest() { }

        public CreateSessionRequest(string caseId, string title, string investigationType)
        {
            CaseId = caseId;
            Title = title;
            InvestigationType = investigationType;
        }
    }





}
