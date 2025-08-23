// File: IIM.Application/Commands/Investigation/ProcessInvestigationCommand.cs
using IIM.Core.Mediator;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Command to process an investigation query within a session.
    /// This matches the existing usage in Program.cs and IIMApiClient.
    /// </summary>
    public class ProcessInvestigationCommand : IRequest<InvestigationResponse>
    {
        /// <summary>
        /// Gets or sets the session ID to process the query in.
        /// Property setter needed for model binding from HTTP POST.
        /// </summary>
        [Required]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the query text to process.
        /// This matches the usage in IIMApiClient.
        /// </summary>
        [Required]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional attachments for the query.
        /// </summary>
        public List<Attachment>? Attachments { get; set; }

        /// <summary>
        /// Gets or sets the user ID for audit trail.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Default constructor for model binding.
        /// </summary>
        public ProcessInvestigationCommand() { }

        /// <summary>
        /// Initializes a new instance with required fields.
        /// </summary>
        /// <param name="sessionId">Session ID where query will be processed</param>
        /// <param name="query">Query text to process</param>
        public ProcessInvestigationCommand(string sessionId, string query)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            Query = query ?? throw new ArgumentNullException(nameof(query));
        }
    }

   
}