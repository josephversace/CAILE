using IIM.Shared.Enums;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

/// <summary>
/// A transitional, high-level interface for orchestrating file management operations.
/// This interface is being refactored to use the new VirtualFile/StoredFile model.
/// Its responsibilities will eventually be absorbed by more specific application services.
/// </summary>
public interface IManagedFileManager
{
    /// <summary>
    /// Ingests a new file into the system. This involves creating a StoredFile (if new) and a VirtualFile.
    /// </summary>
    /// <param name="stream">The file content stream.</param>
    /// <param name="virtualFile">A VirtualFile object pre-populated with context-specific metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created and saved VirtualFile entity.</returns>
    Task<VirtualFile> IngestFileAsync(Stream stream, VirtualFile virtualFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single virtual file by its unique ID.
    /// </summary>
    Task<VirtualFile?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of all virtual files within a given workspace.
    /// </summary>
    Task<IEnumerable<VirtualFile>> GetFilesByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a virtual file.
    /// </summary>
    Task UpdateFileStatusAsync(Guid fileId, FileUploadStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a file using a provided stream processor function and saves the output as a new StoredFile/VirtualFile.
    /// </summary>
    Task<VirtualFile> ProcessFileAsync(Guid originalFileId, string processingType, Func<Stream, Task<Stream>> processor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a file and its chain of custody report to a specified path.
    /// </summary>
    Task ExportFileAsync(Guid fileId, string exportPath, CancellationToken cancellationToken = default);
}

