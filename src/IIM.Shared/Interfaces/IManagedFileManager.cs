using IIM.Shared.Models.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// A high-level orchestrator for managing the lifecycle of files.
    /// This acts as a transitional interface, combining ingestion and processing logic
    /// that will eventually be broken into more specialized services.
    /// </summary>
    public interface IManagedFileManager
    {
        /// <summary>
        /// Ingests a new file into the system from a stream. This is the primary method for adding new evidence.
        /// It handles hashing, deduplication, storage, and metadata creation.
        /// </summary>
        /// <param name="stream">The data stream of the file to ingest.</param>
        /// <param name="virtualFile">A VirtualFile object containing all context-specific metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The newly created and saved VirtualFile entity.</returns>
        Task<VirtualFile> IngestFileAsync(Stream stream, VirtualFile virtualFile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes an existing file using a provided stream processor function, creating a new file as a result.
        /// </summary>
        /// <param name="virtualFileId">The ID of the VirtualFile to process.</param>
        /// <param name="processor">A function that takes the input stream and returns the processed stream.</param>
        /// <param name="processingType">A string describing the type of processing being performed (e.g., "OCR", "TRANSCRIBE").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A new VirtualFile record representing the processed output.</returns>
        Task<VirtualFile> ProcessFileAsync(Guid virtualFileId, Func<Stream, Task<Stream>> processor, string processingType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies the integrity of a stored file by recalculating its hash and comparing it
        // to the hash of its linked StoredFile.
        /// </summary>
        /// <param name="virtualFileId">The ID of the VirtualFile to verify.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the hashes match, otherwise false.</returns>
        Task<bool> VerifyIntegrityAsync(Guid virtualFileId, CancellationToken cancellationToken = default);
    }
}

