using IIM.Infrastructure.Data;
using Minio;
using Minio.DataModel.Args;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace ForensicIngestDemo
{
  
    // The Quarantine Service
    public class QuarantineService
    {
        private readonly MinioClient _minio;
        private readonly EfAuditRepository _auditLogger;
        private readonly EfEvidenceRepositoy _evidenceRepo;

        private const string QuarantineBucket = "iim-quarantine";
        private const string EvidenceBucket = "evidence-bucket"; // Replace with your real bucket

        public QuarantineService(MinioClient minio, EfAuditRepository auditLogger, EfEvidenceRepositoy evidenceRepo)
        {
            _minio = minio;
            _auditLogger = auditLogger;
            _evidenceRepo = evidenceRepo;
        }

        // Generate a presigned upload link for quarantine
        public async Task<string> GenerateUploadLinkAsync(string caseId, string originalFileName, string userId)
        {
            var objectKey = $"{caseId}/{Guid.NewGuid()}_{originalFileName}";
            var url = await _minio.PresignedPutObjectAsync(
                QuarantineBucket,
                objectKey,
                60 * 30 // 30 min validity
            );
            await _auditLogger.LogAsync("quarantine.presign", caseId, originalFileName, userId, "-", $"key={objectKey}");
            return url;
        }

        // Promote a file from quarantine to evidence bucket (copy+delete)
        public async Task PromoteFromQuarantineAsync(string caseId, string sha256Hash, string originalFileName, string classification, string approverId)
        {
            var srcKey = $"{caseId}/{sha256Hash}/{originalFileName}";
            var destKey = $"{classification}/{caseId}/{sha256Hash}/{originalFileName}";

            // Copy from quarantine to evidence
            await _minio.CopyObjectAsync(new CopyObjectArgs()
                .WithBucket(EvidenceBucket)
                .WithObject(destKey)
                .WithCopySource($"{QuarantineBucket}/{srcKey}"));

            // Remove from quarantine
            await _minio.RemoveObjectAsync(QuarantineBucket, srcKey);

            // Register evidence
            await _evidenceRepo.RegisterEvidenceAsync(sha256Hash, caseId, originalFileName, classification);

            // Audit
            await _auditLogger.LogAsync("quarantine.promote", caseId, originalFileName, approverId, sha256Hash);
        }

        // Reject a file (delete from quarantine)
        public async Task RejectAndDeleteFromQuarantineAsync(string caseId, string sha256Hash, string originalFileName, string reviewerId, string reason)
        {
            var key = $"{caseId}/{sha256Hash}/{originalFileName}";
            await _minio.RemoveObjectAsync(QuarantineBucket, key);

            await _auditLogger.LogAsync("quarantine.reject", caseId, originalFileName, reviewerId, sha256Hash, reason);
        }

        // (Optional) Upload a file to quarantine (with deduplication check)
        public async Task<string> UploadToQuarantineAsync(Stream fileStream, string originalFileName, string caseId, string sha256Hash, string uploaderId)
        {
            if (await _evidenceRepo.ExistsByHashAsync(sha256Hash))
            {
                await _auditLogger.LogAsync("quarantine.upload.duplicate", caseId, originalFileName, uploaderId, sha256Hash);
                return "DUPLICATE";
            }

            var key = $"{caseId}/{sha256Hash}/{originalFileName}";
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(QuarantineBucket)
                .WithObject(key)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType("application/octet-stream"));

            await _auditLogger.LogAsync("quarantine.upload", caseId, originalFileName, uploaderId, sha256Hash);
            return key;
        }
    }

  
}
