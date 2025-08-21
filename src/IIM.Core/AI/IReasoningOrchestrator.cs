using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using IIM.Core.Models;
using IIM.Shared.Models;
using IIM.Shared.Enums;
using IIM.Shared.DTOs;

namespace IIM.Core.AI
{
    /// <summary>
    /// High-level reasoning service interface for intelligent orchestration.
    /// This is the intelligence layer responsible for WHAT to do - parsing intent,
    /// chaining operations, and managing complex multi-step reasoning.
    /// </summary>
    public interface IReasoningService
    {
        #region Core Reasoning Operations

        /// <summary>
        /// Processes a natural language query and determines the appropriate action plan.
        /// </summary>
        /// <param name="query">User's natural language query</param>
        /// <param name="session">Optional current investigation session for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Reasoning result with action plan and extracted intent</returns>
        Task<ReasoningResult> ProcessQueryAsync(
            string query,
            InvestigationSession? session = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a multi-step reasoning chain for complex operations.
        /// </summary>
        /// <param name="chain">Chain of reasoning steps to execute</param>
        /// <param name="progress">Progress reporter for long-running chains</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Results from the reasoning chain execution</returns>
        Task<ChainExecutionResult> ExecuteReasoningChainAsync(
            ReasoningChain chain,
            IProgress<ReasoningProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes evidence and generates investigative insights.
        /// </summary>
        /// <param name="evidenceIds">IDs of evidence to analyze</param>
        /// <param name="analysisType">Type of analysis to perform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Analysis results with insights and recommendations</returns>
        Task<AnalysisResult> AnalyzeEvidenceAsync(
            List<string> evidenceIds,
            AnalysisType analysisType,
            CancellationToken cancellationToken = default);

        #endregion

        #region Semantic Kernel Management

        /// <summary>
        /// Gets or creates a Semantic Kernel configured for a specific purpose.
        /// </summary>
        /// <param name="purpose">Purpose for the kernel (e.g., "investigation", "forensics")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Configured Semantic Kernel instance</returns>
        Task<Kernel> GetKernelAsync(
            string purpose,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads a plugin into the reasoning service.
        /// </summary>
        /// <param name="pluginName">Name of the plugin to load</param>
        /// <param name="configuration">Optional plugin configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if plugin loaded successfully</returns>
        Task<bool> LoadPluginAsync(
            string pluginName,
            Dictionary<string, object>? configuration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available plugins for the reasoning service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available plugin information</returns>
        Task<List<PluginInfo>> GetAvailablePluginsAsync(
            CancellationToken cancellationToken = default);

        #endregion

        #region Intent and Context Management

        /// <summary>
        /// Extracts intent from a user query.
        /// </summary>
        /// <param name="query">User's natural language query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Extracted intent with confidence scores</returns>
        Task<IntentExtractionResult> ExtractIntentAsync(
            string query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds context from current investigation session.
        /// </summary>
        /// <param name="sessionId">Current session ID</param>
        /// <param name="includeHistory">Whether to include conversation history</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Built session with enriched context</returns>
        Task<InvestigationSession> BuildSessionContextAsync(
            string sessionId,
            bool includeHistory = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the session context with new information.
        /// </summary>
        /// <param name="session">Session to update</param>
        /// <param name="updates">Updates to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated session</returns>
        Task<InvestigationSession> UpdateSessionContextAsync(
            InvestigationSession session,
            Dictionary<string, object> updates,
            CancellationToken cancellationToken = default);

        #endregion

        #region Prompt and Template Management

        /// <summary>
        /// Generates a prompt for a specific task.
        /// </summary>
        /// <param name="taskType">Type of task requiring a prompt</param>
        /// <param name="parameters">Parameters for prompt generation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated prompt ready for model consumption</returns>
        Task<string> GeneratePromptAsync(
            string taskType,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies a template to format output.
        /// </summary>
        /// <param name="templateName">Name of the template to apply</param>
        /// <param name="data">Data to format with the template</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Formatted output</returns>
        Task<string> ApplyTemplateAsync(
            string templateName,
            object data,
            CancellationToken cancellationToken = default);

        #endregion

        #region Model Routing and Selection

        /// <summary>
        /// Determines the best model for a given task.
        /// </summary>
        /// <param name="task">Task description</param>
        /// <param name="constraints">Optional constraints (e.g., speed, accuracy)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Recommended model configuration</returns>
        Task<ModelRecommendation> RecommendModelAsync(
            string task,
            ModelConstraints? constraints = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Routes a request to the appropriate model or service.
        /// </summary>
        /// <param name="request">Request to route</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Routing decision with selected handler</returns>
        Task<RoutingDecision> RouteRequestAsync(
            ReasoningRequest request,
            CancellationToken cancellationToken = default);

        #endregion

        #region Events

        /// <summary>
        /// Raised when reasoning starts for a query.
        /// </summary>
        event EventHandler<ReasoningStartedEventArgs>? ReasoningStarted;

        /// <summary>
        /// Raised when reasoning completes.
        /// </summary>
        event EventHandler<ReasoningCompletedEventArgs>? ReasoningCompleted;

        /// <summary>
        /// Raised when a reasoning step completes in a chain.
        /// </summary>
        event EventHandler<ReasoningStepCompletedEventArgs>? StepCompleted;

        /// <summary>
        /// Raised when an error occurs during reasoning.
        /// </summary>
        event EventHandler<ReasoningErrorEventArgs>? ReasoningError;

        #endregion
    }

    #region Supporting Models (Only what's not in IIM.Shared)

    /// <summary>
    /// Result from reasoning operations.
    /// </summary>
    public class ReasoningResult
    {
        public string QueryId { get; set; } = Guid.NewGuid().ToString();
        public string OriginalQuery { get; set; } = string.Empty;
        public IntentExtractionResult Intent { get; set; } = new();
        public List<ReasoningStep> ActionPlan { get; set; } = new();
        public Dictionary<string, object> ExtractedEntities { get; set; } = new();
        public float Confidence { get; set; }
        public TimeSpan ProcessingTime { get; set; }
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
    /// Plugin information.
    /// </summary>
    public class PluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<string> Functions { get; set; } = new();
        public bool IsLoaded { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Model recommendation result.
    /// </summary>
    public class ModelRecommendation
    {
        public string ModelId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public float ConfidenceScore { get; set; }
        public List<string> AlternativeModels { get; set; } = new();
        public Dictionary<string, object> RecommendedParameters { get; set; } = new();
    }

    /// <summary>
    /// Constraints for model selection.
    /// </summary>
    public class ModelConstraints
    {
        public int? MaxLatencyMs { get; set; }
        public float? MinAccuracy { get; set; }
        public long? MaxMemoryBytes { get; set; }
        public bool PreferLocal { get; set; } = true;
        public List<string> RequiredCapabilities { get; set; } = new();
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
    /// Result from analysis operations using existing models.
    /// </summary>
    public class AnalysisResult
    {
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
        public AnalysisType Type { get; set; }
        public List<Finding> Findings { get; set; } = new();  // Using existing Finding from IIM.Shared.Models
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public TimeSpan AnalysisTime { get; set; }
        public float ConfidenceScore { get; set; }
    }

    #endregion

    #region Event Arguments (Minimal new definitions)

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

    #endregion
}