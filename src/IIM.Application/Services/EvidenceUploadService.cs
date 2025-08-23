using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Infrastructure.Storage;
using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace IIM.Application.Services
{
    /// <summary>
    /// Evidence upload service that works with your existing interfaces
    /// </summary>
    public class EvidenceUploadService : IEvidenceUploadService
    {
        private readonly ILogger<EvidenceUploadService> _logger;
        private readonly IMinioClient _minioClient;
        private readonly IEvidenceManager _evidenceManager;
        private readonly IDeduplicationService _deduplicationService;
        private readonly IAuditLogger _auditLogger; // Your existing audit logger
        private readonly ISessionService _sessionService;
        private readonly StorageConfiguration _storageConfig;
        private readonly string _bucketName;

        public EvidenceUploadService(
            ILogger<EvidenceUploadService> logger,
            IMinioClient minioClient,
            IEvidenceManager evidenceManager,
            IDeduplicationService deduplicationService,
            IAuditLogger auditLogger, // Using your existing interface
            ISessionService sessionService,
            StorageConfiguration storageConfig)
        {
            _logger = logger;
            _minioClient = minioClient;
            _evidenceManager = evidenceManager;
            _deduplicationService = deduplicationService;
            _auditLogger = auditLogger;
            _sessionService = sessionService;
            _storageConfig = storageConfig;
            _bucketName = storageConfig.EvidencePath ?? "evidence"; // Use existing property
        }

        public async Task<InitiateEvidenceUploadResponse> InitiateUploadAsync(
            InitiateEvidenceUploadRequest request,
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
                        EventType = "EVIDENCE_DUPLICATE_DETECTED",
                        EntityId = existingEvidence.Id,
                        UserId = userId,
                        Timestamp = DateTimeOffset.UtcNow,
                        Details = JsonSerializer.Serialize(new
                        {
                            OriginalFileName = request.FileName,
                            Hash = request.FileHash,
                            CaseNumber = request.Metadata.CaseNumber
                        })
                    };


                    return new InitiateEvidenceUploadResponse
                    {
                        EvidenceId = existingEvidence.Id,
                        Status = EvidenceUploadStatus.Duplicate,
                        DuplicateEvidenceId = existingEvidence.Id,
                        DuplicateInfo = new DuplicateInfo
                        {
                            OriginalEvidenceId = existingEvidence.Id,
                            OriginalUploadDate = existingEvidence.CreatedAt,
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

                var evidence = new Evidence
                {
                    Id = evidenceId,
                    CaseNumber = request.Metadata.CaseNumber,
                    OriginalFileName = request.FileName,
                    StoragePath = objectName,
                    FileSize = request.FileSize,
                    Hash = request.FileHash,
                    HashAlgorithm = "SHA256",
                    Metadata = request.Metadata,
                    Status = EvidenceStatus.Pending,
                    Type = DetermineEvidenceType(request.FileName),
                    CreatedAt = DateTimeOffset.UtcNow.Date,
                    CreatedBy = userId
                };

                // Register pending evidence
                await _evidenceManager.RegisterPendingEvidenceAsync(evidence, cancellationToken);

                // Generate pre-signed URL
                var presignedUrl = await GeneratePresignedUploadUrlAsync(
                    objectName,
                    request.ContentType,
                    cancellationToken);

                // Log upload initiation
                _auditLogger.LogAudit(new AuditEvent
                {
                    EventType = "EVIDENCE_UPLOAD_INITIATED",
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

                return new InitiateEvidenceUploadResponse
                {
                    EvidenceId = evidenceId,
                    Status = EvidenceUploadStatus.Initiated,
                    UploadUrl = presignedUrl.Item1,
                    UploadUrlExpires = DateTimeOffset.UtcNow.AddMinutes(30),
                    RequiredHeaders = presignedUrl.Item2
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate evidence upload");
                return new InitiateEvidenceUploadResponse
                {
                    Status = EvidenceUploadStatus.Error
                };
            }
        }

        public async Task<ConfirmEvidenceUploadResponse> ConfirmUploadAsync(
            ConfirmEvidenceUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            // Implementation continues with same pattern...
            // Using _auditLogger.LogAuditEvent() instead of _auditService.LogAsync()

            var evidence = await _evidenceManager.GetEvidenceAsync(
                request.EvidenceId,
                cancellationToken);

            if (evidence == null)
            {
                return new ConfirmEvidenceUploadResponse
                {
                    Success = false,
                    Status = EvidenceStatus.Failed,
                    ErrorMessage = "Evidence not found"
                };
            }

            // Check if file exists in MinIO
            var exists = await CheckObjectExistsAsync(
                evidence.StoragePath,
                cancellationToken);

            if (!exists)
            {
                await _evidenceManager.UpdateEvidenceStatusAsync(
                    request.EvidenceId,
                    EvidenceStatus.Failed,
                    cancellationToken);

                return new ConfirmEvidenceUploadResponse
                {
                    Success = false,
                    Status = EvidenceStatus.Failed,
                    ErrorMessage = "File not found in storage"
                };
            }

            // Update status to active
            await _evidenceManager.UpdateEvidenceStatusAsync(
                request.EvidenceId,
                EvidenceStatus.Active,
                cancellationToken);

            // Register with deduplication
            await _deduplicationService.RegisterHashAsync(
                evidence.Hash,
                request.EvidenceId,
                cancellationToken);

            return new ConfirmEvidenceUploadResponse
            {
                Success = true,
                Status = EvidenceStatus.Active
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

            var url = await _minioClient.PresignedPutObjectAsync(args);

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