// ============================================
// File: src/IIM.Core/Services/IEvidenceManager.cs
// Purpose: Evidence management interface - uses existing Models
// ============================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Models;

namespace IIM.Core.Services
{
    /// <summary>
    /// Interface for managing digital evidence with chain of custody.

    /// </summary>
    public interface IEvidenceManager
    {
        // Evidence Ingestion
        Task<Evidence> IngestEvidenceAsync(Stream stream, string fileName, EvidenceMetadata metadata, CancellationToken cancellationToken = default);
        Task<Evidence> IngestEvidenceAsync(string filePath, EvidenceMetadata metadata, CancellationToken cancellationToken = default);

        // Evidence Processing
        Task<ProcessedEvidence> ProcessEvidenceAsync(string evidenceId, string processingType, Func<Stream, Task<Stream>> processor, CancellationToken cancellationToken = default);

        // Integrity & Verification
        Task<bool> VerifyIntegrityAsync(string evidenceId, CancellationToken cancellationToken = default);
        Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(string evidenceId, CancellationToken cancellationToken = default);

        // Evidence Export
        Task<EvidenceExport> ExportEvidenceAsync(string evidenceId, string exportPath, CancellationToken cancellationToken = default);

        // Audit & Logging
        Task<List<AuditLogEntry>> GetAuditLogAsync(string evidenceId, CancellationToken cancellationToken = default);
        Task LogAccessAsync(string evidenceId, string action, string userId, CancellationToken cancellationToken = default);

        // Evidence Retrieval
        Task<Evidence?> GetEvidenceAsync(string evidenceId, CancellationToken cancellationToken = default);
        Task<Stream> GetEvidenceStreamAsync(string evidenceId, CancellationToken cancellationToken = default);
        Task<List<Evidence>> ListEvidenceAsync(string? caseNumber = null, CancellationToken cancellationToken = default);

        Task<List<Evidence>> GetEvidenceByCaseAsync(string caseId, CancellationToken cancellationToken = default);

    }


}