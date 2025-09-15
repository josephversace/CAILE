using IIM.Shared.Mediator;
using IIM.Core.Services;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using IIM.Shared.Models.Core;

namespace IIM.Application.Investigation
{
    /// <summary>
    /// Handler for retrieving an investigation session by ID.
    /// Optionally includes message history and filters messages.
    /// </summary>
    public class GetSessionCommandHandler : IRequestHandler<GetSessionCommand, InvestigationSession>
    {
        private readonly ILogger<GetSessionCommandHandler> _logger;
        private readonly ISessionService _sessionService;

        /// <summary>
        /// Initializes a new instance of the GetSessionCommandHandler.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output</param>
        /// <param name="sessionService">Service for session management</param>
        public GetSessionCommandHandler(
            ILogger<GetSessionCommandHandler> logger,
            ISessionService sessionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        /// <summary>
        /// Handles the GetSessionCommand to retrieve a session.
        /// </summary>
        /// <param name="request">Command containing session ID and options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The requested investigation session</returns>
        /// <exception cref="KeyNotFoundException">Thrown when session is not found</exception>
        public async Task<InvestigationSession> Handle(
            GetSessionCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving session {SessionId}. Include messages: {IncludeMessages}",
                request.SessionId, request.IncludeMessages);

            // Get the session
            var session = await _sessionService.GetSessionAsync(request.SessionId, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found", request.SessionId);
                throw new KeyNotFoundException($"Session {request.SessionId} not found");
            }

            // Handle message filtering if requested
            if (!request.IncludeMessages)
            {
                // Clear messages if not requested
                session.Messages = new List<InvestigationMessage>();
                _logger.LogDebug("Messages excluded from session response");
            }
            else if (request.MaxMessages.HasValue && request.MaxMessages.Value > 0)
            {
                // Limit messages to the requested maximum (most recent)
                var messageCount = session.Messages.Count;
                if (messageCount > request.MaxMessages.Value)
                {
                    session.Messages = session.Messages
                        .OrderByDescending(m => m.Timestamp)
                        .Take(request.MaxMessages.Value)
                        .OrderBy(m => m.Timestamp) // Restore chronological order
                        .ToList();

                    _logger.LogDebug("Limited messages from {Total} to {Limited}",
                        messageCount, request.MaxMessages.Value);
                }
            }

            // Calculate session statistics
            if (session.Messages.Any())
            {
                //    session.Metadata["MessageCount"] = session.Messages.Count;
                //    session.Metadata["FirstMessageAt"] = session.Messages.Min(m => m.Timestamp);
                //    session.Metadata["LastMessageAt"] = session.Messages.Max(m => m.Timestamp);
                //    session.Metadata["UserMessageCount"] = session.Messages.Count(m => m.Role == MessageRole.User);
                //    session.Metadata["AssistantMessageCount"] = session.Messages.Count(m => m.Role == MessageRole.Assistant);
                //
            }

            _logger.LogInformation("Successfully retrieved session {SessionId} with {MessageCount} messages",
                session.Id, session.Messages.Count);

            return session;
        }
    }
}