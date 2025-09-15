using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Storage
{
    /// <summary>
    /// A transitional implementation of IManagedFileManager.
    /// This class orchestrates file operations by delegating work to specialized providers.
    /// Its role is to bridge legacy application logic with the new agnostic data architecture.
    /// </summary>
    public class FileManager : IManagedFileManager
    {
        private readonly ILogger<FileManager> _logger;
        private readonly IWorkspaceProvider _workspaceProvider;
        private readonly IObjectStorageProvider _storageProvider;
        private readonly IDeduplicationService _deduplicationService;
        private readonly IAuditRepository _auditRepository;

        public FileManager(
            ILogger<FileManager> logger,
            IWorkspaceProvider workspaceProvider,
            IObjectStorageProvider storageProvider,
            IDeduplicationService deduplicationService,
            IAuditRepository auditRepository)
        {
            _logger = logger;
            _workspaceProvider = workspaceProvider;
            _storageProvider = storageProvider;
            _deduplicationService = deduplicationService;
            _auditRepository = auditRepository;
        }

        public async Task<VirtualFile> IngestFileAsync(Stream stream, VirtualFile virtualFile, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Ingesting file {FileName} for workspace {WorkspaceId}", virtualFile.FileName, virtualFile.WorkspaceId);

            try
            {
                // 1. Calculate hash for deduplication and integrity
                var hashes = await _deduplicationService.ComputeHashAsync(stream, cancellationToken);
                var primaryHash = hashes["SHA256"];
                virtualFile.StoredFileHash = primaryHash;

                // 2. Check if the content (StoredFile) already exists
                if (!await _workspaceProvider.StoredFileExistsAsync(primaryHash, cancellationToken))
                {
                    _logger.LogInformation("New file content detected. Uploading to storage with key {Hash}", primaryHash);
                    // 2a. If not, upload the new content to object storage
                    stream.Position = 0;
                    await _storageProvider.PutObjectAsync("evidence", primaryHash, stream, cancellationToken);

                    // 2b. Create the StoredFile record
                    var storedFile = new StoredFile
                    {
                        Hash = primaryHash,
                        FileSize = stream.Length,
                        MimeType = "application/octet-stream" // This should be determined more accurately
                        // ClassificationTags would be added here after AI enrichment step
                    };
                    await _workspaceProvider.CreateStoredFileAsync(storedFile, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("File content is a duplicate of {Hash}. Linking to existing stored file.", primaryHash);
                }

                // 3. Create the VirtualFile record
                virtualFile.Id = Guid.NewGuid();
                // ... other properties of virtualFile are pre-populated ...
                var createdVirtualFile = await _workspaceProvider.CreateVirtualFileAsync(virtualFile);

                // 4. Add initial chain of custody entry
                createdVirtualFile.ChainOfCustody.Add(new ChainOfCustodyEntry
                {
                    Action = "INGESTED",
                    Actor = virtualFile.CreatedBy,
                    Timestamp = DateTimeOffset.UtcNow,
                    Details = $"File '{virtualFile.FileName}' ingested into workspace '{virtualFile.WorkspaceId}'."
                });
                await _workspaceProvider.UpdateVirtualFileAsync(createdVirtualFile, cancellationToken);

                _logger.LogInformation("File ingested successfully. VirtualFileId: {VirtualFileId}", createdVirtualFile.Id);
                return createdVirtualFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest file {FileName}", virtualFile.FileName);
                throw;
            }
        }

        public Task<VirtualFile?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
        {
            return _workspaceProvider.GetVirtualFileByIdAsync(fileId, cancellationToken);
        }

        public Task<IEnumerable<VirtualFile>> GetFilesByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return _workspaceProvider.GetVirtualFilesByWorkspaceAsync(workspaceId, cancellationToken);
        }

        public async Task UpdateFileStatusAsync(Guid fileId, FileUploadStatus status, CancellationToken cancellationToken = default)
        {
            var file = await _workspaceProvider.GetVirtualFileByIdAsync(fileId, cancellationToken);
            if (file != null)
            {
                file.Status = status;
                file.UpdatedAt = DateTimeOffset.UtcNow;
                await _workspaceProvider.UpdateVirtualFileAsync(file, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Could not find file with ID {FileId} to update status.", fileId);
            }
        }

        public async Task<VirtualFile> ProcessFileAsync(Guid originalFileId, string processingType, Func<Stream, Task<Stream>> processor, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing file {OriginalFileId} with process '{ProcessingType}'", originalFileId, processingType);

            var originalVirtualFile = await _workspaceProvider.GetVirtualFileByIdAsync(originalFileId, cancellationToken);
            if (originalVirtualFile == null)
            {
                throw new FileNotFoundException($"Original file with ID '{originalFileId}' not found.");
            }

            // Get the original file's content from storage
            using var originalStream = await _storageProvider.GetObjectAsync("evidence", originalVirtualFile.StoredFileHash, cancellationToken);

            // Process the stream
            using var processedStream = await processor(originalStream);

            // Create a new virtual file for the processed version
            var processedVirtualFile = new VirtualFile
            {
                WorkspaceId = originalVirtualFile.WorkspaceId,
                FileName = $"{Path.GetFileNameWithoutExtension(originalVirtualFile.FileName)}_{processingType}{Path.GetExtension(originalVirtualFile.FileName)}",
                Path = originalVirtualFile.Path,
                Status = FileUploadStatus.Completed,
                CreatedBy = "System", // Or get current user
                CollectedBy = "System",
                CollectionDate = DateTimeOffset.UtcNow,
                CollectedLocation = "Processed in-system"
            };

            // Ingest the processed stream as a new file (this will handle deduplication of the processed output)
            var newVirtualFile = await IngestFileAsync(processedStream, processedVirtualFile, cancellationToken);

            // Add a chain of custody entry to the original file to note the processing
            originalVirtualFile.ChainOfCustody.Add(new ChainOfCustodyEntry
            {
                Action = $"PROCESSED_AS_{processingType.ToUpper()}",
                Actor = "System",
                Details = $"Created new version: {newVirtualFile.Id}",
                Timestamp = DateTimeOffset.UtcNow
            });
            await _workspaceProvider.UpdateVirtualFileAsync(originalVirtualFile, cancellationToken);

            return newVirtualFile;
        }

        public Task ExportFileAsync(Guid fileId, string exportPath, CancellationToken cancellationToken = default)
        {
            // This logic will require significant changes to accommodate the new model.
            // For now, it's a placeholder.
            _logger.LogWarning("ExportFileAsync is not fully implemented in the new architecture yet.");
            return Task.CompletedTask;
        }
    }
}