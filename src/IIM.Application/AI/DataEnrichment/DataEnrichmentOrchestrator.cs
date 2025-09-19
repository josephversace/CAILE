using IIM.Application.AI.DataEnrichment.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment
{
    /// <summary>
    /// Main orchestrator that coordinates data enrichment services.
    /// Delegates specific tasks to specialized services.
    /// </summary>
    public class DataEnrichmentOrchestrator : IDataReasoningService
    {
        private readonly ILogger<DataEnrichmentOrchestrator> _logger;
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly IFileClassificationService _classificationService;
        private readonly IDataQueryService _queryService;
        private readonly IGovernanceSuggestionService _governanceService;
        private readonly IRiskAssessmentService _riskAssessmentService;
        private readonly IObjectStorageProvider _storageProvider;

        public DataEnrichmentOrchestrator(
            ILogger<DataEnrichmentOrchestrator> logger,
            IContentAnalysisService contentAnalysisService,
            IFileClassificationService classificationService,
            IDataQueryService queryService,
            IGovernanceSuggestionService governanceService,
            IRiskAssessmentService riskAssessmentService,
            IObjectStorageProvider storageProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contentAnalysisService = contentAnalysisService ?? throw new ArgumentNullException(nameof(contentAnalysisService));
            _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _governanceService = governanceService ?? throw new ArgumentNullException(nameof(governanceService));
            _riskAssessmentService = riskAssessmentService ?? throw new ArgumentNullException(nameof(riskAssessmentService));
            _storageProvider = storageProvider;
        }




        #region Events
        public event EventHandler<AnalysisStartedEventArgs>? AnalysisStarted;
        public event EventHandler<AnalysisCompletedEventArgs>? AnalysisCompleted;
        public event EventHandler<AnalysisErrorEventArgs>? AnalysisError;
        #endregion

        #region IDataReasoningService Implementation - Delegates to Specialized Services

        public async Task ProcessUploadedFile(string bucketName, string objectKey, long fileSize)
        {
            try
            {
                // Fetch the file from storage
                using var stream = await _storageProvider.GetObjectAsync(bucketName, objectKey);

                // Use the existing method
                var analysis = await AnalyzeFileContentAsync(
                    stream,
                    objectKey,
                    "application/octet-stream", // Determine from file
                    CancellationToken.None);

              

                // Check if needs to move based on classification
                //if (analysis.StructuredData != null)
                //{
                //    var routingDecision = new RoutingDecision
                //    {
                //        TargetBucket = DetermineTargetBucket(analysis.Classification),
                //        RequiresQuarantine = analysis.Classification.RiskScore > 0.7f
                //    };

                //    if (routingDecision.TargetBucket != bucketName)
                //    {
                //        // Move file to appropriate bucket
                //        await _storageProvider.CopyObjectAsync(
                //            bucketName, objectKey,
                //            routingDecision.TargetBucket, objectKey);

                //        await _storageProvider.DeleteObjectAsync(bucketName, objectKey);
                //    }
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process uploaded file {ObjectKey}", objectKey);
                throw;
            }
        }

       public async Task ProcessFileFromStorageAsync(
       string bucketName,
       string objectKey,
       long fileSize)
        {
            try
            {
                _logger.LogInformation("Processing file {ObjectKey} from bucket {Bucket}", objectKey, bucketName);

                // Fetch the file from storage
                using var stream = await _storageProvider.GetObjectAsync(bucketName, objectKey);

                // Use existing analysis method
                var analysis = await AnalyzeFileContentAsync(
                    stream,
                    objectKey,
                    "application/octet-stream", // You could determine this from the file
                    CancellationToken.None);

                // Log results
                //_logger.LogInformation(
                //    "Completed processing {ObjectKey}. Classification: {Category}, Sensitivity: {Sensitivity}",
                //    objectKey,
                //    analysis.Classification?.Category,
                //    analysis.Classification?.Sensitivity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process file {ObjectKey} from {Bucket}", objectKey, bucketName);
                throw;
            }
        }

        private string DetermineTargetBucket(ClassificationData classification)
        {
            // Use the existing classification data
            if (classification.Level == DataClassificationLevel.Confidential)
                return "primary/sensitive";
            if (classification.Level == DataClassificationLevel.Confidential)
                return "primary/nodedup";
            return "primary/objects";
        }

        public async Task<ContentAnalysis> AnalyzeFileContentAsync(Stream content, string fileName, string mimeType, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Orchestrating content analysis for file {FileName}", fileName);

            OnAnalysisStarted(new AnalysisStartedEventArgs { FileName = fileName, AnalysisType = "ContentAnalysis", StartedAt = DateTime.UtcNow });

            try
            {
                var result = await _contentAnalysisService.AnalyzeContentAsync(content, fileName, mimeType, cancellationToken);

                OnAnalysisCompleted(new AnalysisCompletedEventArgs { FileName = fileName, AnalysisType = "ContentAnalysis", Success = true, CompletedAt = DateTime.UtcNow });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error orchestrating content analysis for {FileName}", fileName);
                OnAnalysisError(new AnalysisErrorEventArgs { FileName = fileName, AnalysisType = "ContentAnalysis", ErrorMessage = ex.Message, Exception = ex });
                throw;
            }
        }

        public async Task<ClassificationSuggestion> SuggestClassificationAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Orchestrating classification for file {FileName}", fileName);
            return await _classificationService.SuggestClassificationAsync(fileName, content, cancellationToken);
        }

        public async Task<EntityExtractionResult> ExtractEntitiesAsync(Stream content, string mimeType, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Orchestrating entity extraction for content type {MimeType}", mimeType);
            return await _contentAnalysisService.ExtractEntitiesAsync(content, mimeType, cancellationToken);
        }

        public async Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default)
        {
            return await _contentAnalysisService.GenerateEmbeddingsAsync(text, cancellationToken);
        }

        public async Task<QueryResult> ProcessDataQueryAsync(string query, Guid? workspaceId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Orchestrating data query: {Query}", query);
            return await _queryService.ProcessQueryAsync(query, workspaceId, cancellationToken);
        }

        public async Task<DataInsight> GenerateDataInsightAsync(string question, Guid? workspaceId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Orchestrating data insight generation for question: {Question}", question);
            return await _queryService.GenerateInsightAsync(question, workspaceId, cancellationToken);
        }

        public async Task<SimilarityResult> FindSimilarFilesAsync(Guid virtualFileId, int maxResults = 10, CancellationToken cancellationToken = default)
        {
            return await _queryService.FindSimilarFilesAsync(virtualFileId, maxResults, cancellationToken);
        }

        public async Task<PolicySuggestion> SuggestGovernanceRulesAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Orchestrating governance rule suggestions for workspace {WorkspaceId}", workspaceId);
            return await _governanceService.SuggestGovernanceRulesAsync(workspaceId, cancellationToken);
        }

        public async Task<ComplianceCheck> CheckComplianceAsync(VirtualFile file, GovernanceFramework rules, CancellationToken cancellationToken = default)
        {
            return await _governanceService.CheckComplianceAsync(file, rules, cancellationToken);
        }

        public async Task<RiskAssessment> AssessDataRiskAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Orchestrating risk assessment for workspace {WorkspaceId}", workspaceId);
            return await _riskAssessmentService.AssessWorkspaceRiskAsync(workspaceId, cancellationToken);
        }

        #endregion

        #region Event Helpers
        private void OnAnalysisStarted(AnalysisStartedEventArgs e) => AnalysisStarted?.Invoke(this, e);
        private void OnAnalysisCompleted(AnalysisCompletedEventArgs e) => AnalysisCompleted?.Invoke(this, e);
        private void OnAnalysisError(AnalysisErrorEventArgs e) => AnalysisError?.Invoke(this, e);
        #endregion
    }
}