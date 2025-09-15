using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System.Threading;
using System.Threading.Tasks;


namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Service interface for managing file uploads
    /// </summary>
    public interface IManagedFileUploadService
    {
        /// <summary>
        /// Initiates evidence upload by checking for duplicates and generating pre-signed URL
        /// </summary>
        /// <param name="request">Upload request with hash and metadata</param>
        /// <param name="userId">ID of user initiating upload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response with upload URL or duplicate information</returns>
        Task<InitiateFileUploadResponse> InitiateUploadAsync(
            InitiateFileUploadRequest request,
            string userId,
            CancellationToken cancellationToken = default);

 
    }
}
