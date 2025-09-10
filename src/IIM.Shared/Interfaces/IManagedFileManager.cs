using IIM.Shared.Enums;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// A high-level service for orchestrating file management operations. It acts as a bridge
    /// between application logic and the low-level storage and workspace providers.
    /// </summary>
    public interface IManagedFileManager
    {
        /// <summary>
        /// Creates a new file in the system by uploading its content and saving its metadata.
        /// </summary>
        Task<ManagedFile> CreateFileAsync(
            string workspaceId,
            string path,
            string fileName,
            Stream data,
            string createdBy,
            Dictionary<string, string> customMetadata,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the metadata for a file by its ID.
        /// </summary>
        Task<ManagedFile?> GetFileAsync(string fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a stream for the content of a file.
        /// </summary>
        Task<Stream> GetFileStreamAsync(string fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all files within a specific workspace.
        /// </summary>
        Task<IEnumerable<ManagedFile>> GetFilesByWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the processing status of a file.
        /// </summary>
        Task UpdateFileStatusAsync(string fileId, FileProcessingStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a chain of custody report for a file.
        /// </summary>
        Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(string fileId, CancellationToken cancellationToken = default);

        // Methods to be refactored or removed as they are tied to old concepts
        // Task<ProcessedFile> ProcessFileAsync(string fileId, string processingType, Func<Stream, Task<Stream>> processor, CancellationToken cancellationToken = default);
        // Task<FileExport> ExportFilesAsync(string fileId, string exportPath, CancellationToken cancellationToken = default);
    }
}
