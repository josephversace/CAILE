using IIM.Core.Mediator;
using IIM.Shared.Models;
using Mediator;

namespace IIM.Application.Files
{
    /// <summary>
    /// Command to confirm file upload completion
    /// </summary>
    public class ConfirmFileUploadCommand : IRequest<ConfirmFileUploadResponse>, IAuditableCommand
    {
        /// <summary>
        /// File ID to confirm
        /// </summary>
        public string FileId { get; set; } = string.Empty;

      

        /// <summary>
        /// ETag from MinIO
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// Client hash for verification
        /// </summary>
        public string? ClientHash { get; set; }

        // IAuditableCommand implementation
        public string? SessionId { get; set; }
        public string? CaseNumber { get; set; }
    }
}