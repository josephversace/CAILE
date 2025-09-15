using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Storage
{
    /// <summary>
    /// Orchestrates the lifecycle of files, delegating storage and metadata operations to specialized providers.
    /// This class implements the core business logic for file ingestion, processing, and integrity verification.
    /// </summary>
    public class FileManager : IManagedFileManager
    {
        private readonly ILogger<FileManager> _logger;
        private readonly IWorkspaceProvider _workspaceProvider;
        private readonly IObjectStorageProvider _storageProvider;
        private readonly IDeduplicationService _deduplicationService;

        // This should be driven by configuration
        private const string EvidenceBucketName = "evidence";

        public FileManager(
            ILogger<FileManager> logger,
            IWorkspaceProvider workspaceProvider,
            IObjectStorageProvider storageProvider,
            IDeduplicationService deduplicationService)
        {
            _logger = logger;
            _workspaceProvider = workspaceProvider;
            _storageProvider = storageProvider;
            _deduplicationService = deduplicationService;
        }

        public async Task<VirtualFile> IngestFileAsync(Stream stream, VirtualFile virtualFile, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Ingesting file {FileName} for workspace {WorkspaceId}", virtualFile.FileName, virtualFile.WorkspaceId);

            if (stream == null || stream.Length == 0)
            {
                throw new ArgumentException("Input stream cannot be null or empty.", nameof(stream));
            }

            try
            {
                var hashes = await _deduplicationService.ComputeHashesAsync(stream, cancellationToken);
                var primaryHash = hashes["SHA256"];
                virtualFile.StoredFileHash = primaryHash;

                var existingStoredFile = await _workspaceProvider.GetStoredFileByHashAsync(primaryHash, cancellationToken);

                if (existingStoredFile == null)
                {
                    _logger.LogInformation("New content detected. Uploading to storage with key {Hash}", primaryHash);
                    stream.Position = 0;
                    await _storageProvider.PutObjectAsync(EvidenceBucketName, primaryHash, stream, cancellationToken);

                    var storedFile = new StoredFile
                    {
                        Hash = primaryHash,
                        FileSize = stream.Length,
                        MimeType = "application/octet-stream" // Placeholder
                        // ClassificationTags would be added here in a separate enrichment step
                    };
                    await _workspaceProvider.CreateStoredFileAsync(storedFile, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Duplicate content detected for hash {Hash}. Linking to existing stored file.", primaryHash);
                }

                virtualFile.Id = Guid.NewGuid();
                virtualFile.ChainOfCustody.Add(new ChainOfCustodyEntry
                {
                    Action = "INGESTED",
                    Actor = virtualFile.CreatedBy,
                    Timestamp = DateTimeOffset.UtcNow,
                    Details = $"File '{virtualFile.FileName}' ingested by user '{virtualFile.CreatedBy}'."
                });

                var createdVirtualFile = await _workspaceProvider.CreateVirtualFileAsync(virtualFile, cancellationToken);

                _logger.LogInformation("File ingested successfully. VirtualFileId: {VirtualFileId}", createdVirtualFile.Id);
                return createdVirtualFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest file {FileName}", virtualFile.FileName);
                throw;
            }
        }

        public async Task<VirtualFile> ProcessFileAsync(Guid virtualFileId, Func<Stream, Task<Stream>> processor, string processingType, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing file {OriginalFileId} with process '{ProcessingType}'", virtualFileId, processingType);

            var originalFile = await _workspaceProvider.GetVirtualFileByIdAsync(virtualFileId, cancellationToken);
            if (originalFile == null)
            {
                throw new FileNotFoundException($"Virtual file with ID '{virtualFileId}' not found.");
            }

            using var originalStream = await _storageProvider.GetObjectAsync(EvidenceBucketName, originalFile.StoredFileHash, cancellationToken);
            using var processedStream = await processor(originalStream);

            var newVirtualFile = new VirtualFile
            {
                WorkspaceId = originalFile.WorkspaceId,
                FileName = $"{Path.GetFileNameWithoutExtension(originalFile.FileName)}_{processingType}{Path.GetExtension(originalFile.FileName)}",
                Path = originalFile.Path,
                Status = FileUploadStatus.Completed,
                CreatedBy = "System", // Or resolve current user context
                CollectedBy = "System",
                CollectionDate = DateTimeOffset.UtcNow,
                CollectedLocation = "Processed In-System"
            };

            // Ingest the processed content. This handles deduplication of the output automatically.
            var createdProcessedFile = await IngestFileAsync(processedStream, newVirtualFile, cancellationToken);

            originalFile.ProcessedVersions.Add(new ProcessedFile
            {
                Id = Guid.NewGuid(),
                OriginalVirtualFileId = originalFile.Id,
                ProcessedVirtualFileId = createdProcessedFile.Id,
                ProcessingType = processingType,
                ProcessedAt = DateTimeOffset.UtcNow,
                ProcessedBy = "System"
            });
            await _workspaceProvider.UpdateVirtualFileAsync(originalFile, cancellationToken);

            return createdProcessedFile;
        }

        public async Task<bool> VerifyIntegrityAsync(Guid virtualFileId, CancellationToken cancellationToken = default)
        {
            var virtualFile = await _workspaceProvider.GetVirtualFileByIdAsync(virtualFileId, cancellationToken);
            if (virtualFile == null)
            {
                _logger.LogWarning("VerifyIntegrityAsync: VirtualFile with ID {VirtualFileId} not found.", virtualFileId);
                return false;
            }

            try
            {
                using var stream = await _storageProvider.GetObjectAsync(EvidenceBucketName, virtualFile.StoredFileHash, cancellationToken);
                var currentHashes = await _deduplicationService.ComputeHashesAsync(stream, cancellationToken);

                var storedHash = virtualFile.StoredFileHash;
                var currentHash = currentHashes.GetValueOrDefault("SHA256");

                bool isValid = storedHash.Equals(currentHash, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogError("Integrity check FAILED for VirtualFile {VirtualFileId}. Stored Hash: {StoredHash}, Recalculated Hash: {CurrentHash}",
                        virtualFileId, storedHash, currentHash);
                }
                else
                {
                    _logger.LogInformation("Integrity check PASSED for VirtualFile {VirtualFileId}", virtualFileId);
                }
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during integrity check for VirtualFile {VirtualFileId}", virtualFileId);
                return false;
            }
        }
    }
}

