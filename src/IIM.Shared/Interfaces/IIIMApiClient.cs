using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Defines the contract for the client-side API service that communicates with the backend.
    /// </summary>
    public interface IIIMApiClient
    {
        Task<List<Workspace>> GetWorkspacesAsync(CancellationToken cancellationToken = default);

        Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

        Task<List<VirtualFile>> GetFilesAsync(Guid workspaceId, CancellationToken cancellationToken = default);

        Task<VirtualFile?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates a file upload process with the backend.
        /// </summary>
        /// <returns>
        /// A response object containing either a pre-signed URL for a new upload
        /// or details of the duplicate file if the content already exists.
        /// </returns>
        Task<InitiateFileUploadResponse> InitiateFileUploadAsync(
            Guid workspaceId,
            string path,
            string fileName,
            long fileSize,
            string fileHash,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirms with the backend that a file has been successfully uploaded to the pre-signed URL.
        /// </summary>
        /// <param name="transactionId">The unique transaction ID received from the initiate step.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The finalized VirtualFile object after confirmation.</returns>
        Task<VirtualFile?> ConfirmFileUploadAsync(string transactionId, CancellationToken cancellationToken = default);
    }
}

