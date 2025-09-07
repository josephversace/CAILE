using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using IIM.Infrastructure.Storage;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;


namespace IIM.Infrastructure.Storage
{
    /// <summary>
    /// Evidence upload service that works with your existing interfaces
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly ILogger<FileUploadService> _logger;
        private readonly IS3StorageService _s3Client;
        private readonly IManagedFileManager _fileManager;
        private readonly IDeduplicationService _deduplicationService;
        private readonly IAuditService _auditLogger; // Your existing audit logger
        private readonly ISessionService _sessionService;
        private readonly StorageConfiguration _storageConfig;
        private readonly string _bucketName;

        public FileUploadService(
            ILogger<FileUploadService> logger,
            IS3StorageService s3Client,
            IManagedFileManager fileManager,
            IDeduplicationService deduplicationService,
            IAuditService auditLogger, // Using your existing interface
            ISessionService sessionService,
            StorageConfiguration storageConfig)
        {
            _logger = logger;
            _s3Client = s3Client;
            _fileManager = fileManager;
            _deduplicationService = deduplicationService;
            _auditLogger = auditLogger;
            _sessionService = sessionService;
            _storageConfig = storageConfig;
            _bucketName = storageConfig.EvidencePath ?? "files"; // Use existing property
        }

        public async Task<InitiateFileUploadResponse> InitiateUploadAsync(
            InitiateFileUploadRequest request,
            string userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Initiating evidence upload for file {FileName} with hash {Hash}",
                request.FileName, request.FileHash);

            try
            {
                // Check for duplicates
                var existingEvidence = await _deduplicationService.CheckDuplicateAsync(
                    request.FileHash,
                    cancellationToken);

                if (existingEvidence != null)
                {
                    // Log duplicate detection using your existing audit logger
                    var auditEvent = new AuditEvent
                    {
                        EventType = "FILE_DUPLICATE_DETECTED",
                        EntityId = existingEvidence.Id,
                        UserId = userId,
                        Timestamp = DateTimeOffset.UtcNow,
                        Details = JsonSerializer.Serialize(new
                        {
                            OriginalFileName = request.FileName,
                            Hash = request.FileHash
                        })
                    };


                    return new InitiateFileUploadResponse
                    {
                        FileId = existingEvidence.Id,
                        Status = FileUploadStatus.Duplicate,
                        DuplicateEvidenceId = existingEvidence.Id,
                        DuplicateInfo = new DuplicateInfo
                        {
                            OriginalEvidenceId = existingEvidence.Id,
                            OriginalUploadDate = existingEvidence.UpdatedAt.Value,
                            OriginalUploadedBy = existingEvidence.Metadata.CollectedBy,
                            OriginalCaseNumber = existingEvidence.CaseNumber,
                            DuplicateCount = await _deduplicationService.GetDuplicateCountAsync(
                                request.FileHash, cancellationToken)
                        }
                    };
                }

                // Create new evidence record
                var evidenceId = Guid.NewGuid().ToString("N");
                var objectName = $"{request.Metadata.CaseNumber}/{evidenceId}/{request.FileName}";

                var file = new ManagedFile
                {
                    Id = evidenceId,
                    CaseNumber = request.Metadata.CaseNumber,
                    OriginalFileName = request.FileName,
                    StoragePath = objectName,
                    FileSize = request.FileSize,
                    Hash = request.FileHash,
                    HashAlgorithm = HashType.SHA256,
                   
                    Metadata = request.Metadata,
                    Status = FileUploadStatus.Pending,
                    Type = DetermineEvidenceType(request.FileName),
                    UpdatedAt = DateTimeOffset.UtcNow.Date,
                    CreatedBy = userId
                };

                // Register pending evidence
                await _fileManager.RegisterPendingFileAsync(file, cancellationToken);

                // Generate pre-signed URL
                var presignedUrl = await GeneratePresignedUploadUrlAsync(
                    objectName,
                    request.ContentType,
                    cancellationToken);

                // Log upload initiation
                _auditLogger.LogAudit(new AuditEvent
                {
                    EventType = "FILE_UPLOAD_INITIATED",
                    EntityId = evidenceId,
                    UserId = userId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        FileName = request.FileName,
                        FileSize = request.FileSize,
                        CaseNumber = request.Metadata.CaseNumber
                    })
                });

                return new InitiateFileUploadResponse
                {
                    FileId = evidenceId,
                    Status = FileUploadStatus.Pending,
                    UploadUrl = presignedUrl.Item1,
                    UploadUrlExpires = DateTimeOffset.UtcNow.AddMinutes(30),
                    RequiredHeaders = presignedUrl.Item2
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate evidence upload");
                return new InitiateFileUploadResponse
                {
                    Status = FileUploadStatus.Failed
                };
            }
        }

        public async Task<ConfirmFileUploadResponse> ConfirmUploadAsync(
            ConfirmFileUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            // Implementation continues with same pattern...
            // Using _auditLogger.LogAuditEvent() instead of _auditService.LogAsync()

            var evidence = await _fileManager.GetFilesAsync(
                request.FileId,
                cancellationToken);

            if (evidence == null)
            {
                return new ConfirmFileUploadResponse
                {
                    Success = false,
                    Status = FileUploadStatus.Failed,
                    ErrorMessage = "File not found"
                };
            }

            // Check if file exists in MinIO
            var exists = await CheckObjectExistsAsync(
                evidence.StoragePath,
                cancellationToken);

            if (!exists)
            {
                await _fileManager.UpdateFileStatusAsync(
                    request.FileId,
                    FileUploadStatus.Failed,
                    cancellationToken);

                return new ConfirmFileUploadResponse
                {
                    Success = false,
                    Status = FileProcessingStatus.Failed,
                    ErrorMessage = "File not found in storage"
                };
            }

            // Update status to active
            await _fileManager.UpdateFileStatusAsync(
                request.FileId,
                FileProcessingStatus.Active,
                cancellationToken);

            // Register with deduplication
            await _deduplicationService.RegisterHashAsync(
                evidence.Hash,
                request.FileId,
                cancellationToken);

            return new ConfirmFileUploadResponse
            {
                Success = true,
                Status = FileProcessingStatus.Active
            };
        }

        public async Task<bool> HandleMinIOWebhookAsync(
            string bucketName,
            string objectName,
            string eventType,
            CancellationToken cancellationToken = default)
        {
            // Simple implementation for webhook handling
            _logger.LogInformation("MinIO webhook: {EventType} for {ObjectName}",
                eventType, objectName);

            return true;
        }

        private async Task<(string, Dictionary<string, string>)> GeneratePresignedUploadUrlAsync(
            string objectName,
            string contentType,
            CancellationToken cancellationToken)
        {
            var args = new PresignedPutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithExpiry(1800); // 30 minutes

            var url = await _s3Client.PresignedPutObjectAsync(args);

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = contentType
            };

            return (url, headers);
        }

        private async Task<bool> CheckObjectExistsAsync(
            string objectName,
            CancellationToken cancellationToken)
        {
            try
            {
                var args = new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName);

                // Just try to stat the object, don't create ObjectStat
                await _minioClient.StatObjectAsync(args, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private EvidenceType DetermineEvidenceType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            return extension switch
            {
                ".pdf" or ".doc" or ".docx" => EvidenceType.Document,
                ".jpg" or ".jpeg" or ".png" => EvidenceType.Image,
                ".mp4" or ".avi" or ".mov" => EvidenceType.Video,
                ".mp3" or ".wav" => EvidenceType.Audio,
                _ => EvidenceType.Other
            };
        }
    }
}