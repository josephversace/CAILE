using FluentAssertions;
using IIM.Application.Services;
using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Core.Storage;
using IIM.Infrastructure.Storage;
using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Xunit;

namespace IIM.Application.Tests.Services
{
    /// <summary>
    /// Unit tests for EvidenceUploadService
    /// </summary>
    public class EvidenceUploadServiceTests
    {
        private readonly Mock<ILogger<EvidenceUploadService>> _loggerMock;
        private readonly Mock<IMinioClient> _minioClientMock;
        private readonly Mock<IEvidenceManager> _evidenceManagerMock;
        private readonly Mock<IDeduplicationService> _deduplicationServiceMock;
        private readonly Mock<IAuditLogger> _auditServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly StorageConfiguration _storageConfig;
        private readonly EvidenceUploadService _service;

        public EvidenceUploadServiceTests()
        {
            _loggerMock = new Mock<ILogger<EvidenceUploadService>>();
            _minioClientMock = new Mock<IMinioClient>();
            _evidenceManagerMock = new Mock<IEvidenceManager>();
            _deduplicationServiceMock = new Mock<IDeduplicationService>();
            _auditServiceMock = new Mock<IAuditLogger>();
            _sessionServiceMock = new Mock<ISessionService>();
            _storageConfig = new StorageConfiguration
            {
                MinioBucketName = "test-evidence",
                VerifyHashOnUpload = true
            };

            _service = new EvidenceUploadService(
                _loggerMock.Object,
                _minioClientMock.Object,
                _evidenceManagerMock.Object,
                _deduplicationServiceMock.Object,
                _auditServiceMock.Object,
                _sessionServiceMock.Object,
                _storageConfig);
        }

