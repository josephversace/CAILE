using IIM.Core.Mediator;
using IIM.Shared.Models;
using Mediator;

namespace IIM.Application.Evidence
{
    /// <summary>
    /// Command to initiate evidence upload with deduplication check
    /// </summary>
    public class InitiateFileUploadCommand : IRequest<InitiateFileUploadResponse>, IAuditableCommand
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
        public FileMetadata Metadata { get; set; } = new();

        /// <summary>
        /// User initiating the upload
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        // IAuditableCommand implementation

    }
}
