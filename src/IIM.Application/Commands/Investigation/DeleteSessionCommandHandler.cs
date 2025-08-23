using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Handles session deletion or archival.
    /// </summary>
    public class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand, bool>
    {
        private readonly ILogger<DeleteSessionCommandHandler> _logger;
        private readonly ISessionService _sessionService;

        public DeleteSessionCommandHandler(
            ILogger<DeleteSessionCommandHandler> logger,
            ISessionService sessionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        public async Task<bool> Handle(
            DeleteSessionCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing deletion request for session {SessionId}. Archive only: {ArchiveOnly}",
                request.SessionId, request.ArchiveOnly);

            if (request.ArchiveOnly)
            {
                // Archive by updating status
                await _sessionService.UpdateSessionAsync(
                    request.SessionId,
                    session => session.Status = InvestigationStatus.Archived,
                    cancellationToken);

                _logger.LogInformation("Session {SessionId} archived. Reason: {Reason}",
                    request.SessionId, request.Reason ?? "Not specified");

                return true;
            }
            else
            {
                // Delete the session
                var result = await _sessionService.DeleteSessionAsync(request.SessionId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Session {SessionId} deleted. Reason: {Reason}",
                        request.SessionId, request.Reason ?? "Not specified");
                }

                return result;
            }
        }
    }
}