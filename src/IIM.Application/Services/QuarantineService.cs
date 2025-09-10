using IIM.Shared.Interfaces;
using IIM.Shared.Models; // Assuming AuditEvent is here
using System;
using System.Threading.Tasks;

namespace IIM.Application.Services
{
    /// <summary>
    /// Manages the business logic for the file quarantine process.
    /// This service orchestrates interactions between storage, metadata, and auditing,
    /// acting as a high-level workflow coordinator.
    /// </summary>
    public class QuarantineService
    {
        private readonly IObjectStorageProvider _storageProvider;
        private readonly IAuditRepository _auditRepository;
        private readonly IWorkspaceProvider _workspaceProvider;

        // Bucket names should be read from configuration, but are hardcoded here for simplicity.
        private const string QuarantineBucket = "iim-quarantine";
        private const string EvidenceBucket = "iim-evidence";

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
        /// <param name="workspaceId">The identifier for the workspace where the upload is initiated.</param>
        /// <param name="fileName">The original name of the file being uploaded.</param>
        /// <param name="userId">The ID of the user performing the upload.</param>
        /// <returns>A pre-signed URL for the client to use for a PUT request.</returns>
        public async Task<string> GenerateUploadLinkAsync(string workspaceId, string fileName, string userId)
        {
            var objectKey = $"{workspaceId}/{Guid.NewGuid()}-{fileName}";
            var url = await _storageProvider.GetPresignedUploadUrlAsync(
                QuarantineBucket,
                objectKey,
                TimeSpan.FromMinutes(30)
            );

            var auditEvent = new AuditEvent
            {
                EventType = "quarantine.upload.initiated",
                UserId = userId,
                EntityType = "File",
                EntityId = fileName, // Using filename as the temporary ID
                Details = $"Generated presigned URL for quarantine key: {objectKey}"
            };
            await _auditRepository.AddEventAsync(auditEvent);

            return url;
        }

        /// <summary>
        /// Promotes a file from quarantine to the main evidence bucket.
        /// This creates the permanent metadata record and moves the file to its final storage location.
        /// </summary>
        /// <param name="quarantineKey">The object key of the file in the quarantine bucket.</param>
        /// <param name="targetPath">The virtual path where the file will reside in the workspace.</param>
        /// <param name="targetFileName">The final name for the file.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="fileHash">The SHA256 hash of the file, used as the final, deduplicated storage key.</param>
        /// <param name="approverId">The ID of the user approving the promotion.</param>
        public async Task PromoteFromQuarantineAsync(string quarantineKey, string targetPath, string targetFileName, long fileSize, string fileHash, string approverId)
        {
            // The file hash is the new storage key, enabling content-based deduplication.
            var finalStorageKey = fileHash;

            // 1. Copy the file from the quarantine bucket to the permanent evidence bucket.
            await _storageProvider.CopyObjectAsync(QuarantineBucket, quarantineKey, EvidenceBucket, finalStorageKey);

            // 2. Create the official file reference in our database, linking the virtual path to the storage key.
            var fileReference = await _workspaceProvider.CreateFileReferenceAsync(targetPath, targetFileName, fileSize, finalStorageKey);

            // 3. Delete the original file from quarantine now that it's safely stored and referenced.
            await _storageProvider.DeleteObjectAsync(QuarantineBucket, quarantineKey);

            // 4. Log this important event.
            var auditEvent = new AuditEvent
            {
                EventType = "quarantine.promote",
                UserId = approverId,
                EntityType = "File",
                EntityId = fileReference.Id, // Use the new permanent ID
                Details = $"Promoted quarantine key {quarantineKey} to permanent storage key {finalStorageKey} at path {targetPath}/{targetFileName}"
            };
            await _auditRepository.AddEventAsync(auditEvent);
        }

        /// <summary>
        /// Rejects a file and deletes it permanently from the quarantine bucket.
        /// </summary>
        /// <param name="quarantineKey">The object key of the file to be deleted.</param>
        /// <param name="reviewerId">The ID of the user rejecting the file.</param>
        /// <param name="reason">The reason for the rejection.</param>
        public async Task RejectFromQuarantineAsync(string quarantineKey, string reviewerId, string reason)
        {
            await _storageProvider.DeleteObjectAsync(QuarantineBucket, quarantineKey);

            var auditEvent = new AuditEvent
            {
                EventType = "quarantine.reject",
                UserId = reviewerId,
                EntityType = "File",
                EntityId = quarantineKey,
                Details = $"Rejected and deleted file from quarantine. Reason: {reason}"
            };
            await _auditRepository.AddEventAsync(auditEvent);
        }
    }
}

