using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace IIM.Application.Commands.Investigation
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
    public class GetSessionsByCaseCommand : IRequest<List<InvestigationSession>>
    {
        public string CaseId { get; }

       
    }

    public class GetSessionsByCaseCommandHandler : IRequestHandler<GetSessionsByCaseCommand, List<InvestigationSession>>
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
            GetSessionsByCaseCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting sessions for case {CaseId}", request.CaseId);
            return await _sessionService.GetSessionsByCaseAsync(request.CaseId, cancellationToken);
        }
    }


}