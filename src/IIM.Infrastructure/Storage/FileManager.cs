using System;
using IIM.Core.Models;
using IIM.Infrastructure.Data;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace IIM.Infrastructure.Storage
{
    /// <summary>
    /// Evidence manager implementation - uses existing Models, no duplicates!
    /// </summary>
    public class FileManager 
    {
        private readonly ILogger<FileManager> _logger;
        private readonly FilesConfiguration _config;
        private readonly AuditDbContext _audit;
        private readonly Dictionary<string, ManagedFile> _fileStore = new();
        private readonly object _lock = new();

        public FileManager(ILogger<FileManager> logger, FilesConfiguration config, AuditDbContext audit)
        {
            _logger = logger;
            _config = config;
            EnsureDirectoriesExist();
            _audit = audit;
        }

        public async Task<ManagedFile> IngestEvidenceAsync(Stream stream, string fileName, FileMetadata metadata, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Ingesting file: {FileName} for workspace {CaseNumber}", fileName, metadata.);

            if (!_config.IsFileTypeAllowed(fileName))
            {
                throw new ArgumentException($"File type not allowed: {Path.GetExtension(fileName)}");
            }

            var evidenceId = Guid.NewGuid().ToString("N");
            var storagePath = GetStoragePath(evidenceId, metadata.CustomFields.GetValueOrDefault("Classification", "UNCLASSIFIED"));

            var evidence = new ManagedFile
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
                    _fileStore[evidenceId] = evidence;
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

        public Task<ManagedFile> IngestFileAsync(string filePath, FileMetadata metadata, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return IngestEvidenceAsync(stream, Path.GetFileName(filePath), metadata, cancellationToken);
        }

        public async Task<ProcessedFile> ProcessfileAsync(string fileId, string processingType, Func<Stream, Task<Stream>> processor, CancellationToken cancellationToken = default)
        {
            var evidence = await GetEvidenceAsync(fileId, cancellationToken);
            if (evidence == null)
                throw new ManagedFileNotFoundException($"File {fileId} not found");

            if (!await VerifyIntegrityAsync(fileId, cancellationToken))
                throw new IntegrityException($"File {fileId} failed integrity check");

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

            var processed = new ProcessedFile
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

                Details = $"Processed with {processingType}",
                Hash = processedHash
            });

            return processed;
        }

        public async Task<bool> VerifyIntegrityAsync(string fileId, CancellationToken cancellationToken = default)
        {
            var _file = await GetEvidenceAsync(fileId, cancellationToken);
            if (_file == null)
                throw new ManagedFileNotFoundException($"File {fileId} not found");

            if (!File.Exists(_file.StoragePath))
            {
                _logger.LogError("Managed file not found: {Path}", _file.StoragePath);
                return false;
            }

            using var stream = new FileStream(_file.StoragePath, FileMode.Open, FileAccess.Read);
            var currentHashes = await CalculateHashesAsync(stream, cancellationToken);

            foreach (var (algorithm, originalHash) in _file.Hashes)
            {
                if (!currentHashes.TryGetValue(algorithm, out var currentHash) || currentHash != originalHash)
                {
                    _logger.LogError("Integrity check failed for {EvidenceId}. {Algorithm} mismatch", fileId, algorithm);
                    return false;
                }
            }

            return true;
        }

        public async Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(string fileId, CancellationToken cancellationToken = default)
        {
            var evidence = await GetEvidenceAsync(fileId, cancellationToken);
            if (evidence == null)
                throw new ManagedFileNotFoundException($"File {fileId} not found");

            var integrityValid = await VerifyIntegrityAsync(fileId, cancellationToken);

            return new ChainOfCustodyReport
            {
                EvidenceId = fileId,
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

        public async Task<FileExport> ExportEvidenceAsync(string evidenceId, string exportPath, CancellationToken cancellationToken = default)
        {
            var evidence = await GetEvidenceAsync(evidenceId, cancellationToken);
            if (evidence == null)
                throw new EvidenceNotFoundException($"Evidence {evidenceId} not found");

            Directory.CreateDirectory(exportPath);

            var export = new FileExport
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

        public Task<ManagedFile?> GetEvidenceAsync(string evidenceId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _fileStore.TryGetValue(evidenceId, out var evidence);
                return Task.FromResult(evidence);
            }
        }

        public Task<Stream> GetEvidenceStreamAsync(string evidenceId, CancellationToken cancellationToken = default)
        {
            var evidence = _fileStore.GetValueOrDefault(evidenceId);
            if (evidence == null)
                throw new ManagedFileNotFoundException($"Evidence {evidenceId} not found");

            return Task.FromResult<Stream>(new FileStream(evidence.StoragePath, FileMode.Open, FileAccess.Read));
        }

        public Task<List<ManagedFile>> ListEvidenceAsync(string? caseNumber = null, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var query = _fileStore.Values.AsEnumerable();
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

        private string GenerateSignature(ManagedFile evidence)
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

        public Task<List<ManagedFile>> GetFilesByWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var evidence = _fileStore.Values
                    .Where(e => e.CaseId == workspaceId)
                    .ToList();
                return Task.FromResult(evidence);
            }
        }

        /// <summary>
        /// Registers evidence in pending state before upload completes
        /// </summary>
        public async Task<ManagedFile> RegisterPendingEvidenceAsync(
            ManagedFile managedFile,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Registering pending evidence {Id} for case {CaseNumber}",
                managedFile.Id, managedFile.CaseNumber);

            // Store in memory dictionary (or database if you have one)
            lock (_lock)
            {
                _fileStore[managedFile.Id] = managedFile;
            }

            // If you have a database context, save it here:
            // await _dbContext.Evidence.AddAsync(evidence, cancellationToken);
            // await _dbContext.SaveChangesAsync(cancellationToken);

            return await Task.FromResult(managedFile);
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
                if (_fileStore.TryGetValue(evidenceId, out var evidence))
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
