using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Result from reasoning service with extended properties.
    /// </summary>
    public class ReasoningResult
    {
        // Existing core properties
        public string QueryId { get; set; } = Guid.NewGuid().ToString();
        public string OriginalQuery { get; set; } = string.Empty;
        public IntentExtractionResult Intent { get; set; } = new();
        public Dictionary<string, object> ExtractedEntities { get; set; } = new();
        public double Confidence { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public List<ReasoningStep> ActionPlan { get; set; } = new();
        // New optional properties
        public string? RecommendedModel { get; set; }  // Best model for this query
        public List<string>? RequiredTools { get; set; }  // Tools needed
        public Dictionary<string, double>? ModelScores { get; set; }  // Model confidence scores
        public bool? RequiresRAG { get; set; }  // Needs document search
        public List<string>? RelevantEvidenceIds { get; set; }  // Related evidence
    }

  
    /// <summary>
    /// Represents a chain of reasoning steps.
    /// </summary>
    public class ReasoningChain
    {
        public string ChainId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public List<ReasoningStep> Steps { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public ChainExecutionMode Mode { get; set; } = ChainExecutionMode.Sequential;
    }

    /// <summary>
    /// Single step in a reasoning chain.
    /// </summary>
    public class ReasoningStep
    {
        public string StepId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public bool IsOptional { get; set; }
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// Result from chain execution.
    /// </summary>
    public class ChainExecutionResult
    {
        public string ChainId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<StepResult> StepResults { get; set; } = new();
        public Dictionary<string, object> FinalOutput { get; set; } = new();
        public TimeSpan TotalExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result from a single step execution.
    /// </summary>
    public class StepResult
    {
        public string StepId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public object? Output { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Progress information for reasoning operations.
    /// </summary>
    public class ReasoningProgress
    {
        public string OperationId { get; set; } = string.Empty;
        public string CurrentStep { get; set; } = string.Empty;
        public int StepsCompleted { get; set; }
        public int TotalSteps { get; set; }
        public float PercentComplete => TotalSteps > 0 ? (float)StepsCompleted / TotalSteps * 100 : 0;
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of analysis that can be performed (extends existing investigation types).
    /// </summary>
    public enum AnalysisType
    {
        Forensic,
        Temporal,
        Relational,
        Pattern,
        Anomaly,
        Sentiment,
        Entity,
        Comprehensive
    }

    /// <summary>
    /// Chain execution modes.
    /// </summary>
    public enum ChainExecutionMode
    {
        Sequential,
        Parallel,
        Conditional,
        Adaptive
    }

    /// <summary>
    /// Result from intent extraction.
    /// </summary>
    public class IntentExtractionResult
    {
        public string PrimaryIntent { get; set; } = string.Empty;
        public Dictionary<string, float> IntentScores { get; set; } = new();
        public List<string> Entities { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public float Confidence { get; set; }
    }

 
    /// <summary>
    /// Request for reasoning operations.
    /// </summary>
    public class ReasoningRequest
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public object Input { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public InvestigationSession? Session { get; set; }
    }

    /// <summary>
    /// Routing decision for a request.
    /// </summary>
    public class RoutingDecision
    {
        public string RequestId { get; set; } = string.Empty;
        public string SelectedHandler { get; set; } = string.Empty;
        public string HandlerType { get; set; } = string.Empty;
        public Dictionary<string, object> HandlerParameters { get; set; } = new();
        public float ConfidenceScore { get; set; }
    }


    /// <summary>
    /// Event args for reasoning started.
    /// </summary>
    public class ReasoningStartedEventArgs : EventArgs
    {
        public string QueryId { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }

    /// <summary>
    /// Event args for reasoning completed.
    /// </summary>
    public class ReasoningCompletedEventArgs : EventArgs
    {
        public string QueryId { get; set; } = string.Empty;
        public ReasoningResult Result { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Event args for step completed.
    /// </summary>
    public class ReasoningStepCompletedEventArgs : EventArgs
    {
        public string ChainId { get; set; } = string.Empty;
        public string StepId { get; set; } = string.Empty;
        public StepResult Result { get; set; } = new();
    }

    /// <summary>
    /// Event args for reasoning errors.
    /// </summary>
    public class ReasoningErrorEventArgs : EventArgs
    {
        public string OperationId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

}
