using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using IIM.Core.Models;
using IIM.Shared.DTOs;
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

}