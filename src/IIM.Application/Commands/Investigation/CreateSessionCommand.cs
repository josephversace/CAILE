using IIM.Core.Mediator;

using System.ComponentModel.DataAnnotations;
using IIM.Shared.Models;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Command to create a new investigation session within a case.
    /// The session inherits the case's model configuration and evidence pool.
    /// </summary>
    public class CreateSessionCommand : IRequest<InvestigationSession>
    {
        /// <summary>
        /// Gets the unique identifier of the case this session belongs to.
        /// </summary>
        [Required]
        public string CaseId { get; }

        /// <summary>
        /// Gets the title or description of the session.
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; }

        /// <summary>
        /// Gets the type of investigation for this session.
        /// </summary>
        [Required]
        public string InvestigationType { get; }

        /// <summary>
        /// Gets the optional description providing context for the session.
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; }

        /// <summary>
        /// Gets the user ID of the person creating the session.
        /// </summary>
        public string? UserId { get; }

        /// <summary>
        /// Gets optional metadata for the session.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; }

        /// <summary>
        /// Initializes a new instance of the CreateSessionCommand.
        /// </summary>
        /// <param name="caseId">Case ID this session belongs to</param>
        /// <param name="title">Title of the session</param>
        /// <param name="investigationType">Type of investigation</param>
        public CreateSessionCommand(string caseId, string title, string investigationType)
        {
            CaseId = caseId ?? throw new ArgumentNullException(nameof(caseId));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            InvestigationType = investigationType ?? throw new ArgumentNullException(nameof(investigationType));
            Metadata = new Dictionary<string, object>();
        }
    }
}