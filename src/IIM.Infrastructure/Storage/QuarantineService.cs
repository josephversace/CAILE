using IIM.Shared.Interfaces;
using System;
using System.Threading.Tasks;

namespace IIM.Application.Services
{
    /// <summary>
    /// Manages the business logic for the file quarantine process.
    /// This service orchestrates interactions between storage, metadata, and auditing.
    /// </summary>
    public class QuarantineService
    {
        private readonly IObjectStorageProvider _storageProvider;
        private readonly IAuditRepository _auditRepository;
        private readonly IWorkspaceProvider _workspaceProvider;

        private const string QuarantineBucket = "iim-quarantine";
        private const string EvidenceBucket = "iim-evidence"; // Your main, trusted bucket

        public QuarantineService(
            IObjectStorageProvider storageProvider,
            IAuditRepository auditRepository,
            IWorkspaceProvider workspaceProvider)
        {
            _storageProvider = storageProvider;
            _auditRepository = auditRepository;
            _workspaceProvider = workspaceProvider;
        }

        /// <summary>
        /// Generates a pre-signed URL for a client to upload a file directly to the quarantine area.
        /// </summary>
        public async Task<string> GenerateUploadLinkAsync(string workspaceId, string originalFileName, string userId)
        {
            var objectKey = $"{workspaceId}/{Guid.NewGuid()}_{originalFileName}";
            var url = await _storageProvider.GetPresignedUploadUrlAsync(
                QuarantineBucket,
                objectKey,
                TimeSpan.FromMinutes(30)
            );

            // It is important to audit the generation of the link itself.
            await _auditRepository.AddEventAsync("quarantine.upload.initiated", workspaceId, originalFileName, userId, "-", $"Generated presigned URL for key: {objectKey}");

            return url;
        }

        /// <summary>
        /// Promotes a file from quarantine to the main evidence bucket. This involves copying the object
        /// and then creating a permanent metadata record for it in the workspace.
        /// </summary>
        public async Task PromoteFromQuarantineAsync(string sourceObjectKey, string newPath, string newFileName, long fileSize, string approverId)
        {
            var destObjectKey = $"{newPath}/{Guid.NewGuid()}_{newFileName}";

            // 1. Copy the file from the quarantine bucket to the final evidence bucket.
            await _storageProvider.CopyObjectAsync(QuarantineBucket, sourceObjectKey, EvidenceBucket, destObjectKey);

            // 2. Create the official file reference in our database.
            await _workspaceProvider.CreateFileReferenceAsync(newPath, newFileName, fileSize, destObjectKey);

            // 3. Delete the original file from quarantine.
            await _storageProvider.DeleteObjectAsync(QuarantineBucket, sourceObjectKey);

            // 4. Log this important event.
            await _auditRepository.AddEventAsync("quarantine.promote", newPath, newFileName, approverId, "-", $"Promoted from {sourceObjectKey} to {destObjectKey}");
        }

        /// <summary>
        /// Rejects a file and deletes it from the quarantine bucket.
        /// </summary>
        public async Task RejectFromQuarantineAsync(string objectKey, string reviewerId, string reason)
        {
            await _storageProvider.DeleteObjectAsync(QuarantineBucket, objectKey);

            await _auditRepository.AddEventAsync("quarantine.reject", "-", objectKey, reviewerId, "-", $"Reason: {reason}");
        }
    }
}
