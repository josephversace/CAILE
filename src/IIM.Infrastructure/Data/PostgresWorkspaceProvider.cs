using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data
{
    public class PostgresWorkspaceProvider : IWorkspaceProvider
    {
        private readonly FileDbContext _dbContext;
        private readonly ILogger<PostgresWorkspaceProvider> _logger;

        public PostgresWorkspaceProvider(
            FileDbContext dbContext,
            ILogger<PostgresWorkspaceProvider> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        #region Virtual File Operations

        public async Task<VirtualFile?> GetVirtualFileByIdAsync(Guid virtualFileId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.VirtualFiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == virtualFileId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virtual file by ID {VirtualFileId}", virtualFileId);
                throw;
            }
        }

        public async Task<IEnumerable<VirtualFile>> GetVirtualFilesByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.VirtualFiles
                    .Where(f => f.WorkspaceId == workspaceId)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virtual files for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        public async Task<VirtualFile> CreateVirtualFileAsync(VirtualFile virtualFile, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure ID is set
                if (virtualFile.Id == Guid.Empty)
                {
                    virtualFile.Id = Guid.NewGuid();
                }

                // Set timestamps
                virtualFile.CreatedAt = DateTimeOffset.UtcNow;
                virtualFile.UpdatedAt = null;

                _dbContext.VirtualFiles.Add(virtualFile);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created virtual file {VirtualFileId} in workspace {WorkspaceId}",
                    virtualFile.Id, virtualFile.WorkspaceId);

                return virtualFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating virtual file {FileName} in workspace {WorkspaceId}",
                    virtualFile.FileName, virtualFile.WorkspaceId);
                throw;
            }
        }

        public async Task<VirtualFile> UpdateVirtualFileAsync(VirtualFile virtualFile, CancellationToken cancellationToken = default)
        {
            try
            {
                virtualFile.UpdatedAt = DateTimeOffset.UtcNow;

                _dbContext.VirtualFiles.Update(virtualFile);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated virtual file {VirtualFileId}", virtualFile.Id);

                return virtualFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating virtual file {VirtualFileId}", virtualFile.Id);
                throw;
            }
        }

        public async Task DeleteVirtualFileAsync(Guid virtualFileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var virtualFile = await _dbContext.VirtualFiles
                    .FirstOrDefaultAsync(f => f.Id == virtualFileId, cancellationToken);

                if (virtualFile == null)
                {
                    _logger.LogWarning("Virtual file {VirtualFileId} not found for deletion", virtualFileId);
                    return;
                }

                _dbContext.VirtualFiles.Remove(virtualFile);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted virtual file {VirtualFileId}", virtualFileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting virtual file {VirtualFileId}", virtualFileId);
                throw;
            }
        }

        #endregion

        #region Stored File Operations

        public async Task<bool> StoredFileExistsAsync(string hash, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.StoredFiles
                    .AnyAsync(f => f.Hash == hash, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if stored file exists with hash {Hash}", hash);
                throw;
            }
        }

        public async Task<StoredFile?> GetStoredFileByHashAsync(string hash, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.StoredFiles
                    .Include(sf => sf.ClassificationTags)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Hash == hash, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stored file by hash {Hash}", hash);
                throw;
            }
        }

        public async Task<StoredFile> CreateStoredFileAsync(StoredFile storedFile, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate hash is provided
                if (string.IsNullOrWhiteSpace(storedFile.Hash))
                {
                    throw new ArgumentException("StoredFile hash cannot be null or empty", nameof(storedFile));
                }

                _dbContext.StoredFiles.Add(storedFile);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created stored file with hash {Hash} and size {FileSize}",
                    storedFile.Hash, storedFile.FileSize);

                return storedFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stored file with hash {Hash}", storedFile.Hash);
                throw;
            }
        }

        #endregion

        #region Folder Operations

        public async Task<VirtualFolder> CreateFolderAsync(VirtualFolder folder, CancellationToken cancellationToken = default)
        {
            try
            {
                // Note: This assumes you have a VirtualFolder entity and DbSet
                // You may need to create this entity if it doesn't exist
                _dbContext.VirtualFolders.Add(folder);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created virtual folder {FolderName} at path {Path}",
                    folder.Name, folder.Path);

                return folder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating virtual folder {FolderName} at path {Path}",
                    folder.Name, folder.Path);
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetFolderContentsAsync(Guid workspaceId, string path, CancellationToken cancellationToken = default)
        {
            try
            {
                var results = new List<object>();

                // Get virtual files in this path
                var files = await _dbContext.VirtualFiles
                    .Where(f => f.WorkspaceId == workspaceId && f.Path == path)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                results.AddRange(files);

                // Get virtual folders in this path (if you have VirtualFolder entity)
                if (_dbContext.VirtualFolders != null)
                {
                    var folders = await _dbContext.VirtualFolders
                        .Where(f => f.Path == path)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    results.AddRange(folders);
                }

                _logger.LogDebug("Retrieved {FileCount} files and {FolderCount} folders from workspace {WorkspaceId} path {Path}",
                    files.Count, results.Count - files.Count, workspaceId, path);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder contents for workspace {WorkspaceId} at path {Path}",
                    workspaceId, path);
                throw;
            }
        }

        #endregion

        #region Additional Helper Methods (Optional)

        /// <summary>
        /// Gets virtual files by path within a workspace
        /// </summary>
        public async Task<IEnumerable<VirtualFile>> GetVirtualFilesByPathAsync(Guid workspaceId, string path, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.VirtualFiles
                    .Where(f => f.WorkspaceId == workspaceId && f.Path.StartsWith(path))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virtual files by path {Path} in workspace {WorkspaceId}",
                    path, workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Gets count of virtual files in a workspace
        /// </summary>
        public async Task<int> GetVirtualFileCountAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.VirtualFiles
                    .CountAsync(f => f.WorkspaceId == workspaceId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virtual file count for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Gets all stored files that are not referenced by any virtual files (orphaned files)
        /// </summary>
        public async Task<IEnumerable<StoredFile>> GetOrphanedStoredFilesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all stored file hashes
                var storedFileHashes = await _dbContext.StoredFiles
                    .Select(sf => sf.Hash)
                    .ToListAsync(cancellationToken);

                // Get all referenced hashes from virtual files
                var referencedHashes = await _dbContext.VirtualFiles
                    .Where(vf => !string.IsNullOrEmpty(vf.StoredFileHash))
                    .Select(vf => vf.StoredFileHash)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Find orphaned hashes
                var orphanedHashes = storedFileHashes.Except(referencedHashes);

                // Return orphaned stored files
                return await _dbContext.StoredFiles
                    .Where(sf => orphanedHashes.Contains(sf.Hash))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orphaned stored files");
                throw;
            }
        }

        #endregion

        #region Cleanup and Maintenance

        /// <summary>
        /// Removes orphaned stored files that are no longer referenced
        /// </summary>
        public async Task<int> CleanupOrphanedStoredFilesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var orphanedFiles = await GetOrphanedStoredFilesAsync(cancellationToken);
                var orphanedList = orphanedFiles.ToList();

                if (orphanedList.Any())
                {
                    _dbContext.StoredFiles.RemoveRange(orphanedList);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Cleaned up {Count} orphaned stored files", orphanedList.Count);
                }

                return orphanedList.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned stored files");
                throw;
            }
        }

        #endregion
    }
}