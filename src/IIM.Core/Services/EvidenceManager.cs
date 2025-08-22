// ============================================
// File: src/IIM.Core/Services/EvidenceManager.cs
// Purpose: Implementation using ONLY existing Models
// ============================================

using IIM.Core.Configuration;
using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.Services
{
    /// <summary>
    /// Evidence manager implementation - uses existing Models, no duplicates!
    /// </summary>
    public class EvidenceManager : IEvidenceManager
    {
        private readonly ILogger<EvidenceManager> _logger;
        private readonly EvidenceConfiguration _config;
        private readonly AuditDbContext _audit;
        private readonly Dictionary<string, Evidence> _evidenceStore = new();
        private readonly object _lock = new();

        public EvidenceManager(ILogger<EvidenceManager> logger, EvidenceConfiguration config, AuditDbContext audit)
        {
            _logger = logger;
            _config = config;
            EnsureDirectoriesExist();
            _audit = audit;
        }

        public async Task<Evidence> IngestEvidenceAsync(Stream stream, string fileName, EvidenceMetadata metadata, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Ingesting evidence: {FileName} for case {CaseNumber}", fileName, metadata.CaseNumber);

            if (!_config.IsFileTypeAllowed(fileName))
            {
                throw new ArgumentException($"File type not allowed: {Path.GetExtension(fileName)}");
            }

            var evidenceId = Guid.NewGuid().ToString("N");
            var storagePath = GetStoragePath(evidenceId, metadata.CustomFields.GetValueOrDefault("Classification", "UNCLASSIFIED"));

            var evidence = new Evidence
            {
                Id = evidenceId,
                CaseNumber = metadata.CaseNumber,
                OriginalFileName = fileName,
                StoragePath = storagePath,
                Metadata = metadata,
                Status = EvidenceStatus.Pending,
                Type = DetermineEvidenceType(fileName)
            };

            try
            {
                // Calculate hashes
                var hashes = await CalculateHashesAsync(stream, cancellationToken);
                evidence.Hashes = hashes;
                stream.Position = 0;

                // Save file
                using (var fileStream = new FileStream(storagePath, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream, cancellationToken);
                    evidence.FileSize = fileStream.Length;
                }

                // Check size limit
                if (evidence.FileSize > _config.MaxFileSizeMb * 1024 * 1024)
                {
                    File.Delete(storagePath);
                    throw new ArgumentException($"File exceeds maximum size of {_config.MaxFileSizeMb} MB");
                }

                // Add initial chain of custody entry
                evidence.ChainOfCustody.Add(new ChainOfCustodyEntry
                {
                    Action = "INGESTED",
                    Actor = metadata.CollectedBy,
                    Officer = metadata.CollectedBy,
                    Details = $"Evidence ingested from {metadata.CollectionLocation}",
                    Hash = hashes.GetValueOrDefault("SHA256", ""),
                    Notes = metadata.Description
                });

                // Generate signature
                evidence.Signature = GenerateSignature(evidence);
                evidence.Status = EvidenceStatus.Ingested;
                evidence.IntegrityValid = true;

                // Store
                lock (_lock)
                {
                    _evidenceStore[evidenceId] = evidence;
                }

                _logger.LogInformation("Evidence ingested successfully: {EvidenceId}", evidenceId);
                return evidence;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest evidence");
                if (File.Exists(storagePath))
                {
                    File.Delete(storagePath);
                }
                throw;
            }
        }

        public Task<Evidence> IngestEvidenceAsync(string filePath, EvidenceMetadata metadata, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return IngestEvidenceAsync(stream, Path.GetFileName(filePath), metadata, cancellationToken);
        }

        public async Task<ProcessedEvidence> ProcessEvidenceAsync(string evidenceId, string processingType, Func<Stream, Task<Stream>> processor, CancellationToken cancellationToken = default)
        {
            var evidence = await GetEvidenceAsync(evidenceId, cancellationToken);
            if (evidence == null)
                throw new EvidenceNotFoundException($"Evidence {evidenceId} not found");

            if (!await VerifyIntegrityAsync(evidenceId, cancellationToken))
                throw new IntegrityException($"Evidence {evidenceId} failed integrity check");

            var processedId = Guid.NewGuid().ToString("N");
            var processedPath = Path.Combine(_config.StorePath, "Processed", $"{processedId}_{Path.GetFileName(evidence.OriginalFileName)}");
            Directory.CreateDirectory(Path.GetDirectoryName(processedPath)!);

            using (var inputStream = new FileStream(evidence.StoragePath, FileMode.Open, FileAccess.Read))
            using (var processedStream = await processor(inputStream))
            using (var outputStream = new FileStream(processedPath, FileMode.Create, FileAccess.Write))
            {
                await processedStream.CopyToAsync(outputStream, cancellationToken);
            }

            // Calculate hash of processed file
            string processedHash;
            using (var stream = new FileStream(processedPath, FileMode.Open, FileAccess.Read))
            {
                var hashes = await CalculateHashesAsync(stream, cancellationToken);
                processedHash = hashes["SHA256"];
            }

            var processed = new ProcessedEvidence
            {
                Id = processedId,
                OriginalEvidenceId = evidenceId,
                ProcessingType = processingType,
                ProcessedBy = Environment.UserName,
                ProcessedHash = processedHash,
                StoragePath = processedPath
            };

            evidence.ProcessedVersions.Add(processed);
            evidence.Status = EvidenceStatus.Analyzed;

            // Add chain of custody entry
            evidence.ChainOfCustody.Add(new ChainOfCustodyEntry
            {
                Action = $"PROCESSED_{processingType.ToUpper()}",
                Actor = Environment.UserName,
                Officer = Environment.UserName,
                Details = $"Processed with {processingType}",
                Hash = processedHash
            });

            return processed;
        }

        public async Task<bool> VerifyIntegrityAsync(string evidenceId, CancellationToken cancellationToken = default)
        {
            var evidence = await GetEvidenceAsync(evidenceId, cancellationToken);
            if (evidence == null)
                throw new EvidenceNotFoundException($"Evidence {evidenceId} not found");

            if (!File.Exists(evidence.StoragePath))
            {
                _logger.LogError("Evidence file not found: {Path}", evidence.StoragePath);
                return false;
            }

            using var stream = new FileStream(evidence.StoragePath, FileMode.Open, FileAccess.Read);
            var currentHashes = await CalculateHashesAsync(stream, cancellationToken);

            foreach (var (algorithm, originalHash) in evidence.Hashes)
            {
                if (!currentHashes.TryGetValue(algorithm, out var currentHash) || currentHash != originalHash)
                {
                    _logger.LogError("Integrity check failed for {EvidenceId}. {Algorithm} mismatch", evidenceId, algorithm);
                    return false;
                }
            }

            return true;
        }

        public async Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(string evidenceId, CancellationToken cancellationToken = default)
        {
            var evidence = await GetEvidenceAsync(evidenceId, cancellationToken);
            if (evidence == null)
                throw new EvidenceNotFoundException($"Evidence {evidenceId} not found");

            var integrityValid = await VerifyIntegrityAsync(evidenceId, cancellationToken);

            return new ChainOfCustodyReport
            {
                EvidenceId = evidenceId,
                OriginalFileName = evidence.OriginalFileName,
                CaseNumber = evidence.CaseNumber,
                ChainEntries = evidence.ChainOfCustody.OrderBy(e => e.Timestamp).ToList(),
                ProcessedVersions = evidence.ProcessedVersions,
                IntegrityValid = integrityValid,
                OriginalHashes = evidence.Hashes,
                Signature = evidence.Signature,
                IngestTimestamp = evidence.IngestTimestamp.DateTime
            };
        }

        public async Task<EvidenceExport> ExportEvidenceAsync(string evidenceId, string exportPath, CancellationToken cancellationToken = default)
        {
            var evidence = await GetEvidenceAsync(evidenceId, cancellationToken);
            if (evidence == null)
                throw new EvidenceNotFoundException($"Evidence {evidenceId} not found");

            Directory.CreateDirectory(exportPath);

            var export = new EvidenceExport
            {
                EvidenceId = evidenceId,
                ExportPath = exportPath,
                ExportedBy = Environment.UserName
            };

            // Copy original evidence
            var destPath = Path.Combine(exportPath, evidence.OriginalFileName);
            File.Copy(evidence.StoragePath, destPath, true);
            export.Files.Add(destPath);

            // Copy processed versions
            foreach (var processed in evidence.ProcessedVersions)
            {
                if (File.Exists(processed.StoragePath))
                {
                    var processedDest = Path.Combine(exportPath, $"processed_{Path.GetFileName(processed.StoragePath)}");
                    File.Copy(processed.StoragePath, processedDest, true);
                    export.Files.Add(processedDest);
                }
            }

            // Generate chain of custody report
            var report = await GenerateChainOfCustodyAsync(evidenceId, cancellationToken);
            var reportPath = Path.Combine(exportPath, $"chain_of_custody_{evidenceId}.json");
            await File.WriteAllTextAsync(reportPath, System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            export.Files.Add(reportPath);

            export.IntegrityValid = report.IntegrityValid;
            return export;
        }

        public async Task<List<AuditEvent>> GetAuditLogAsync(string evidenceId, CancellationToken cancellationToken = default)
        {
            var evidenceLog = await _audit.AuditLogs
                .Where(e => e.EntityId == evidenceId)
                .ToListAsync(cancellationToken);

            return evidenceLog;
        }


        public Task LogAccessAsync(string evidenceId, string action, string userId, CancellationToken cancellationToken = default)
        {
            AuditEvent auditEvent = new AuditEvent
            {
                EntityId = evidenceId,
                Action = action,
                UserId = userId,
                Timestamp = DateTimeOffset.UtcNow
            };

            _audit.AuditLogs.Add(auditEvent);
            _audit.SaveChanges();
            return Task.CompletedTask;
        }

        public Task<Evidence?> GetEvidenceAsync(string evidenceId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _evidenceStore.TryGetValue(evidenceId, out var evidence);
                return Task.FromResult(evidence);
            }
        }

        public Task<Stream> GetEvidenceStreamAsync(string evidenceId, CancellationToken cancellationToken = default)
        {
            var evidence = _evidenceStore.GetValueOrDefault(evidenceId);
            if (evidence == null)
                throw new EvidenceNotFoundException($"Evidence {evidenceId} not found");

            return Task.FromResult<Stream>(new FileStream(evidence.StoragePath, FileMode.Open, FileAccess.Read));
        }

        public Task<List<Evidence>> ListEvidenceAsync(string? caseNumber = null, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var query = _evidenceStore.Values.AsEnumerable();
                if (!string.IsNullOrEmpty(caseNumber))
                    query = query.Where(e => e.CaseNumber == caseNumber);

                return Task.FromResult(query.ToList());
            }
        }

        // Helper methods
        private async Task<Dictionary<string, string>> CalculateHashesAsync(Stream stream, CancellationToken cancellationToken)
        {
            var hashes = new Dictionary<string, string>();

            // SHA256
            using (var sha256 = SHA256.Create())
            {
                stream.Position = 0;
                var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
                hashes["SHA256"] = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

            // MD5 (for legacy compatibility)
            using (var md5 = MD5.Create())
            {
                stream.Position = 0;
                var hash = await md5.ComputeHashAsync(stream, cancellationToken);
                hashes["MD5"] = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

            stream.Position = 0;
            return hashes;
        }

        private string GenerateSignature(Evidence evidence)
        {
            var data = $"{evidence.Id}{evidence.OriginalFileName}{evidence.FileSize}{string.Join("", evidence.Hashes.Values)}";
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private string GetStoragePath(string evidenceId, string classification)
        {
            var basePath = _config.GetStoragePathForClassification(classification);
            Directory.CreateDirectory(basePath);
            return Path.Combine(basePath, $"{evidenceId}.evidence");
        }

        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(_config.StorePath);
            Directory.CreateDirectory(_config.TempPath);
            Directory.CreateDirectory(_config.QuarantinePath);
            Directory.CreateDirectory(Path.Combine(_config.StorePath, "Processed"));
        }

        private EvidenceType DetermineEvidenceType(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            return ext switch
            {
                ".pdf" or ".doc" or ".docx" or ".txt" => EvidenceType.Document,
                ".jpg" or ".jpeg" or ".png" or ".gif" => EvidenceType.Image,
                ".mp4" or ".avi" or ".mkv" or ".mov" => EvidenceType.Video,
                ".mp3" or ".wav" or ".flac" => EvidenceType.Audio,
                ".eml" or ".msg" or ".pst" => EvidenceType.Email,
                ".db" or ".sqlite" or ".mdb" => EvidenceType.Database,
                ".dd" or ".e01" or ".img" => EvidenceType.DiskImage,
                ".dmp" or ".mdmp" => EvidenceType.MemoryDump,
                ".pcap" or ".pcapng" => EvidenceType.NetworkCapture,
                ".log" or ".evtx" => EvidenceType.LogFile,
                ".zip" or ".rar" or ".7z" => EvidenceType.Archive,
                _ => EvidenceType.Other
            };
        }

        public Task<List<Evidence>> GetEvidenceByCaseAsync(string caseId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var evidence = _evidenceStore.Values
                    .Where(e => e.CaseId == caseId)
                    .ToList();
                return Task.FromResult(evidence);
            }
        }

        /// <summary>
        /// Registers evidence in pending state before upload completes
        /// </summary>
        public async Task<Evidence> RegisterPendingEvidenceAsync(
            Evidence evidence,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Registering pending evidence {Id} for case {CaseNumber}",
                evidence.Id, evidence.CaseNumber);

            // Store in memory dictionary (or database if you have one)
            lock (_lock)
            {
                _evidenceStore[evidence.Id] = evidence;
            }

            // If you have a database context, save it here:
            // await _dbContext.Evidence.AddAsync(evidence, cancellationToken);
            // await _dbContext.SaveChangesAsync(cancellationToken);

            return await Task.FromResult(evidence);
        }

        /// <summary>
        /// Updates the status of existing evidence
        /// </summary>
        public async Task UpdateEvidenceStatusAsync(
            string evidenceId,
            EvidenceStatus status,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating evidence {Id} status to {Status}",
                evidenceId, status);

            lock (_lock)
            {
                if (_evidenceStore.TryGetValue(evidenceId, out var evidence))
                {
                    evidence.Status = status;
                    evidence.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    _logger.LogWarning("Evidence {Id} not found for status update", evidenceId);
                }
            }

            // If using database:
            // var evidence = await _dbContext.Evidence.FindAsync(evidenceId);
            // if (evidence != null)
            // {
            //     evidence.Status = status;
            //     await _dbContext.SaveChangesAsync(cancellationToken);
            // }

            await Task.CompletedTask;
        }

     
    }
}
