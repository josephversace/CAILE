using IIM.Shared.Enums;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IEvidenceManager
    {
        Task<EvidenceExport> ExportEvidenceAsync(string evidenceId, string exportPath, CancellationToken cancellationToken = default);
        Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(string evidenceId, CancellationToken cancellationToken = default);
        Task<List<AuditEvent>> GetAuditLogAsync(string evidenceId, CancellationToken cancellationToken = default);
        Task<Evidence?> GetEvidenceAsync(string evidenceId, CancellationToken cancellationToken = default);
        Task<List<Evidence>> GetEvidenceByCaseAsync(string caseId, CancellationToken cancellationToken = default);
        Task<Stream> GetEvidenceStreamAsync(string evidenceId, CancellationToken cancellationToken = default);
        Task<Evidence> IngestEvidenceAsync(Stream stream, string fileName, EvidenceMetadata metadata, CancellationToken cancellationToken = default);
        Task<Evidence> IngestEvidenceAsync(string filePath, EvidenceMetadata metadata, CancellationToken cancellationToken = default);
        Task<List<Evidence>> ListEvidenceAsync(string? caseNumber = null, CancellationToken cancellationToken = default);
        Task LogAccessAsync(string evidenceId, string action, string userId, CancellationToken cancellationToken = default);
        Task<ProcessedEvidence> ProcessEvidenceAsync(string evidenceId, string processingType, Func<Stream, Task<Stream>> processor, CancellationToken cancellationToken = default);
        Task<Evidence> RegisterPendingEvidenceAsync(Evidence evidence, CancellationToken cancellationToken = default);
        Task UpdateEvidenceStatusAsync(string evidenceId, EvidenceStatus status, CancellationToken cancellationToken = default);
        Task<bool> VerifyIntegrityAsync(string evidenceId, CancellationToken cancellationToken = default);
    }
}