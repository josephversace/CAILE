
using IIM.Core.AI;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Enums;

using Microsoft.Extensions.Logging;
using System.Text.Json;
using IIM.Shared.Models;
using IIM.Application.Investigation;
using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;

/// Handles processing of investigation queries - adapted to actual shared models.
/// </summary>
public class ProcessInvestigationCommandHandler : IRequestHandler<ProcessInvestigationCommand, InvestigationResponse>
{
    private readonly ILogger<ProcessInvestigationCommandHandler> _logger;
    private readonly ISessionService _sessionService;
    private readonly IWorkspaceManager _workspaceManager;
    private readonly IReasoningService _reasoningService;
    private readonly IInferenceService _inferenceService;
    private readonly IManagedFileManager _fileManager;
    private readonly IVisualizationService _visualizationService;

    public ProcessInvestigationCommandHandler(
        ILogger<ProcessInvestigationCommandHandler> logger,
        ISessionService sessionService,
        IWorkspaceManager workspaceManager,
        IReasoningService reasoningService,
        IInferenceService inferenceService,
        IManagedFileManager fileManager,
        IVisualizationService visualizationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _reasoningService = reasoningService ?? throw new ArgumentNullException(nameof(reasoningService));
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        _visualizationService = visualizationService ?? throw new ArgumentNullException(nameof(visualizationService));
    }

    public async Task<InvestigationResponse> Handle(
        ProcessInvestigationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing query for session {SessionId}", request.SessionId);

        var session = await _sessionService.GetSessionAsync(request.SessionId, cancellationToken);
        var workspaceEntity = await _workspaceManager.GetWorkspaceAsync(session.WorkspaceId, cancellationToken);

        // Build InvestigationQuery
        var query = new InvestigationQuery
        {
            Text = request.Query,
            Attachments = request.Attachments,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Create and persist user message
        var userMessage = new InvestigationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = MessageRole.User,
            Content = query.Text,
            Attachments = query.Attachments,
            Timestamp = query.Timestamp
            // Note: SessionId doesn't exist on InvestigationMessage
        };

        await _sessionService.AddMessageAsync(request.SessionId, userMessage, cancellationToken);

        // Process attachments - adapt to actual Attachment model
        if (query.Attachments?.Any() == true)
        {
            await ProcessAttachmentsAsync(session.WorkspaceId, query.Attachments, cancellationToken);
        }

        // Use reasoning service
        var reasoningResult = await _reasoningService.ProcessQueryAsync(
            query.Text,
            session,
            cancellationToken);

        // Execute reasoning plan
        var responseText = await ExecuteReasoningPlanAsync(
            reasoningResult,
            query,
            session,
            workspaceEntity,
            cancellationToken);

        // Build response - adapt to actual InvestigationResponse model
        var response = new InvestigationResponse
        {
            Id = Guid.NewGuid().ToString(),
            // SessionId doesn't exist on InvestigationResponse
            Message = responseText,
            Confidence = reasoningResult.Confidence,
           // DisplayType = _visualizationService.DetermineDisplayType(),
            Metadata = new Dictionary<string, object>
            {
                ["SessionId"] = request.SessionId, // Store in metadata instead
                ["ProcessingTime"] = reasoningResult.ProcessingTime.TotalMilliseconds,
                ["IntentCategory"] = reasoningResult.Intent?.PrimaryIntent ?? "Unknown"
            }
        };

        // Handle citations - use actual Citation model structure
        if (reasoningResult.ActionPlan?.Any(s => s.Action.Contains("RAG")) == true)
        {
            var ragResponse = await _inferenceService.QueryDocumentsAsync(
                query.Text,
                session.WorkspaceId,
                cancellationToken);

            // Map to actual Citation structure (no Index property)
            var citations = new List<Citation>();
            foreach (var chunk in ragResponse.Chunks ?? new List<object>())
            {
                citations.Add(new Citation
                {
                    SourceId = Guid.NewGuid().ToString(),
                    SourceType = "Document",
                    Text = chunk.ToString() ?? "",
                    Relevance = 0.85
                });
            }
            response.Citations = citations;
        }

        // Get related evidence
        response.RelatedFiles = await GetRelatedFilesAsync(
            session.WorkspaceId,
            query.Text,
            5,
            cancellationToken);

        // Persist assistant message
        var assistantMessage = new InvestigationMessage
        {
            Id = response.Id,
            Role = MessageRole.Assistant,
            Content = response.Message,
            Citations = response.Citations,
            Timestamp = DateTimeOffset.UtcNow,
            ModelUsed = reasoningResult.RecommendedModel ?? "default"
        };

        await _sessionService.AddMessageAsync(request.SessionId, assistantMessage, cancellationToken);

        return response;
    }

    private async Task ProcessAttachmentsAsync(
        string caseId,
        List<Attachment> attachments,
        CancellationToken cancellationToken)
    {
        foreach (var attachment in attachments)
        {
            // Create file record from Attachment
            var file = new ManagedFile
            {
                Id = Guid.NewGuid().ToString(),
                WorkspaceId = caseId,
            
                OriginalFileName = attachment.FileName,
                FileSize = attachment.Size,
                Type = MapAttachmentTypeToEvidenceType(attachment.Type),
                Status = FileUploadStatus.Pending,
                StoragePath = attachment.StoragePath ?? "",
                Hash = "", // Will be computed during ingestion
                IngestTimestamp = DateTimeOffset.UtcNow,
                Metadata = new FileMetadata
                {
                    //CaseNumber = caseId,
                    //CollectedBy = Environment.UserName,
                    //CollectionDate = DateTimeOffset.UtcNow
                }
            };

            // Note: AddEvidenceAsync doesn't exist, use IngestEvidenceAsync
            using var stream = new MemoryStream();
            await _fileManager.IngestFileAsync(
                stream,
                attachment.FileName,
                file.Metadata,
                cancellationToken);
        }
    }

    private FileType MapAttachmentTypeToEvidenceType(AttachmentType attachmentType)
    {
        return attachmentType switch
        {
            AttachmentType.Image => FileType.Image,
            AttachmentType.Document => FileType.Document,
            AttachmentType.Audio => FileType.Audio,
            AttachmentType.Video => FileType.Video,
            AttachmentType.Archive => FileType.Archive,
            _ => FileType.Other
        };
    }

    private async Task<string> ExecuteReasoningPlanAsync(
        ReasoningResult reasoningResult,
        InvestigationQuery query,
        InvestigationSession session,
        Workspace workspaceEntity,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = new
            {
                Query = query.Text,
                Context = reasoningResult.ExtractedEntities,
                SessionId = session.Id,
                WorkspaceId = workspaceEntity.Id
            };

            var responseText = await _inferenceService.InferAsync(
                JsonSerializer.Serialize(context),
                cancellationToken);

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing reasoning plan");
            return "I encountered an error while processing your request. Please try again.";
        }
    }

    private async Task<List<VirtualFile>> GetRelatedFilesAsync(
        string caseId,
        string queryText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var evidence = await _fileManager.GetFilesByWorkspaceAsync(caseId, cancellationToken);
        return evidence.Take(maxResults).ToList();
    }
}
