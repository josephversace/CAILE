using IIM.Shared.Mediator;
using IIM.Core.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace IIM.Application.Investigation
{
    /// <summary>
    /// Command to retrieve an investigation session by ID.
    /// </summary>
    public class GetSessionCommand : IRequest<InvestigationSession>
    {
        /// <summary>
        /// Gets the session ID to retrieve.
        /// </summary>
        [Required]
        public string SessionId { get; }

        /// <summary>
        /// Gets whether to include full message history.
        /// </summary>
        public bool IncludeMessages { get; }

        /// <summary>
        /// Gets the maximum number of messages to include.
        /// </summary>
        public int? MaxMessages { get; }

        /// <summary>
        /// Initializes a new instance of the GetSessionCommand.
        /// </summary>
        /// <param name="sessionId">Session ID to retrieve</param>
        /// <param name="includeMessages">Whether to include messages</param>
        /// <param name="maxMessages">Maximum messages to include</param>
        public GetSessionCommand(string sessionId, bool includeMessages = true, int? maxMessages = null)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            IncludeMessages = includeMessages;
            MaxMessages = maxMessages;
        }
    }

    /// <summary>
    /// Query to get sessions by case ID
    /// </summary>
    public class GetSessionsByWorkspaceCommand : IRequest<List<InvestigationSession>>
    {
        public Guid WorkspaceId { get; }

        public GetSessionsByWorkspaceCommand(string workspaceId)
        {
            var guid = Guid.Parse(workspaceId);

            WorkspaceId = guid;
        }
    }

    public class GetSessionsByCaseCommandHandler : IRequestHandler<GetSessionsByWorkspaceCommand, List<InvestigationSession>>
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<GetSessionsByCaseCommandHandler> _logger;

        public GetSessionsByCaseCommandHandler(
            ISessionService sessionService,
            ILogger<GetSessionsByCaseCommandHandler> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        public async Task<List<InvestigationSession>> Handle(
            GetSessionsByWorkspaceCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting sessions for case {CaseId}", request.WorkspaceId);
            return await _sessionService.GetSessionsByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        }
    }


}