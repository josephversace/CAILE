using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Security;

/// <summary>
/// Interface for managing evidence with cryptographic integrity
/// </summary>
public interface IEvidenceManager
{
    Task<EvidenceItem> IngestEvidenceAsync(Stream fileStream, string fileName, EvidenceMetadata metadata, CancellationToken ct = default);
    Task<EvidenceItem> IngestEvidenceAsync(string filePath, EvidenceMetadata metadata, CancellationToken ct = default);
    Task<ProcessedEvidence> ProcessEvidenceAsync(string evidenceId, string processingType, Func<Stream, Task<Stream>> processor, CancellationToken ct = default);
    Task<bool> VerifyIntegrityAsync(string evidenceId, CancellationToken ct = default);
    Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(string evidenceId, CancellationToken ct = default);
    Task<EvidenceExport> ExportEvidenceAsync(string evidenceId, string exportPath, CancellationToken ct = default);
    Task<List<AuditLogEntry>> GetAuditLogAsync(string evidenceId, CancellationToken ct = default);
}

/// <summary>
/// Manages evidence with cryptographic integrity and chain of custody tracking
/// Ensures all evidence and AI outputs are forensically sound for court proceedings
/// </summary>
public sealed class EvidenceManager : IEvidenceManager
{
    private readonly ILogger<EvidenceManager> _logger;
    private readonly string _evidenceStorePath;
    private readonly string _auditLogPath;
    private readonly ConcurrentDictionary<string, EvidenceItem> _evidenceCache = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    // Cryptographic components
    private readonly RSA _rsaKey;
    private readonly string _publicKeyXml;
    private readonly string _machineIdentifier;

    /// <summary>
    /// Initializes the evidence manager with secure storage
    /// </summary>
    public EvidenceManager(ILogger<EvidenceManager> logger, EvidenceConfiguration config)
    {
        _logger = logger;
        _evidenceStorePath = config.StorePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IIM", "Evidence");
        _auditLogPath = Path.Combine(_evidenceStorePath, "AuditLogs");

        Directory.CreateDirectory(_evidenceStorePath);
        Directory.CreateDirectory(_auditLogPath);

        // Initialize RSA key for signing
        _rsaKey = RSA.Create(2048);
        _publicKeyXml = _rsaKey.ToXmlString(false);

        // Generate machine identifier
        _machineIdentifier = GenerateMachineIdentifier();

        _logger.LogInformation("Evidence manager initialized at {Path}", _evidenceStorePath);
    }

