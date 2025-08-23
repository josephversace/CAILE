using IIM.Core.Mediator;
using IIM.Shared.DTOs;
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
}