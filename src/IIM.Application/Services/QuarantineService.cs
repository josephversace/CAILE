using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Services
{
    public class QuarantineService
    {
        private readonly IObjectStorageProvider _storageProvider;
        private readonly IAuditRepository _auditRepository;
        private readonly IWorkspaceProvider _workspaceProvider;
        private const string QuarantineBucket = "iim-quarantine";
        private const string EvidenceBucket = "evidence"; // This should come from configuration

        public QuarantineService(
            IObjectStorageProvider storageProvider,
            IAuditRepository auditRepository,
            IWorkspaceProvider workspaceProvider)
        {
            _storageProvider = storageProvider;
            _auditRepository = auditRepository;
            _workspaceProvider = workspaceProvider;
        }

        public Task<string> GenerateUploadLinkAsync(Guid workspaceId, string originalFileName, CancellationToken cancellationToken = default)
        {
            var objectKey = $"{workspaceId}/{Guid.NewGuid()}_{originalFileName}";
            return _storageProvider.GetPresignedUploadUrlAsync(QuarantineBucket, objectKey, TimeSpan.FromMinutes(30));
        }

        public async Task PromoteFromQuarantineAsync(string sourceKey, VirtualFile virtualFile, CancellationToken cancellationToken = default)
        {
            var destKey = virtualFile.StoredFileHash;

            // 1. Copy the object from quarantine to the permanent evidence bucket
            await _storageProvider.CopyObjectAsync(QuarantineBucket, sourceKey, EvidenceBucket, destKey);

            // 2. Create the permanent virtual file record in the database
            await _workspaceProvider.CreateVirtualFileAsync(virtualFile, cancellationToken);

            // 3. Delete the temporary file from quarantine
            await _storageProvider.DeleteObjectAsync(QuarantineBucket, sourceKey);

            // 4. Log the audit event
            var auditEvent = new AuditEvent
            {
                EventType = "FILE_PROMOTED",
                EntityId = virtualFile.Id.ToString(),
                EntityType = "VirtualFile",
                UserId = virtualFile.CreatedBy,
                Details = $"File '{virtualFile.FileName}' promoted from quarantine for workspace {virtualFile.WorkspaceId}."
            };
            await _auditRepository.AddEventAsync(auditEvent, cancellationToken);
        }
    }
}

