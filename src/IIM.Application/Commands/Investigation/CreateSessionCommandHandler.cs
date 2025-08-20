using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Fixed handler for creating investigation sessions
    /// </summary>
    public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, InvestigationSession>
    {
        private readonly ISessionService _sessionService;
        private readonly IMediator _mediator;
        private readonly ILogger<CreateSessionCommandHandler> _logger;

        public CreateSessionCommandHandler(
            ISessionService sessionService,
            IMediator mediator,
            ILogger<CreateSessionCommandHandler> logger)
        {
            _sessionService = sessionService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<InvestigationSession> Handle(
            CreateSessionCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating new investigation session for case {CaseId}", request.CaseId);

                // Use only the properties that CreateSessionRequest actually has
                var createRequest = new CreateSessionRequest
                {
                    CaseId = request.CaseId,
                    Title = request.Title,
                    InvestigationType = request.Type.ToString()
                };

                var session = await _sessionService.CreateSessionAsync(createRequest, cancellationToken);

                // Set additional properties on the session after creation
                if (request.EnabledTools != null)
                {
                    session.EnabledTools = request.EnabledTools;
                }

                await _mediator.Publish(new SessionCreatedNotification
                {
                    SessionId = session.Id,
                    CaseId = session.CaseId,
                    Title = session.Title,
                    Timestamp = session.CreatedAt
                }, cancellationToken);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create investigation session");
                throw;
            }
        }
    }
}