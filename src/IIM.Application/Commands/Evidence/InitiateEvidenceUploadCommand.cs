using IIM.Core.Mediator;
using IIM.Shared.Models;
using Mediator;

namespace IIM.Application.Commands.Evidence
{
    /// <summary>
    /// Command to initiate evidence upload with deduplication check
    /// </summary>
    public class InitiateEvidenceUploadCommand : IRequest<InitiateEvidenceUploadResponse>, IAuditableCommand
    {
        /// <summary>
        /// SHA-256 hash of the file
        /// </summary>
        public string FileHash { get; set; } = string.Empty;

        /// <summary>
        /// Original filename
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// MIME type
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Evidence metadata
        /// </summary>
        public EvidenceMetadata Metadata { get; set; } = new();

        /// <summary>
        /// User initiating the upload
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        // IAuditableCommand implementation
        public string? SessionId => Metadata.SessionId;
        public string? CaseNumber => Metadata.CaseNumber;
    }
}
