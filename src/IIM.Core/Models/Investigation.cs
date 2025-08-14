using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Core.Models;


public class InvestigationSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CaseId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public InvestigationType Type { get; set; }
    public SessionStatus Status { get; set; }

    // Configuration
    public Dictionary<ModalityType, ModelConfiguration> Models { get; set; } = new();
    public List<string> EnabledTools { get; set; } = new();
    public SessionContext Context { get; set; } = new();

    // Conversation History
    public List<InvestigationMessage> Messages { get; set; } = new();
    public List<ToolExecution> ToolExecutions { get; set; } = new();

    // Timestamps
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }

    // Results
    public List<Finding> Findings { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class InvestigationQuery
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<Attachment> Attachments { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> RequestedTools { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string UserId { get; set; } = string.Empty;
}

public class InvestigationResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; set; } = string.Empty;
    public string QueryId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // Results from various sources
    public RAGSearchResult? RAGResults { get; set; }
    public List<TranscriptionResult> Transcriptions { get; set; } = new();
    public List<ImageAnalysisResult> ImageAnalyses { get; set; } = new();
    public List<ToolResult> ToolResults { get; set; } = new();

    // References
    public List<Citation> Citations { get; set; } = new();
    public List<string> EvidenceIds { get; set; } = new();
    public List<string> EntityIds { get; set; } = new();

    // Metadata
    public double Confidence { get; set; }
    public string? FineTuneJobId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SessionContext
{
    public string CaseId { get; set; } = string.Empty;
    public List<string> RelevantEvidenceIds { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
    public List<Entity> TrackedEntities { get; set; } = new();
    public TimeRange? FocusTimeRange { get; set; }
}

public class InvestigationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<Attachment> Attachments { get; set; } = new();
    public List<ToolResult> ToolResults { get; set; } = new();
    public List<Citation> Citations { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? ModelUsed { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public enum InvestigationType
{
    GeneralInquiry,
    EvidenceAnalysis,
    TimelineConstruction,
    NetworkAnalysis,
    PatternRecognition,
    ReportGeneration,
    OSINTResearch,
    ForensicAnalysis,
    InterviewAnalysis,
    Custom
}

public enum SessionStatus
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
