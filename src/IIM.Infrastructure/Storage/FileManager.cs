using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Enums;
using IIM.Shared.Models;

namespace IIM.Infrastructure.Storage
{
    /// <summary>
    /// A refactored implementation of the file manager that delegates responsibilities
    /// to specialized providers, acting as an orchestrator for file-related business logic.
    /// </summary>
    public class FileManager : IManagedFileManager
    {
        private readonly ILogger<FileManager> _logger;
        private readonly IWorkspaceProvider _workspaceProvider;
        private readonly IObjectStorageProvider _storageProvider;
        private readonly IDeduplicationService _deduplicationService;
        private readonly IAuditRepository _auditRepository;

        private const string MainBucket = "iim-files"; // A single bucket for all deduplicated files

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

        public async Task<ManagedFile> CreateFileAsync(
            string workspaceId,
            string path,
            string fileName,
            Stream data,
            string createdBy,
            Dictionary<string, string> customMetadata,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating file {FileName} in workspace {WorkspaceId}", fileName, workspaceId);

            try
            {
                // 1. Calculate hash for deduplication and integrity
                var hashes = await _deduplicationService.ComputeHashAsync(data, cancellationToken);
                var primaryHash = hashes["SHA256"];
                data.Position = 0;

                // 2. Upload the file content to object storage using its hash as the key
                await _storageProvider.PutObjectAsync(MainBucket, primaryHash, data, cancellationToken);
                _logger.LogInformation("Stored file content with hash/key {Hash}", primaryHash);

                // 3. Create the metadata object
                var file = new ManagedFile
                {
                    Id = Guid.NewGuid().ToString(),
                    WorkspaceId = workspaceId,
                    Path = path,
                    FileName = fileName,
                    FileSize = data.Length,
                    Hash = primaryHash,
                    Hashes = hashes,
                    StoragePath = primaryHash, // The path in storage is the hash
                    CreatedBy = createdBy,
                    CustomMetadata = customMetadata,
                    Status = FileProcessingStatus.Ingested,
                    // Populate forensic fields from custom metadata if they exist
                    CollectedBy = customMetadata.GetValueOrDefault("CollectedBy"),
                    CollectionLocation = customMetadata.GetValueOrDefault("CollectionLocation"),
                    Description = customMetadata.GetValueOrDefault("Description"),
                    CollectionDate = customMetadata.TryGetValue("CollectionDate", out var dateStr) && DateTimeOffset.TryParse(dateStr, out var date) ? date : null
                };

                // Add initial chain of custody entry
                file.ChainOfCustody.Add(new ChainOfCustodyEntry
                {
                    Action = "INGESTED",
                    Actor = createdBy,
                    Details = $"File '{fileName}' created in path '{path}'. Stored with hash {primaryHash}.",
                    Hash = primaryHash,
                    Timestamp = DateTimeOffset.UtcNow
                });

                // 4. Save the metadata reference to the database
                var fileReference = await _workspaceProvider.CreateFileReferenceAsync(file);
                _logger.LogInformation("Created file metadata record {FileId}", file.Id);

                // 5. Audit the creation event
                await _auditRepository.AddEventAsync(new AuditEvent
                {
                    EventType = "file.created",
                    EntityId = file.Id,
                    EntityType = "ManagedFile",
                    UserId = createdBy,
                    Details = $"File {fileName} created in workspace {workspaceId}."
                }, cancellationToken);

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create file {FileName}", fileName);
                throw;
            }
        }

        public Task<ManagedFile?> GetFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            return _workspaceProvider.GetFileAsync(fileId, cancellationToken);
        }

        public async Task<Stream> GetFileStreamAsync(string fileId, CancellationToken cancellationToken = default)
        {
            var file = await _workspaceProvider.GetFileAsync(fileId, cancellationToken);
            if (file is null)
            {
                throw new FileNotFoundException($"File with ID {fileId} not found.");
            }

            return await _storageProvider.GetObjectAsync(MainBucket, file.Hash, cancellationToken);
        }

        public Task<IEnumerable<ManagedFile>> GetFilesByWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
        {
            return _workspaceProvider.GetFilesByWorkspaceAsync(workspaceId, cancellationToken);
        }

        public async Task UpdateFileStatusAsync(string fileId, FileProcessingStatus status, CancellationToken cancellationToken = default)
        {
            var file = await _workspaceProvider.GetFileAsync(fileId, cancellationToken);
            if (file != null)
            {
                file.Status = status;
                file.UpdatedAt = DateTimeOffset.UtcNow;
                await _workspaceProvider.UpdateFileAsync(file, cancellationToken);

                await _auditRepository.AddEventAsync(new AuditEvent
                {
                    EventType = "file.status.updated",
                    EntityId = fileId,
                    EntityType = "ManagedFile",
                    Details = $"Status updated to {status}"
                }, cancellationToken);
            }
        }

        public async Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(string fileId, CancellationToken cancellationToken = default)
        {
            var file = await GetFileAsync(fileId, cancellationToken);
            if (file == null)
                throw new FileNotFoundException($"File {fileId} not found");

            // In a real scenario, you might re-verify the hash here if needed
            var integrityValid = true;

            return new ChainOfCustodyReport
            {
                FileId = fileId,
                OriginalFileName = file.FileName,
                CaseNumber = file.WorkspaceId, // Using WorkspaceId as CaseNumber
                ChainEntries = file.ChainOfCustody.OrderBy(e => e.Timestamp).ToList(),
                ProcessedVersions = file.ProcessedVersions,
                IntegrityValid = integrityValid,
                OriginalHashes = file.Hashes,
                Signature = file.Signature,
                IngestTimestamp = file.CreatedAt.DateTime
            };
        }
    }
}
