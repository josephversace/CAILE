using IIM.Application.Interfaces;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Handles execution of investigation tools within sessions.
    /// </summary>
    public class ExecuteToolCommandHandler : IRequestHandler<ExecuteToolCommand, ToolResult>
    {
        private readonly ILogger<ExecuteToolCommandHandler> _logger;
        private readonly ISessionService _sessionService;
        private readonly IInferenceService _inferenceService;
        private readonly IEvidenceManager _evidenceManager;
        private readonly ICaseManager _caseManager;

        /// <summary>
        /// Initializes a new instance of the ExecuteToolCommandHandler.
        /// </summary>
        public ExecuteToolCommandHandler(
            ILogger<ExecuteToolCommandHandler> logger,
            ISessionService sessionService,
            IInferenceService inferenceService,
            IEvidenceManager evidenceManager,
            ICaseManager caseManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
            _evidenceManager = evidenceManager ?? throw new ArgumentNullException(nameof(evidenceManager));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
        }

        /// <summary>
        /// Handles the ExecuteToolCommand to run a specific tool.
        /// </summary>
        /// <param name="request">Command containing tool name and parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of tool execution</returns>
        public async Task<ToolResult> Handle(
            ExecuteToolCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Executing tool {ToolName} for session {SessionId}",
                request.ToolName, request.SessionId);

            var startTime = DateTimeOffset.UtcNow;
            var session = await _sessionService.GetSessionAsync(request.SessionId, cancellationToken);

            try
            {
                var result = request.ToolName.ToLowerInvariant() switch
                {
                    "image_search" or "clip_search" =>
                        await ExecuteImageSearchAsync(request.Parameters, session.CaseId, cancellationToken),

                    "transcribe" or "whisper" =>
                        await ExecuteTranscriptionAsync(request.Parameters, cancellationToken),

                    "rag_query" or "document_search" =>
                        await ExecuteDocumentSearchAsync(request.Parameters, session.CaseId, cancellationToken),

                    "evidence_analysis" =>
                        await ExecuteEvidenceAnalysisAsync(request.Parameters, session.CaseId, cancellationToken),

                    "timeline_generation" =>
                        await ExecuteTimelineGenerationAsync(session.CaseId, cancellationToken),

                    _ => throw new NotSupportedException($"Tool '{request.ToolName}' is not supported")
                };

                result.ExecutedAt = startTime;
                result.ExecutionTime = DateTimeOffset.UtcNow - startTime;
                result.Metadata["SessionId"] = request.SessionId;

                _logger.LogInformation("Tool {ToolName} executed successfully in {Time}ms",
                    request.ToolName, result.ExecutionTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool execution failed for {ToolName}", request.ToolName);

                return new ToolResult
                {
                    Id = Guid.NewGuid().ToString(),
                    ToolName = request.ToolName,
                    Status = ToolStatus.Failed,
                    ErrorMessage = ex.Message,
                    ExecutedAt = startTime,
                    ExecutionTime = DateTimeOffset.UtcNow - startTime,
                    Metadata = new Dictionary<string, object>
                    {
                        ["SessionId"] = request.SessionId,
                        ["ErrorType"] = ex.GetType().Name
                    }
                };
            }
        }

        /// <summary>
        /// Executes image search using CLIP model.
        /// </summary>
        private async Task<ToolResult> ExecuteImageSearchAsync(
            Dictionary<string, object> parameters,
            string caseId,
            CancellationToken cancellationToken)
        {
            var imageData = parameters.GetValueOrDefault("ImageData") as byte[];
            if (imageData == null)
            {
                return CreateFailedResult("image_search", "No image data provided");
            }

            var searchResults = await _inferenceService.SearchImagesAsync(imageData, 5, cancellationToken);

            return new ToolResult
            {
                Id = Guid.NewGuid().ToString(),
                ToolName = "image_search",
                Status = ToolStatus.Success,
                Data = searchResults,
                Visualizations = new List<Visualization>
                {
                    new Visualization
                    {
                        Type = VisualizationType.Table,
                        Title = "Similar Images",
                        Data = searchResults.Matches
                    }
                },
                PreferredDisplayType = ResponseDisplayType.Image
            };
        }

        /// <summary>
        /// Executes audio transcription using Whisper model.
        /// </summary>
        private async Task<ToolResult> ExecuteTranscriptionAsync(
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken)
        {
            var audioPath = parameters.GetValueOrDefault("AudioPath")?.ToString();
            if (string.IsNullOrEmpty(audioPath))
            {
                return CreateFailedResult("transcribe", "No audio file path provided");
            }

            var language = parameters.GetValueOrDefault("Language")?.ToString() ?? "en";
            var transcription = await _inferenceService.TranscribeAudioAsync(audioPath, language, cancellationToken);

            return new ToolResult
            {
                Id = Guid.NewGuid().ToString(),
                ToolName = "transcribe",
                Status = ToolStatus.Success,
                Data = transcription,
                Metadata = new Dictionary<string, object>
                {
                    ["Duration"] = transcription.Duration,
                    ["Language"] = transcription.Language,
                    ["Confidence"] = transcription.Confidence
                }
            };
        }

        /// <summary>
        /// Executes document search using RAG pipeline.
        /// </summary>
        private async Task<ToolResult> ExecuteDocumentSearchAsync(
            Dictionary<string, object> parameters,
            string caseId,
            CancellationToken cancellationToken)
        {
            var query = parameters.GetValueOrDefault("Query")?.ToString() ?? "";
            var ragResponse = await _inferenceService.QueryDocumentsAsync(query, caseId, cancellationToken);

            return new ToolResult
            {
                Id = Guid.NewGuid().ToString(),
                ToolName = "document_search",
                Status = ToolStatus.Success,
                Data = ragResponse,
                Metadata = new Dictionary<string, object>
                {
                    ["ChunksRetrieved"] = ragResponse.RetrievedChunks.Count,
                    ["Confidence"] = ragResponse.Confidence
                }
            };
        }

        /// <summary>
        /// Executes evidence analysis.
        /// </summary>
        private async Task<ToolResult> ExecuteEvidenceAnalysisAsync(
            Dictionary<string, object> parameters,
            string caseId,
            CancellationToken cancellationToken)
        {
            var evidence = await _evidenceManager.GetEvidenceByCaseAsync(caseId, cancellationToken);

            return new ToolResult
            {
                Id = Guid.NewGuid().ToString(),
                ToolName = "evidence_analysis",
                Status = ToolStatus.Success,
                Data = new
                {
                    TotalEvidence = evidence.Count,
                    FileTypes = evidence.GroupBy(e => e.FileType)
                        .Select(g => new { Type = g.Key, Count = g.Count() }),
                    TotalSize = evidence.Sum(e => e.FileSize),
                    DateRange = new
                    {
                        Earliest = evidence.Min(e => e.UploadedAt),
                        Latest = evidence.Max(e => e.UploadedAt)
                    }
                },
                Visualizations = new List<Visualization>
                {
                    new Visualization
                    {
                        Type = VisualizationType.Chart,
                        Title = "Evidence Distribution",
                        Data = evidence.GroupBy(e => e.FileType)
                    }
                }
            };
        }

        /// <summary>
        /// Executes timeline generation for a case.
        /// </summary>
        private async Task<ToolResult> ExecuteTimelineGenerationAsync(
            string caseId,
            CancellationToken cancellationToken)
        {
            var events = await _caseManager.GetCaseTimelineAsync(caseId, cancellationToken);

            return new ToolResult
            {
                Id = Guid.NewGuid().ToString(),
                ToolName = "timeline_generation",
                Status = ToolStatus.Success,
                Data = events,
                Visualizations = new List<Visualization>
                {
                    new Visualization
                    {
                        Type = VisualizationType.Timeline,
                        Title = "Case Timeline",
                        Data = events
                    }
                },
                PreferredDisplayType = ResponseDisplayType.Timeline
            };
        }

        /// <summary>
        /// Creates a failed tool result.
        /// </summary>
        private ToolResult CreateFailedResult(string toolName, string errorMessage)
        {
            return new ToolResult
            {
                Id = Guid.NewGuid().ToString(),
                ToolName = toolName,
                Status = ToolStatus.Failed,
                ErrorMessage = errorMessage
            };
        }
    }
}