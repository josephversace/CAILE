using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

/// <summary>
/// Defines the contract for a low-level data provider that manages the metadata
/// of the virtual file system (files and folders) within workspaces.
/// </summary>
public interface IWorkspaceProvider
{


    Task<VirtualFolder> CreateFolderAsync(string path, string folderName, Guid workspaceId);
    Task<IEnumerable<object>> GetFolderContentsAsync(string path, Guid workspaceId);

    // === File Operations ===

    /// <summary>
    /// Creates a new virtual file reference in the database.
    /// </summary>
    Task<VirtualFile> CreateVirtualFileAsync(VirtualFile virtualFile);

    /// <summary>
    /// Retrieves a specific virtual file by its unique ID.
    /// </summary>
    Task<VirtualFile?> GetVirtualFileByIdAsync(Guid virtualFileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all virtual files within a specific workspace.
    /// </summary>
    Task<IEnumerable<VirtualFile>> GetVirtualFilesByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing virtual file record.
    /// </summary>
    Task UpdateVirtualFileAsync(VirtualFile virtualFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a StoredFile with the given hash already exists.
    /// </summary>
    Task<bool> StoredFileExistsAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new StoredFile record.
    /// </summary>
    Task CreateStoredFileAsync(StoredFile storedFile, CancellationToken cancellationToken = default);

    // === Generic Operations ===

    /// <summary>
    /// Deletes a virtual file or folder reference by its ID.
    /// </summary>
    Task DeleteReferenceAsync(Guid id);
}

