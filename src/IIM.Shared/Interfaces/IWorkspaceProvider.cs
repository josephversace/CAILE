using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Provides a low-level, data-centric interface for interacting with the virtual file system's metadata.
    /// This is the contract for the database implementation (e.g., PostgreSQL).
    /// </summary>
    public interface IWorkspaceProvider
    {
        // --- Virtual File Operations ---
        Task<VirtualFile?> GetVirtualFileByIdAsync(Guid virtualFileId, CancellationToken cancellationToken = default);
        Task<IEnumerable<VirtualFile>> GetVirtualFilesByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<VirtualFile> CreateVirtualFileAsync(VirtualFile virtualFile, CancellationToken cancellationToken = default);
        Task<VirtualFile> UpdateVirtualFileAsync(VirtualFile virtualFile, CancellationToken cancellationToken = default);
        Task DeleteVirtualFileAsync(Guid virtualFileId, CancellationToken cancellationToken = default);

        // --- Stored File (Content) Operations ---
        Task<bool> StoredFileExistsAsync(string hash, CancellationToken cancellationToken = default);
        Task<StoredFile?> GetStoredFileByHashAsync(string hash, CancellationToken cancellationToken = default);
        Task<StoredFile> CreateStoredFileAsync(StoredFile storedFile, CancellationToken cancellationToken = default);

        // --- Folder Operations ---
        Task<VirtualFolder> CreateFolderAsync(VirtualFolder folder, CancellationToken cancellationToken = default);
        Task<IEnumerable<object>> GetFolderContentsAsync(Guid workspaceId, string path, CancellationToken cancellationToken = default);
    }
}

