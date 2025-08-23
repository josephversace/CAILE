using IIM.Application.Commands.Investigation;
using IIM.Application.Interfaces;
using IIM.Core.AI;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Enums;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// Handles processing of investigation queries - adapted to actual shared models.
/// </summary>
public class ProcessInvestigationCommandHandler : IRequestHandler<ProcessInvestigationCommand, InvestigationResponse>
{
    private readonly ILogger<ProcessInvestigationCommandHandler> _logger;
    private readonly ISessionService _sessionService;
    private readonly ICaseManager _caseManager;
    private readonly IReasoningService _reasoningService;
    private readonly IInferenceService _inferenceService;
    private readonly IEvidenceManager _evidenceManager;
    private readonly IVisualizationService _visualizationService;

    public ProcessInvestigationCommandHandler(
        ILogger<ProcessInvestigationCommandHandler> logger,
        ISessionService sessionService,
        ICaseManager caseManager,
        IReasoningService reasoningService,
        IInferenceService inferenceService,
        IEvidenceManager evidenceManager,
        IVisualizationService visualizationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
        _reasoningService = reasoningService ?? throw new ArgumentNullException(nameof(reasoningService));
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _evidenceManager = evidenceManager ?? throw new ArgumentNullException(nameof(evidenceManager));
        _visualizationService = visualizationService ?? throw new ArgumentNullException(nameof(visualizationService));
    }

    public async Task<InvestigationResponse> Handle(
        ProcessInvestigationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing query for session {SessionId}", request.SessionId);

        var session = await _sessionService.GetSessionAsync(request.SessionId, cancellationToken);
        var caseEntity = await _caseManager.GetCaseAsync(session.CaseId, cancellationToken);

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
            await ProcessAttachmentsAsync(session.CaseId, query.Attachments, cancellationToken);
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
            caseEntity,
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
                session.CaseId,
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
        response.RelatedEvidence = await GetRelatedEvidenceAsync(
            session.CaseId,
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
            // Create Evidence record from Attachment
            var evidence = new Evidence
            {
                Id = Guid.NewGuid().ToString(),
                CaseId = caseId,
                CaseNumber = caseId, // Assuming caseId is the case number
                OriginalFileName = attachment.FileName,
                FileSize = attachment.Size,
                Type = MapAttachmentTypeToEvidenceType(attachment.Type),
                Status = EvidenceStatus.Pending,
                StoragePath = attachment.StoragePath ?? "",
                Hash = "", // Will be computed during ingestion
                IngestTimestamp = DateTimeOffset.UtcNow,
                Metadata = new EvidenceMetadata
                {
                    CaseNumber = caseId,
                    CollectedBy = Environment.UserName,
                    CollectionDate = DateTimeOffset.UtcNow
                }
            };

            // Note: AddEvidenceAsync doesn't exist, use IngestEvidenceAsync
            using var stream = new MemoryStream();
            await _evidenceManager.IngestEvidenceAsync(
                stream,
                attachment.FileName,
                evidence.Metadata,
                cancellationToken);
        }
    }

    private EvidenceType MapAttachmentTypeToEvidenceType(AttachmentType attachmentType)
    {
        return attachmentType switch
        {
            AttachmentType.Image => EvidenceType.Image,
            AttachmentType.Document => EvidenceType.Document,
            AttachmentType.Audio => EvidenceType.Audio,
            AttachmentType.Video => EvidenceType.Video,
            AttachmentType.Archive => EvidenceType.Archive,
            _ => EvidenceType.Other
        };
    }

    private async Task<string> ExecuteReasoningPlanAsync(
        ReasoningResult reasoningResult,
        InvestigationQuery query,
        InvestigationSession session,
        Case caseEntity,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = new
            {
                Query = query.Text,
                Context = reasoningResult.ExtractedEntities,
                SessionId = session.Id,
                CaseId = caseEntity.Id
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

    private async Task<List<Evidence>> GetRelatedEvidenceAsync(
        string caseId,
        string queryText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var evidence = await _evidenceManager.GetEvidenceByCaseAsync(caseId, cancellationToken);
        return evidence.Take(maxResults).ToList();
    }
}
