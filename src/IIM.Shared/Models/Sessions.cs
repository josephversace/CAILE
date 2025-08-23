using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
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

    /// <summary>
    /// Message in an investigation session with extended properties.
    /// </summary>
    public class InvestigationMessage
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<Attachment>? Attachments { get; set; }
        public List<ToolResult>? ToolResults { get; set; }
        public List<Citation>? Citations { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? ModelUsed { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        
        // Properties from DTO version
        public RAGSearchResult? RAGResults { get; set; }
        public List<TranscriptionResult>? Transcriptions { get; set; }
        public List<ImageAnalysisResult>? ImageAnalyses { get; set; }
        public List<string>? EvidenceIds { get; set; }
        public List<string>? EntityIds { get; set; }
        public string? FineTuneJobId { get; set; }
        
        // New optional properties
        public string? SessionId { get; set; }  // Session this message belongs to
        public string? ParentMessageId { get; set; }  // For threaded conversations
        public List<string>? ChildMessageIds { get; set; }  // Reply chain
        public bool? IsEdited { get; set; }  // Message was edited
        public DateTimeOffset? EditedAt { get; set; }  // When edited
        public string? EditedBy { get; set; }  // Who edited
        public MessageStatus? Status { get; set; }  // Processing status
        public double? Confidence { get; set; }  // Confidence score
    }

    public class InvestigationQuery
    {
        public string Text { get; set; } = string.Empty;
        public List<Attachment> Attachments { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        
        // Properties from DTO version
        public string? SessionId { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public List<string>? RequestedTools { get; set; }
    } = string.Empty;
        public List<Attachment> Attachments { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Response from an investigation query with extended properties.
    /// </summary>
    public class InvestigationResponse
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        public List<ToolResult>? ToolResults { get; set; }
        public List<Citation>? Citations { get; set; }
        public List<Evidence>? RelatedEvidence { get; set; }
        public double? Confidence { get; set; }
        public ResponseDisplayType DisplayType { get; set; } = ResponseDisplayType.Auto;
        public Dictionary<string, object>? Metadata { get; set; }
        public Dictionary<string, object>? DisplayMetadata { get; set; }
        public List<Visualization>? Visualizations { get; set; }

        
        // Properties from DTO version
        public RAGSearchResult? RAGResults { get; set; }
        public List<TranscriptionResult>? Transcriptions { get; set; }
        public List<ImageAnalysisResult>? ImageAnalyses { get; set; }
        public List<string>? EvidenceIds { get; set; }
        public List<string>? EntityIds { get; set; }
        public string? FineTuneJobId { get; set; }
        
        // New optional properties
        public string? SessionId { get; set; }  // Session this response belongs to
        public DateTimeOffset? Timestamp { get; set; }  // When response was generated
        public string? QueryId { get; set; }  // Original query ID
        public string? ModelUsed { get; set; }  // Which model generated this
        public TimeSpan? ProcessingTime { get; set; }  // How long it took
        public string? CreatedBy { get; set; }  // User or system
        public DateTimeOffset? CreatedAt { get; set; }  // Creation timestamp
        public string? Hash { get; set; }  // For integrity verification
        public ResponseVisualization? Visualization { get; set; }  // Primary visualization
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

    /// <summary>
    /// Request to create a new investigation session with extended properties.
    /// </summary>
    public class CreateSessionRequest
    { 
        // Existing constructor and properties
        public CreateSessionRequest(string caseId, string title, string investigationType)
        {
            CaseId = caseId;
            Title = title;
            InvestigationType = investigationType;
        }

        public string CaseId { get; }
        public string Title { get; }
        public string InvestigationType { get; }

        
        // Properties from DTO version
        public RAGSearchResult? RAGResults { get; set; }
        public List<TranscriptionResult>? Transcriptions { get; set; }
        public List<ImageAnalysisResult>? ImageAnalyses { get; set; }
        public List<string>? EvidenceIds { get; set; }
        public List<string>? EntityIds { get; set; }
        public string? FineTuneJobId { get; set; }
        
        // New optional properties
        public string? Description { get; set; }
        public string? UserId { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public List<string>? EnabledTools { get; set; }
        public Dictionary<string, object>? InitialContext { get; set; }
        
        // Properties from DTO version
        public Dictionary<string, ModelConfiguration>? Models { get; set; }
        public SessionContext? Context { get; set; }
    }
}



