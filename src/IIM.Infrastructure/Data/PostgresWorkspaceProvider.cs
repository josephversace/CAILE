// Located in: src/IIM.Infrastructure/Data/PostgresWorkspaceProvider.cs

using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data
{
    public class PostgresWorkspaceProvider : IWorkspaceProvider
    {
        private readonly FileDbContext _dbContext;

        public PostgresWorkspaceProvider(FileDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<object>> GetFolderContentsAsync(string path)
        {
            var files = await _dbContext.Files
                .Where(f => f.Path == path)
                .Select(f => new FileReference(f.Id.ToString(), f.FileName, f.Path, f.FileSize, f.StorageKey))
                .ToListAsync();

            // This assumes you have a DbSet<WorkspaceFolder> for your folders.
            // If not, you will need to add one to your FileDbContext.
            var folders = await _dbContext.Folders
                .Where(f => f.Path == path)
                .Select(f => new FolderReference(f.Id.ToString(), f.Name, f.Path))
                .ToListAsync();

            return files.Cast<object>().Concat(folders);
        }

        public async Task<FileReference?> GetFileReferenceAsync(string fileId)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
            {
                return null;
            }

            var file = await _dbContext.Files
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == fileGuid);

            if (file is null)
            {
                return null;
            }

            return new FileReference(file.Id.ToString(), file.FileName, file.Path, file.FileSize, file.StorageKey);
        }


        public async Task<FileReference> CreateFileReferenceAsync(string path, string fileName, long size, string storageKey)
        {
            var newFile = new ManagedFile
            {
                Id = Guid.NewGuid().ToString(),
                OriginalFileName = fileName,
                 = path,
                FileSize = size,
                StorageKey = storageKey,
                // Set other necessary properties here...
                // e.g., UploadedOn, UploadedBy, etc.
            };

            _dbContext.Files.Add(newFile);
            await _dbContext.SaveChangesAsync();

            return new FileReference(
                newFile.Id.ToString(),
                newFile.FileName,
                newFile.Path,
                newFile.FileSize,
                newFile.StorageKey
            );
        }

        public async Task<FolderReference> CreateFolderAsync(string path, string folderName)
        {
            // Implementation for creating a folder in the database.
            // You will need to create a `WorkspaceFolder` entity and a `DbSet<WorkspaceFolder>` 
            // in your `FileDbContext`.
            var newFolder = new WorkspaceFolder
            {
                Id = Guid.NewGuid(),
                Name = folderName,
                Path = path,
            };

            _dbContext.Folders.Add(newFolder);
            await _dbContext.SaveChangesAsync();

            return new FolderReference(newFolder.Id.ToString(), newFolder.Name, newFolder.Path);
        }

        public async Task DeleteReferenceAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
            {
                // Handle invalid ID
                return;
            }

            var fileToDelete = await _dbContext.Files.FindAsync(guid);
            if (fileToDelete != null)
            {
                _dbContext.Files.Remove(fileToDelete);
                await _dbContext.SaveChangesAsync();
                return;
            }

            var folderToDelete = await _dbContext.Folders.FindAsync(guid);
            if (folderToDelete != null)
            {
                // Note: You will also need to handle deleting files and subfolders within this folder.
                _dbContext.Folders.Remove(folderToDelete);
                await _dbContext.SaveChangesAsync();
            }
        }
    }

}