    /// <summary>
    /// Ingests evidence from a stream with full integrity tracking
    /// </summary>
    public async Task<EvidenceItem> IngestEvidenceAsync(
        Stream fileStream,
        string fileName,
        EvidenceMetadata metadata,
        CancellationToken ct = default)
    {
        var evidenceId = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow;

        _logger.LogInformation("Ingesting evidence: {FileName} with ID {EvidenceId}", fileName, evidenceId);

        await _writeLock.WaitAsync(ct);
        try
        {
            // Create evidence directory
            var evidenceDir = Path.Combine(_evidenceStorePath, evidenceId);
            Directory.CreateDirectory(evidenceDir);

            // Calculate hashes BEFORE storing
            fileStream.Position = 0;
            var sha256Hash = await CalculateSHA256Async(fileStream, ct);

            fileStream.Position = 0;
            var sha512Hash = await CalculateSHA512Async(fileStream, ct);

            fileStream.Position = 0;
            var md5Hash = await CalculateMD5Async(fileStream, ct);

            // Store original file
            var originalPath = Path.Combine(evidenceDir, "original_" + SanitizeFileName(fileName));
            fileStream.Position = 0;
            using (var fileOut = File.Create(originalPath))
            {
                await fileStream.CopyToAsync(fileOut, ct);
            }

            // Create evidence item
            var evidence = new EvidenceItem
            {
                Id = evidenceId,
                OriginalFileName = fileName,
                StoragePath = originalPath,
                IngestTimestamp = timestamp,
                FileSize = new FileInfo(originalPath).Length,
                Metadata = metadata,
                Hashes = new HashSet
                {
                    SHA256 = sha256Hash,
                    SHA512 = sha512Hash,
                    MD5 = md5Hash
                },
                MachineIdentifier = _machineIdentifier,
                IngestingOfficer = metadata.CollectedBy,
                CaseNumber = metadata.CaseNumber,
                ChainOfCustody = new List<CustodyEntry>
                {
                    new CustodyEntry
                    {
                        Timestamp = timestamp,
                        Action = "INGESTED",
                        Officer = metadata.CollectedBy,
                        Location = _machineIdentifier,
                        Details = $"Original file ingested: {fileName}",
                        HashBefore = sha256Hash,
                        HashAfter = sha256Hash
                    }
                }
            };

            // Generate and store signature
            evidence.Signature = await GenerateSignatureAsync(evidence);

            // Save evidence manifest
            var manifestPath = Path.Combine(evidenceDir, "manifest.json");
            var manifestJson = JsonSerializer.Serialize(evidence, GetJsonOptions());
            await File.WriteAllTextAsync(manifestPath, manifestJson, ct);

            // Create immutable backup
            var backupPath = Path.Combine(evidenceDir, $"manifest_{timestamp.ToUnixTimeSeconds()}.json.bak");
            await File.WriteAllTextAsync(backupPath, manifestJson, ct);
            File.SetAttributes(backupPath, FileAttributes.ReadOnly);

            // Log to audit trail
            await LogAuditEventAsync(new AuditLogEntry
            {
                Timestamp = timestamp,
                EvidenceId = evidenceId,
                Action = AuditAction.Ingested,
                Actor = metadata.CollectedBy,
                Details = $"Ingested file: {fileName}, Size: {evidence.FileSize} bytes",
                Hash = sha256Hash,
                Signature = evidence.Signature
            }, ct);

            // Cache the evidence item
            _evidenceCache[evidenceId] = evidence;

            _logger.LogInformation("Evidence {EvidenceId} ingested successfully. SHA256: {Hash}",
                evidenceId, sha256Hash);

            return evidence;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Ingests evidence from a file path
    /// </summary>
    public async Task<EvidenceItem> IngestEvidenceAsync(
        string filePath,
        EvidenceMetadata metadata,
        CancellationToken ct = default)
    {
        using var fileStream = File.OpenRead(filePath);
        return await IngestEvidenceAsync(fileStream, Path.GetFileName(filePath), metadata, ct);
    }

    /// <summary>
    /// Processes evidence with full tracking of transformations
    /// </summary>
    public async Task<ProcessedEvidence> ProcessEvidenceAsync(
        string evidenceId,
        string processingType,
        Func<Stream, Task<Stream>> processor,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Processing evidence {EvidenceId} with {ProcessingType}",
            evidenceId, processingType);

        var evidence = await LoadEvidenceAsync(evidenceId, ct);
        if (evidence == null)
        {
            throw new EvidenceNotFoundException($"Evidence {evidenceId} not found");
        }

        var timestamp = DateTimeOffset.UtcNow;
        var processedId = Guid.NewGuid().ToString("N");

        await _writeLock.WaitAsync(ct);
        try
        {
            // Load original file
            using var originalStream = File.OpenRead(evidence.StoragePath);

            // Calculate hash before processing
            originalStream.Position = 0;
            var hashBefore = await CalculateSHA256Async(originalStream, ct);

            // Verify integrity before processing
            if (hashBefore != evidence.Hashes.SHA256)
            {
                throw new IntegrityException($"Evidence {evidenceId} integrity check failed before processing");
            }

            // Process the evidence
            originalStream.Position = 0;
            using var processedStream = await processor(originalStream);

            // Save processed output
            var evidenceDir = Path.GetDirectoryName(evidence.StoragePath)!;
            var processedPath = Path.Combine(evidenceDir, $"processed_{processedId}_{processingType}.dat");

            processedStream.Position = 0;
            using (var fileOut = File.Create(processedPath))
            {
                await processedStream.CopyToAsync(fileOut, ct);
            }

            // Calculate hash of processed output
            processedStream.Position = 0;
            var hashAfter = await CalculateSHA256Async(processedStream, ct);

            // Create processed evidence record
            var processed = new ProcessedEvidence
            {
                Id = processedId,
                OriginalEvidenceId = evidenceId,
                ProcessingType = processingType,
                ProcessedTimestamp = timestamp,
                ProcessedPath = processedPath,
                ProcessedHash = hashAfter,
                ProcessingParameters = new Dictionary<string, object>
                {
                    ["Type"] = processingType,
                    ["Timestamp"] = timestamp,
                    ["MachineId"] = _machineIdentifier
                }
            };

            // Update chain of custody
            var custodyEntry = new CustodyEntry
            {
                Timestamp = timestamp,
                Action = $"PROCESSED:{processingType}",
                Officer = Environment.UserName,
                Location = _machineIdentifier,
                Details = $"Evidence processed using {processingType}",
                HashBefore = hashBefore,
                HashAfter = hashAfter,
                ProcessedEvidenceId = processedId
            };

            evidence.ChainOfCustody.Add(custodyEntry);
            evidence.ProcessedVersions.Add(processed);

            // Re-sign the evidence
            evidence.Signature = await GenerateSignatureAsync(evidence);

            // Save updated manifest
            await SaveEvidenceAsync(evidence, ct);

            // Log to audit trail
            await LogAuditEventAsync(new AuditLogEntry
            {
                Timestamp = timestamp,
                EvidenceId = evidenceId,
                Action = AuditAction.Processed,
                Actor = Environment.UserName,
                Details = $"Processed with {processingType}",
                Hash = hashAfter,
                Signature = evidence.Signature,
                ProcessedEvidenceId = processedId
            }, ct);

            _logger.LogInformation("Evidence {EvidenceId} processed successfully. Output: {ProcessedId}",
                evidenceId, processedId);

            return processed;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Verifies the integrity of evidence
    /// </summary>
    public async Task<bool> VerifyIntegrityAsync(string evidenceId, CancellationToken ct = default)
    {
        _logger.LogInformation("Verifying integrity of evidence {EvidenceId}", evidenceId);

        var evidence = await LoadEvidenceAsync(evidenceId, ct);
        if (evidence == null)
        {
            _logger.LogWarning("Evidence {EvidenceId} not found", evidenceId);
            return false;
        }

        try
        {
            // Verify file hash
            using var fileStream = File.OpenRead(evidence.StoragePath);
            var currentHash = await CalculateSHA256Async(fileStream, ct);

            if (currentHash != evidence.Hashes.SHA256)
            {
                _logger.LogError("Hash mismatch for evidence {EvidenceId}. Expected: {Expected}, Got: {Actual}",
                    evidenceId, evidence.Hashes.SHA256, currentHash);

                await LogAuditEventAsync(new AuditLogEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    EvidenceId = evidenceId,
                    Action = AuditAction.IntegrityCheckFailed,
                    Actor = Environment.UserName,
                    Details = "Hash verification failed",
                    Hash = currentHash
                }, ct);

                return false;
            }

            // Verify signature
            var signatureValid = await VerifySignatureAsync(evidence);

            if (!signatureValid)
            {
                _logger.LogError("Signature verification failed for evidence {EvidenceId}", evidenceId);

                await LogAuditEventAsync(new AuditLogEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    EvidenceId = evidenceId,
                    Action = AuditAction.IntegrityCheckFailed,
                    Actor = Environment.UserName,
                    Details = "Signature verification failed"
                }, ct);

                return false;
            }

            _logger.LogInformation("Integrity verified successfully for evidence {EvidenceId}", evidenceId);

            await LogAuditEventAsync(new AuditLogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                EvidenceId = evidenceId,
                Action = AuditAction.IntegrityVerified,
                Actor = Environment.UserName,
                Details = "Integrity check passed",
                Hash = currentHash
            }, ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying integrity of evidence {EvidenceId}", evidenceId);
            return false;
        }
    }

    /// <summary>
    /// Generates a chain of custody report for evidence
    /// </summary>
    public async Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(
        string evidenceId,
        CancellationToken ct = default)
    {
        var evidence = await LoadEvidenceAsync(evidenceId, ct);
        if (evidence == null)
        {
            throw new EvidenceNotFoundException($"Evidence {evidenceId} not found");
        }

        var report = new ChainOfCustodyReport
        {
            EvidenceId = evidenceId,
            OriginalFileName = evidence.OriginalFileName,
            CaseNumber = evidence.CaseNumber,
            IngestTimestamp = evidence.IngestTimestamp,
            GeneratedTimestamp = DateTimeOffset.UtcNow,
            GeneratedBy = Environment.UserName,
            MachineIdentifier = _machineIdentifier,

            OriginalHashes = evidence.Hashes,
            CurrentIntegrityValid = await VerifyIntegrityAsync(evidenceId, ct),

            CustodyEntries = evidence.ChainOfCustody.OrderBy(c => c.Timestamp).ToList(),
            ProcessedVersions = evidence.ProcessedVersions,

            AuditLog = await GetAuditLogAsync(evidenceId, ct),

            Signature = evidence.Signature,
            PublicKey = _publicKeyXml
        };

        // Sign the report
        report.ReportSignature = await GenerateReportSignatureAsync(report);

        _logger.LogInformation("Chain of custody report generated for evidence {EvidenceId}", evidenceId);

        return report;
    }

    /// <summary>
    /// Exports evidence with all metadata and verification
    /// </summary>
    public async Task<EvidenceExport> ExportEvidenceAsync(
        string evidenceId,
        string exportPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting evidence {EvidenceId} to {Path}", evidenceId, exportPath);

        var evidence = await LoadEvidenceAsync(evidenceId, ct);
        if (evidence == null)
        {
            throw new EvidenceNotFoundException($"Evidence {evidenceId} not found");
        }

        var timestamp = DateTimeOffset.UtcNow;
        var exportId = Guid.NewGuid().ToString("N");

        // Create export directory
        var exportDir = Path.Combine(exportPath, $"Evidence_{evidenceId}_{timestamp.ToUnixTimeSeconds()}");
        Directory.CreateDirectory(exportDir);

        // Copy original file
        var originalExportPath = Path.Combine(exportDir, $"ORIGINAL_{evidence.OriginalFileName}");
        File.Copy(evidence.StoragePath, originalExportPath, overwrite: true);

        // Copy processed versions
        foreach (var processed in evidence.ProcessedVersions)
        {
            var processedExportPath = Path.Combine(exportDir,
                $"PROCESSED_{processed.ProcessingType}_{Path.GetFileName(processed.ProcessedPath)}");
            if (File.Exists(processed.ProcessedPath))
            {
                File.Copy(processed.ProcessedPath, processedExportPath, overwrite: true);
            }
        }

        // Generate chain of custody report
        var report = await GenerateChainOfCustodyAsync(evidenceId, ct);
        var reportPath = Path.Combine(exportDir, "CHAIN_OF_CUSTODY.json");
        await File.WriteAllTextAsync(reportPath,
            JsonSerializer.Serialize(report, GetJsonOptions()), ct);

        // Generate verification script
        var verificationScript = GenerateVerificationScript(evidence);
        var scriptPath = Path.Combine(exportDir, "VERIFY.ps1");
        await File.WriteAllTextAsync(scriptPath, verificationScript, ct);

        // Create export manifest
        var export = new EvidenceExport
        {
            ExportId = exportId,
            EvidenceId = evidenceId,
            ExportTimestamp = timestamp,
            ExportedBy = Environment.UserName,
            ExportPath = exportDir,
            Files = Directory.GetFiles(exportDir).Select(Path.GetFileName).ToList()!,
            IntegrityValid = await VerifyIntegrityAsync(evidenceId, ct),
            Signature = await GenerateExportSignatureAsync(evidence, exportId, timestamp)
        };

        var exportManifestPath = Path.Combine(exportDir, "EXPORT_MANIFEST.json");
        await File.WriteAllTextAsync(exportManifestPath,
            JsonSerializer.Serialize(export, GetJsonOptions()), ct);

        // Log export
        await LogAuditEventAsync(new AuditLogEntry
        {
            Timestamp = timestamp,
            EvidenceId = evidenceId,
            Action = AuditAction.Exported,
            Actor = Environment.UserName,
            Details = $"Exported to {exportDir}",
            ExportId = exportId
        }, ct);

        _logger.LogInformation("Evidence {EvidenceId} exported successfully to {Path}",
            evidenceId, exportDir);

        return export;
    }

    /// <summary>
    /// Gets the audit log for specific evidence
    /// </summary>
    public async Task<List<AuditLogEntry>> GetAuditLogAsync(string evidenceId, CancellationToken ct = default)
    {
        var logFile = Path.Combine(_auditLogPath, $"{evidenceId}.audit.json");

        if (!File.Exists(logFile))
        {
            return new List<AuditLogEntry>();
        }

        var json = await File.ReadAllTextAsync(logFile, ct);
        var entries = JsonSerializer.Deserialize<List<AuditLogEntry>>(json, GetJsonOptions())
            ?? new List<AuditLogEntry>();

        return entries.OrderBy(e => e.Timestamp).ToList();
    }

    #region Private Methods

    /// <summary>
    /// Loads evidence from storage
    /// </summary>
    private async Task<EvidenceItem?> LoadEvidenceAsync(string evidenceId, CancellationToken ct)
    {
        if (_evidenceCache.TryGetValue(evidenceId, out var cached))
        {
            return cached;
        }

        var manifestPath = Path.Combine(_evidenceStorePath, evidenceId, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(manifestPath, ct);
        var evidence = JsonSerializer.Deserialize<EvidenceItem>(json, GetJsonOptions());

        if (evidence != null)
        {
            _evidenceCache[evidenceId] = evidence;
        }

        return evidence;
    }

    /// <summary>
    /// Saves evidence to storage
    /// </summary>
    private async Task SaveEvidenceAsync(EvidenceItem evidence, CancellationToken ct)
    {
        var manifestPath = Path.Combine(_evidenceStorePath, evidence.Id, "manifest.json");
        var json = JsonSerializer.Serialize(evidence, GetJsonOptions());
        await File.WriteAllTextAsync(manifestPath, json, ct);

        // Create versioned backup
        var timestamp = DateTimeOffset.UtcNow;
        var backupPath = Path.Combine(_evidenceStorePath, evidence.Id,
            $"manifest_{timestamp.ToUnixTimeSeconds()}.json.bak");
        await File.WriteAllTextAsync(backupPath, json, ct);
        File.SetAttributes(backupPath, FileAttributes.ReadOnly);
    }

    /// <summary>
    /// Logs an audit event
    /// </summary>
    private async Task LogAuditEventAsync(AuditLogEntry entry, CancellationToken ct)
    {
        var logFile = Path.Combine(_auditLogPath, $"{entry.EvidenceId}.audit.json");

        List<AuditLogEntry> entries;
        if (File.Exists(logFile))
        {
            var json = await File.ReadAllTextAsync(logFile, ct);
            entries = JsonSerializer.Deserialize<List<AuditLogEntry>>(json, GetJsonOptions())
                ?? new List<AuditLogEntry>();
        }
        else
        {
            entries = new List<AuditLogEntry>();
        }

        entries.Add(entry);

        await File.WriteAllTextAsync(logFile,
            JsonSerializer.Serialize(entries, GetJsonOptions()), ct);

        // Also write to append-only log
        var dailyLog = Path.Combine(_auditLogPath,
            $"audit_{DateTime.UtcNow:yyyy-MM-dd}.log");
        await File.AppendAllTextAsync(dailyLog,
            JsonSerializer.Serialize(entry, GetJsonOptions()) + Environment.NewLine, ct);
    }

    /// <summary>
    /// Calculates SHA256 hash of a stream
    /// </summary>
    private async Task<string> CalculateSHA256Async(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Calculates SHA512 hash of a stream
    /// </summary>
    private async Task<string> CalculateSHA512Async(Stream stream, CancellationToken ct)
    {
        using var sha512 = SHA512.Create();
        var hash = await sha512.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Calculates MD5 hash of a stream (for legacy compatibility)
    /// </summary>
    private async Task<string> CalculateMD5Async(Stream stream, CancellationToken ct)
    {
        using var md5 = MD5.Create();
        var hash = await md5.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Generates a digital signature for evidence
    /// </summary>
    private async Task<string> GenerateSignatureAsync(EvidenceItem evidence)
    {
        await Task.CompletedTask; // Make async

        var dataToSign = $"{evidence.Id}|{evidence.Hashes.SHA256}|{evidence.IngestTimestamp.ToUnixTimeSeconds()}";
        var bytes = Encoding.UTF8.GetBytes(dataToSign);
        var signature = _rsaKey.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// Verifies a digital signature
    /// </summary>
    private async Task<bool> VerifySignatureAsync(EvidenceItem evidence)
    {
        await Task.CompletedTask; // Make async

        try
        {
            var dataToVerify = $"{evidence.Id}|{evidence.Hashes.SHA256}|{evidence.IngestTimestamp.ToUnixTimeSeconds()}";
            var bytes = Encoding.UTF8.GetBytes(dataToVerify);
            var signature = Convert.FromBase64String(evidence.Signature);
            return _rsaKey.VerifyData(bytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates signature for chain of custody report
    /// </summary>
    private async Task<string> GenerateReportSignatureAsync(ChainOfCustodyReport report)
    {
        await Task.CompletedTask; // Make async

        var dataToSign = JsonSerializer.Serialize(new
        {
            report.EvidenceId,
            report.OriginalHashes,
            report.GeneratedTimestamp
        });
        var bytes = Encoding.UTF8.GetBytes(dataToSign);
        var signature = _rsaKey.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// Generates signature for evidence export
    /// </summary>
    private async Task<string> GenerateExportSignatureAsync(EvidenceItem evidence, string exportId, DateTimeOffset timestamp)
    {
        await Task.CompletedTask; // Make async

        var dataToSign = $"{evidence.Id}|{exportId}|{timestamp.ToUnixTimeSeconds()}";
        var bytes = Encoding.UTF8.GetBytes(dataToSign);
        var signature = _rsaKey.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// Generates a PowerShell verification script
    /// </summary>
    private string GenerateVerificationScript(EvidenceItem evidence)
    {
        return $@"
# Evidence Verification Script
# Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
# Evidence ID: {evidence.Id}

$evidenceFile = 'ORIGINAL_{evidence.OriginalFileName}'
$expectedSHA256 = '{evidence.Hashes.SHA256}'
$expectedSHA512 = '{evidence.Hashes.SHA512}'
$expectedMD5 = '{evidence.Hashes.MD5}'

Write-Host 'Verifying evidence integrity...' -ForegroundColor Yellow

# Calculate SHA256
$sha256 = Get-FileHash -Path $evidenceFile -Algorithm SHA256
if ($sha256.Hash -eq $expectedSHA256) {{
    Write-Host '✓ SHA256 verification PASSED' -ForegroundColor Green
}} else {{
    Write-Host '✗ SHA256 verification FAILED' -ForegroundColor Red
    Write-Host ""  Expected: $expectedSHA256""
    Write-Host ""  Got:      $($sha256.Hash)""
}}

# Calculate SHA512
$sha512 = Get-FileHash -Path $evidenceFile -Algorithm SHA512
if ($sha512.Hash -eq $expectedSHA512) {{
    Write-Host '✓ SHA512 verification PASSED' -ForegroundColor Green
}} else {{
    Write-Host '✗ SHA512 verification FAILED' -ForegroundColor Red
}}

# Calculate MD5
$md5 = Get-FileHash -Path $evidenceFile -Algorithm MD5
if ($md5.Hash -eq $expectedMD5) {{
    Write-Host '✓ MD5 verification PASSED' -ForegroundColor Green
}} else {{
    Write-Host '✗ MD5 verification FAILED' -ForegroundColor Red
}}

Write-Host ''
Write-Host 'Case Number: {evidence.CaseNumber}' -ForegroundColor Cyan
Write-Host 'Ingested: {evidence.IngestTimestamp:yyyy-MM-dd HH:mm:ss} UTC' -ForegroundColor Cyan
Write-Host 'Ingesting Officer: {evidence.IngestingOfficer}' -ForegroundColor Cyan
";
    }

    /// <summary>
    /// Generates a unique machine identifier
    /// </summary>
    private string GenerateMachineIdentifier()
    {
        var machineInfo = $"{Environment.MachineName}|{Environment.UserDomainName}|{Environment.ProcessorCount}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineInfo));
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    /// <summary>
    /// Sanitizes file names for storage
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 200 ? sanitized.Substring(0, 200) : sanitized;
    }

    /// <summary>
    /// Gets JSON serialization options
    /// </summary>
    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    #endregion
}

// Supporting types
public sealed class EvidenceItem
{
    public required string Id { get; init; }
    public required string OriginalFileName { get; init; }
    public required string StoragePath { get; init; }
    public required DateTimeOffset IngestTimestamp { get; init; }
    public required long FileSize { get; init; }
    public required EvidenceMetadata Metadata { get; init; }
    public required HashSet Hashes { get; init; }
    public required string MachineIdentifier { get; init; }
    public required string IngestingOfficer { get; init; }
    public required string CaseNumber { get; init; }
    public List<CustodyEntry> ChainOfCustody { get; init; } = new();
    public List<ProcessedEvidence> ProcessedVersions { get; init; } = new();
    public string Signature { get; set; } = string.Empty;  // <-- Fixed: Not required, has default value
}


public sealed class EvidenceMetadata
{
    public required string CaseNumber { get; init; }
    public required string CollectedBy { get; init; }
    public DateTimeOffset CollectionDate { get; init; }
    public string? CollectionLocation { get; init; }
    public string? DeviceSource { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, string> CustomFields { get; init; } = new();
}

public sealed class HashSet
{
    public required string SHA256 { get; init; }
    public required string SHA512 { get; init; }
    public required string MD5 { get; init; }
}

public sealed class CustodyEntry
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Action { get; init; }
    public required string Officer { get; init; }
    public required string Location { get; init; }
    public string? Details { get; init; }
    public required string HashBefore { get; init; }
    public required string HashAfter { get; init; }
    public string? ProcessedEvidenceId { get; init; }
}

public sealed class ProcessedEvidence
{
    public required string Id { get; init; }
    public required string OriginalEvidenceId { get; init; }
    public required string ProcessingType { get; init; }
    public required DateTimeOffset ProcessedTimestamp { get; init; }
    public required string ProcessedPath { get; init; }
    public required string ProcessedHash { get; init; }
    public Dictionary<string, object> ProcessingParameters { get; init; } = new();
}

public sealed class AuditLogEntry
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string EvidenceId { get; init; }
    public required AuditAction Action { get; init; }
    public required string Actor { get; init; }
    public string? Details { get; init; }
    public string? Hash { get; init; }
    public string? Signature { get; init; }
    public string? ProcessedEvidenceId { get; init; }
    public string? ExportId { get; init; }
}

public sealed class ChainOfCustodyReport
{
    public required string EvidenceId { get; init; }
    public required string OriginalFileName { get; init; }
    public required string CaseNumber { get; init; }
    public required DateTimeOffset IngestTimestamp { get; init; }
    public required DateTimeOffset GeneratedTimestamp { get; init; }
    public required string GeneratedBy { get; init; }
    public required string MachineIdentifier { get; init; }
    public required HashSet OriginalHashes { get; init; }
    public required bool CurrentIntegrityValid { get; init; }
    public required List<CustodyEntry> CustodyEntries { get; init; }
    public required List<ProcessedEvidence> ProcessedVersions { get; init; }
    public required List<AuditLogEntry> AuditLog { get; init; }
    public required string Signature { get; init; }
    public required string PublicKey { get; init; }
    public string? ReportSignature { get; set; }
}

public sealed class EvidenceExport
{
    public required string ExportId { get; init; }
    public required string EvidenceId { get; init; }
    public required DateTimeOffset ExportTimestamp { get; init; }
    public required string ExportedBy { get; init; }
    public required string ExportPath { get; init; }
    public required List<string> Files { get; init; }
    public required bool IntegrityValid { get; init; }
    public required string Signature { get; init; }
}

public sealed class EvidenceConfiguration
{
    public string? StorePath { get; init; }
    public bool EnableEncryption { get; init; } = false;
    public bool RequireDualControl { get; init; } = false;
    public int MaxFileSizeMb { get; init; } = 10240; // 10GB default
}

public enum AuditAction
{
    Ingested,
    Processed,
    Exported,
    IntegrityVerified,
    IntegrityCheckFailed,
    Accessed,
    Modified,
    Deleted
}

// Exceptions
public class EvidenceNotFoundException : Exception
{
    public EvidenceNotFoundException(string message) : base(message) { }
}

public class IntegrityException : Exception
{
    public IntegrityException(string message) : base(message) { }
}