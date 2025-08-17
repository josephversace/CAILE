using System;
using System.Collections.Generic;

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

    namespace IIM.Core.Models
    {
        /// <summary>
        /// Represents a key finding or discovery from an investigation
        /// </summary>
        public class Finding
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("N");
            public string SessionId { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public FindingType Type { get; set; }
            public FindingSeverity Severity { get; set; }
            public double Confidence { get; set; }

            /// <summary>
            /// Evidence IDs that support this finding
            /// </summary>
            public List<string> SupportingEvidenceIds { get; set; } = new();

            /// <summary>
            /// Related entity IDs (persons, locations, etc.)
            /// </summary>
            public List<string> RelatedEntityIds { get; set; } = new();

            /// <summary>
            /// Tags for categorization
            /// </summary>
            public List<string> Tags { get; set; } = new();

            public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;
            public string DiscoveredBy { get; set; } = string.Empty; // User ID or "AI"

            /// <summary>
            /// Additional metadata specific to the finding
            /// </summary>
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        /// <summary>
        /// Type of investigation finding
        /// </summary>
        public enum FindingType
        {
            Evidence,           // Direct evidence found
            Connection,         // Link between entities/events
            Timeline,           // Temporal relationship discovered
            Pattern,            // Behavioral or data pattern
            Anomaly,            // Unusual or suspicious activity
            Identification,     // Person/object identified
            Location,           // Geographic discovery
            Communication,      // Message/call/contact found
            Financial,          // Money trail or transaction
            Technical,          // Digital forensic finding
            Witness,            // Witness statement or testimony
            Contradiction       // Conflicting information found
        }

        /// <summary>
        /// Severity/importance level of the finding
        /// </summary>
        public enum FindingSeverity
        {
            Low,        // Minor or supporting detail
            Medium,     // Relevant but not critical
            High,       // Important to the case
            Critical    // Case-breaking discovery
        }
    }



    // Enums
    public enum InvestigationType
    {
        GeneralInquiry,
        EvidenceAnalysis,
        OSINTResearch,
        ForensicAnalysis,
        ThreatAssessment,
        IncidentResponse,
        TimelineConstruction,
        NetworkAnalysis,
        PatternRecognition
       
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
