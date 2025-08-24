using IIM.Shared.Models;
using System.Threading;
using System.Threading.Tasks;


namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Service interface for managing evidence uploads with MinIO
    /// </summary>
    public interface IEvidenceUploadService
    {
        /// <summary>
        /// Initiates evidence upload by checking for duplicates and generating pre-signed URL
        /// </summary>
        /// <param name="request">Upload request with hash and metadata</param>
        /// <param name="userId">ID of user initiating upload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response with upload URL or duplicate information</returns>
        Task<InitiateEvidenceUploadResponse> InitiateUploadAsync(
            InitiateEvidenceUploadRequest request,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirms upload completion and triggers verification
        /// </summary>
        /// <param name="request">Confirmation request with evidence ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response with verification results</returns>
        Task<ConfirmEvidenceUploadResponse> ConfirmUploadAsync(
            ConfirmEvidenceUploadRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles MinIO webhook for upload completion (alternative to client confirmation)
        /// </summary>
        /// <param name="bucketName">MinIO bucket name</param>
        /// <param name="objectName">Object key in bucket</param>
        /// <param name="eventType">MinIO event type</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success indicator</returns>
        Task<bool> HandleMinIOWebhookAsync(
            string bucketName,
            string objectName,
            string eventType,
            CancellationToken cancellationToken = default);
    }
}
