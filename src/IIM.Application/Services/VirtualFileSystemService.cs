using IIM.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IIM.Application.Services
{
    public class VirtualFileSystemService
    {
        private readonly IWorkspaceProvider _workspaceProvider;
        private readonly IObjectStorageProvider _storageProvider;

        public VirtualFileSystemService(
            IWorkspaceProvider workspaceProvider,
            IObjectStorageProvider storageProvider)
        {
            _workspaceProvider = workspaceProvider;
            _storageProvider = storageProvider;
        }

        public Task<IEnumerable<object>> GetFolderContentsAsync(Guid workspaceId, string path)
        {
            return _workspaceProvider.GetFolderContentsAsync(workspaceId, path);
        }

        public async Task<string> GetDownloadUrlAsync(Guid fileId)
        {
            var virtualFile = await _workspaceProvider.GetVirtualFileByIdAsync(fileId);
            if (virtualFile is null)
            {
                throw new FileNotFoundException("The requested file does not exist.");
            }

            // Assuming "evidence" is your main bucket. This should come from configuration.
            return await _storageProvider.GetPresignedDownloadUrlAsync("evidence", virtualFile.StoredFileHash, TimeSpan.FromMinutes(15));
        }
    }
}

