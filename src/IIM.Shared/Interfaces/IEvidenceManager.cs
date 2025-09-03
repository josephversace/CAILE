using IIM.Shared.Enums;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IManagedFileManager
    {
        Task<EvidenceExport> ExportFilesAsync(string fileId, string exportPath, CancellationToken cancellationToken = default);
        Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(string fileId, CancellationToken cancellationToken = default);
        Task<List<AuditEvent>> GetAuditLogAsync(string fileId, CancellationToken cancellationToken = default);
        Task<ManagedFile?> GetFilesAsync(string fileId, CancellationToken cancellationToken = default);
        Task<List<ManagedFile>> GetFilesByWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);
        Task<Stream> GetFileStreamAsync(string fileId, CancellationToken cancellationToken = default);
        Task<ManagedFile> IngestFileAsync(Stream stream, string fileName, FileMetadata metadata, CancellationToken cancellationToken = default);
        Task<ManagedFile> IngestFileAsync(string filePath, FileMetadata metadata, CancellationToken cancellationToken = default);
        Task<List<ManagedFile>> ListFilesAsync(string? workspaceNumber = null, CancellationToken cancellationToken = default);
        Task LogAccessAsync(string fileId, string action, string userId, CancellationToken cancellationToken = default);
        Task<ProcessedFile> ProcessFileAsync(string fileId, string processingType, Func<Stream, Task<Stream>> processor, CancellationToken cancellationToken = default);
        Task<ManagedFile> RegisterPendingFileAsync(ManagedFile file, CancellationToken cancellationToken = default);
        Task UpdateFileStatusAsync(string fileId, EvidenceStatus status, CancellationToken cancellationToken = default);
        Task<bool> VerifyIntegrityAsync(string fileId, CancellationToken cancellationToken = default);
    }
}