using IIM.Core.Configuration;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Handles creation of new investigation sessions - adapted to actual models.
    /// </summary>
    public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, InvestigationSession>
    {
        private readonly ILogger<CreateSessionCommandHandler> _logger;
        private readonly ISessionService _sessionService;
        private readonly ICaseManager _caseManager;
        private readonly IModelConfigurationTemplateService _templateService;

        public CreateSessionCommandHandler(
            ILogger<CreateSessionCommandHandler> logger,
            ISessionService sessionService,
            ICaseManager caseManager,
            IModelConfigurationTemplateService templateService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        }

        public async Task<InvestigationSession> Handle(
            CreateSessionCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating session for case {CaseId}: {Title}",
                request.CaseId, request.Title);

            // Verify case exists
            var caseEntity = await _caseManager.GetCaseAsync(request.CaseId, cancellationToken);
            if (caseEntity == null)
            {
                throw new InvalidOperationException($"Case {request.CaseId} not found");
            }

            // Create session request - use only properties that exist
            var createRequest = new CreateSessionRequest(
                request.CaseId,
                request.Title,
                request.InvestigationType);
            // Note: Description, UserId, Metadata don't exist on CreateSessionRequest

            // Create session
            var session = await _sessionService.CreateSessionAsync(createRequest, cancellationToken);

            // Apply template if case has one
            // Note: Case doesn't have ModelTemplateId property
            // Check if template exists in case metadata instead
            if (caseEntity.Metadata?.ContainsKey("ModelTemplateId") == true)
            {
                var templateId = caseEntity.Metadata["ModelTemplateId"]?.ToString();
                if (!string.IsNullOrEmpty(templateId))
                {
                    session = await _templateService.ApplyTemplateToSessionAsync(
                        templateId,
                        session.Id,
                        cancellationToken);
                }
            }

            _logger.LogInformation("Created session {SessionId} for case {CaseId}",
                session.Id, request.CaseId);

            return session;
        }
    }
}
