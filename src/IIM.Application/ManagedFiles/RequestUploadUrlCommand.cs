// IIM.Application/ManagedFiles/RequestUploadUrlCommand.cs
using IIM.Shared.Mediator;
using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.ManagedFiles
{
    public class RequestUploadUrlCommand : IRequest<RequestUploadUrlResult>  // IRequest not ICommand
    {
        public Guid WorkspaceId { get; init; }
        public string FileName { get; init; }
        public string FileHash { get; init; }
        public long FileSize { get; init; }
        public bool RequiresQuarantine { get; init; } = true;
    }

    public class RequestUploadUrlResult
    {
        public bool IsDuplicate { get; init; }
        public Guid? ExistingFileId { get; init; }
        public string UploadUrl { get; init; }
        public string Bucket { get; init; }
        public string ObjectKey { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public string Message { get; init; }
    }

    public class RequestUploadUrlCommandHandler : IRequestHandler<RequestUploadUrlCommand, RequestUploadUrlResult>
    {
        private readonly IWorkspaceProvider _workspaceProvider;
        private readonly IObjectStorageProvider _storageProvider;
        private readonly IDeduplicationService _dedupService;
        private readonly ILogger<RequestUploadUrlCommandHandler> _logger;

        public RequestUploadUrlCommandHandler(
            IWorkspaceProvider workspaceProvider,
            IObjectStorageProvider storageProvider,
            IDeduplicationService dedupService,
            ILogger<RequestUploadUrlCommandHandler> logger)
        {
            _workspaceProvider = workspaceProvider;
            _storageProvider = storageProvider;
            _dedupService = dedupService;
            _logger = logger;
        }

        public async Task<RequestUploadUrlResult> Handle(RequestUploadUrlCommand request, CancellationToken cancellationToken)
        {
            // Implementation as before, but using existing interfaces
            var bucket = request.RequiresQuarantine ? "quarantine" : "primary";

            // Check for existing file with same hash
            var existingFile = await _workspaceProvider.GetStoredFileByHashAsync(request.FileHash, cancellationToken);

            if (existingFile != null && !bucket.Contains("quarantine"))
            {
                // Create virtual file linked to existing
                var virtualFile = new VirtualFile
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = request.WorkspaceId,
                    FileName = request.FileName,
                    FileSize = request.FileSize,
                    StoredFileHash = request.FileHash,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _workspaceProvider.CreateVirtualFileAsync(virtualFile, cancellationToken);

                return new RequestUploadUrlResult
                {
                    IsDuplicate = true,
                    ExistingFileId = virtualFile.Id,
                    Message = "File already exists"
                };
            }

            // Generate presigned URL
            var objectKey = $"{request.WorkspaceId}/{Guid.NewGuid()}_{request.FileName}";
            var presignedUrl = await _storageProvider.GetPresignedUploadUrlAsync(
                bucket,
                objectKey,
                TimeSpan.FromHours(1));

            return new RequestUploadUrlResult
            {
                IsDuplicate = false,
                UploadUrl = presignedUrl,
                Bucket = bucket,
                ObjectKey = objectKey,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Message = "Upload URL generated"
            };
        }
    }
}