        [Fact]
        public async Task InitiateUploadAsync_WhenDuplicateExists_ReturnsDuplicateInfo()
        {
            // Arrange
            var request = new InitiateEvidenceUploadRequest
            {
                FileHash = "abc123def456",
                FileName = "test.pdf",
                FileSize = 1024,
                Metadata = new EvidenceMetadata
                {
                    CaseNumber = "CASE-2024-001",
                    CollectedBy = "Agent Smith"
                }
            };

            var existingEvidence = new Evidence
            {
                Id = "existing-123",
                CaseNumber = "CASE-2024-000",
                Hash = request.FileHash,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
                Metadata = new EvidenceMetadata
                {
                    CollectedBy = "Agent Jones"
                }
            };

            _deduplicationServiceMock
                .Setup(x => x.CheckDuplicateAsync(request.FileHash, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEvidence);

            _deduplicationServiceMock
                .Setup(x => x.GetDuplicateCountAsync(request.FileHash, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            // Act
            var response = await _service.InitiateUploadAsync(request, "user123");

            // Assert
            response.Should().NotBeNull();
            response.Status.Should().Be(EvidenceUploadStatus.Duplicate);
            response.DuplicateEvidenceId.Should().Be("existing-123");
            response.DuplicateInfo.Should().NotBeNull();
            response.DuplicateInfo!.OriginalEvidenceId.Should().Be("existing-123");
            response.DuplicateInfo.DuplicateCount.Should().Be(3);
            response.UploadUrl.Should().BeNull();

            // Verify audit was logged
            _auditServiceMock.Verify(x => x.LogAudit(
                It.Is<AuditEvent>(a => a.Action == "EVIDENCE_DUPLICATE_DETECTED"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InitiateUploadAsync_WhenNewFile_ReturnsUploadUrl()
        {
            // Arrange
            var request = new InitiateEvidenceUploadRequest
            {
                FileHash = "newfile123",
                FileName = "evidence.jpg",
                FileSize = 2048,
                ContentType = "image/jpeg",
                Metadata = new EvidenceMetadata
                {
                    CaseNumber = "CASE-2024-002",
                    CollectedBy = "Agent Brown"
                }
            };

            _deduplicationServiceMock
                .Setup(x => x.CheckDuplicateAsync(request.FileHash, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Evidence?)null);

            _minioClientMock
                .Setup(x => x.PresignedPutObjectAsync(It.IsAny<PresignedPutObjectArgs>()))
                .ReturnsAsync("https://minio.local/presigned-url");

            // Act
            var response = await _service.InitiateUploadAsync(request, "user456");

            // Assert
            response.Should().NotBeNull();
            response.Status.Should().Be(EvidenceUploadStatus.Initiated);
            response.EvidenceId.Should().NotBeNullOrEmpty();
            response.UploadUrl.Should().Be("https://minio.local/presigned-url");
            response.UploadUrlExpires.Should().BeCloseTo(
                DateTimeOffset.UtcNow.AddMinutes(30),
                TimeSpan.FromSeconds(5));
            response.DuplicateInfo.Should().BeNull();

            // Verify evidence was registered as pending
            _evidenceManagerMock.Verify(x => x.RegisterPendingEvidenceAsync(
                It.Is<Evidence>(e =>
                    e.Status == EvidenceStatus.Pending &&
                    e.Hash == request.FileHash),
                It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify audit was logged
            _auditServiceMock.Verify(x => x.LogAudit(
                It.Is<AuditEvent>(a => a.Action == "EVIDENCE_UPLOAD_INITIATED"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ConfirmUploadAsync_WhenHashMatches_ReturnsSuccess()
        {
            // Arrange
            var request = new ConfirmEvidenceUploadRequest
            {
                EvidenceId = "test-evidence-123",
                ClientHash = "abc123"
            };

            var evidence = new Evidence
            {
                Id = request.EvidenceId,
                Hash = "abc123",
                StoragePath = "case/evidence/file.pdf",
                Status = EvidenceStatus.Pending,
                OriginalFileName = "file.pdf"
            };

            _evidenceManagerMock
                .Setup(x => x.GetEvidenceAsync(request.EvidenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evidence);

            // Mock object exists in MinIO
            _minioClientMock
                .Setup(x => x.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Minio.DataModel.ObjectStat());

            // Act
            var response = await _service.ConfirmUploadAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
            response.Status.Should().Be(EvidenceStatus.Active);
            response.ErrorMessage.Should().BeNull();

            // Verify status was updated
            _evidenceManagerMock.Verify(x => x.UpdateEvidenceStatusAsync(
                request.EvidenceId,
                EvidenceStatus.Active,
                It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify hash was registered
            _deduplicationServiceMock.Verify(x => x.RegisterHashAsync(
                evidence.Hash,
                request.EvidenceId,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ConfirmUploadAsync_WhenFileNotFound_ReturnsFailure()
        {
            // Arrange
            var request = new ConfirmEvidenceUploadRequest
            {
                EvidenceId = "missing-123"
            };

            var evidence = new Evidence
            {
                Id = request.EvidenceId,
                Status = EvidenceStatus.Pending,
                StoragePath = "case/missing/file.pdf"
            };

            _evidenceManagerMock
                .Setup(x => x.GetEvidenceAsync(request.EvidenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evidence);

            // Mock object doesn't exist in MinIO
            _minioClientMock
                .Setup(x => x.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Object not found"));

            // Act
            var response = await _service.ConfirmUploadAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Status.Should().Be(EvidenceStatus.Failed);
            response.ErrorMessage.Should().Contain("not found in storage");

            // Verify status was updated to failed
            _evidenceManagerMock.Verify(x => x.UpdateEvidenceStatusAsync(
                request.EvidenceId,
                EvidenceStatus.Failed,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleMinIOWebhookAsync_WhenObjectCreated_ConfirmsUpload()
        {
            // Arrange
            var bucketName = "evidence";
            var objectName = "CASE-2024-001/evidence123/document.pdf";
            var eventType = "s3:ObjectCreated:Put";

            var evidence = new Evidence
            {
                Id = "evidence123",
                Status = EvidenceStatus.Pending,
                StoragePath = objectName,
                Hash = "filehash123"
            };

            _evidenceManagerMock
                .Setup(x => x.GetEvidenceAsync("evidence123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(evidence);

            _minioClientMock
                .Setup(x => x.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Minio.DataModel.ObjectStat());

            // Act
            var result = await _service.HandleMinIOWebhookAsync(
                bucketName,
                objectName,
                eventType);

            // Assert
            result.Should().BeTrue();

            // Verify upload was confirmed
            _evidenceManagerMock.Verify(x => x.UpdateEvidenceStatusAsync(
                "evidence123",
                EvidenceStatus.Active,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}