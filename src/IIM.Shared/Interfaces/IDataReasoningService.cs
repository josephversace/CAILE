// File: src/IIM.Shared/Interfaces/IDataReasoningService.cs
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// High-level data reasoning and enrichment service interface.
    /// This is the main entry point for all data analysis and reasoning operations.
    /// Coordinates between content analysis, classification, query processing, and governance.
    /// </summary>
    public interface IDataReasoningService
    {
        #region Events
        event EventHandler<AnalysisStartedEventArgs>? AnalysisStarted;
        event EventHandler<AnalysisCompletedEventArgs>? AnalysisCompleted;
        event EventHandler<AnalysisErrorEventArgs>? AnalysisError;
        #endregion

        #region Content Analysis Operations

        /// <summary>
        /// Performs comprehensive content analysis on a file
        /// </summary>
        /// <param name="content">File content stream</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="mimeType">MIME type of the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Detailed content analysis results</returns>
        Task<ContentAnalysis> AnalyzeFileContentAsync(Stream content, string fileName, string mimeType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts entities from file content
        /// </summary>
        /// <param name="content">File content stream</param>
        /// <param name="mimeType">MIME type of the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Extracted entities with confidence scores</returns>
        Task<EntityExtractionResult> ExtractEntitiesAsync(Stream content, string mimeType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates vector embeddings for semantic search
        /// </summary>
        /// <param name="text">Text content to generate embeddings for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Vector embeddings</returns>
        Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default);

        #endregion

        #region Classification Operations

        /// <summary>
        /// Suggests classification tags and policies for a file
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="content">File content stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Classification suggestions with confidence scores</returns>
        Task<ClassificationSuggestion> SuggestClassificationAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

        #endregion

        #region Query and Search Operations

        /// <summary>
        /// Processes natural language queries against data
        /// </summary>
        /// <param name="query">Natural language query</param>
        /// <param name="workspaceId">Optional workspace scope</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Query results with matching files and generated response</returns>
        Task<QueryResult> ProcessDataQueryAsync(string query, Guid? workspaceId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates data insights based on questions
        /// </summary>
        /// <param name="question">Question to generate insights for</param>
        /// <param name="workspaceId">Optional workspace scope</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated insights with supporting evidence</returns>
        Task<DataInsight> GenerateDataInsightAsync(string question, Guid? workspaceId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds files similar to a given file
        /// </summary>
        /// <param name="virtualFileId">ID of the source file</param>
        /// <param name="maxResults">Maximum number of similar files to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Similar files with similarity scores</returns>
        Task<SimilarityResult> FindSimilarFilesAsync(Guid virtualFileId, int maxResults = 10, CancellationToken cancellationToken = default);

        #endregion

        #region Governance and Compliance Operations

        /// <summary>
        /// Suggests governance rules based on workspace data patterns
        /// </summary>
        /// <param name="workspaceId">Workspace to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Policy suggestions with reasoning</returns>
        Task<PolicySuggestion> SuggestGovernanceRulesAsync(Guid workspaceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks file compliance against governance framework
        /// </summary>
        /// <param name="file">File to check</param>
        /// <param name="rules">Governance framework rules</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Compliance check results</returns>
        Task<ComplianceCheck> CheckComplianceAsync(VirtualFile file, GovernanceFramework rules, CancellationToken cancellationToken = default);

        #endregion

        #region Risk Assessment Operations

        /// <summary>
        /// Assesses data risks across a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace to assess</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Risk assessment with identified risks and recommendations</returns>
        Task<RiskAssessment> AssessDataRiskAsync(Guid workspaceId, CancellationToken cancellationToken = default);

        #endregion
    }
}