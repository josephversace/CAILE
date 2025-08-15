using System;
using System.Collections.Generic;

namespace IIM.Core.Models
{
    public class InvestigationSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = "New Investigation";
        public InvestigationType Type { get; set; } = InvestigationType.GeneralInquiry;
        public List<InvestigationMessage> Messages { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, ModelConfiguration> Models { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = Environment.UserName;
        public InvestigationStatus Status { get; set; } = InvestigationStatus.Active;
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
    }

    public class InvestigationResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<ToolResult> ToolResults { get; set; } = new();
        public List<Citation> Citations { get; set; } = new();
        public List<Evidence> RelatedEvidence { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class CreateSessionRequest
    {
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = "New Investigation";
        public string InvestigationType { get; set; } = "GeneralInquiry";

        public CreateSessionRequest() { }

        // Fix: Ensure constructor parameter names match property names exactly
        public CreateSessionRequest(string caseId, string title, string investigationType)
        {
            CaseId = caseId;  // Property name is CaseId, not caseId
            Title = title;
            InvestigationType = investigationType;
        }
    }

    public class Attachment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public AttachmentType Type { get; set; }
        public string? StoragePath { get; set; }
        public Stream? Stream { get; set; }
    }

   


    // Enums
    public enum InvestigationType
    {
        GeneralInquiry,
        EvidenceAnalysis,
        OSINTResearch,
        ForensicAnalysis,
        ThreatAssessment,
        IncidentResponse
    }

    public enum InvestigationStatus
    {
        Active,
        Paused,
        Completed,
        Archived
    }

    public enum MessageRole
    {
        User,
        Assistant,
        System,
        Tool
    }

    public enum AttachmentType
    {
        Image,
        Document,
        Audio,
        Video,
        Data,
        Archive,
        Other
    }
}
