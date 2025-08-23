using IIM.Core.Mediator;
using System.ComponentModel.DataAnnotations;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Command to delete an investigation session and all associated messages.
    /// This operation is audited for compliance.
    /// </summary>
    public class DeleteSessionCommand : IRequest<bool>
    {
        /// <summary>
        /// Gets the ID of the session to delete.
        /// </summary>
        [Required]
        public string SessionId { get; }

        /// <summary>
        /// Gets the reason for deletion (for audit trail).
        /// </summary>
        [StringLength(500)]
        public string? Reason { get; }

        /// <summary>
        /// Gets the user ID requesting deletion.
        /// </summary>
        public string? UserId { get; }

        /// <summary>
        /// Gets whether to archive instead of hard delete.
        /// </summary>
        public bool ArchiveOnly { get; }

        /// <summary>
        /// Initializes a new instance of the DeleteSessionCommand.
        /// </summary>
        /// <param name="sessionId">Session ID to delete</param>
        /// <param name="reason">Optional reason for deletion</param>
        /// <param name="archiveOnly">Whether to archive instead of delete</param>
        public DeleteSessionCommand(string sessionId, string? reason = null, bool archiveOnly = false)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            Reason = reason;
            ArchiveOnly = archiveOnly;
        }
    }
}
