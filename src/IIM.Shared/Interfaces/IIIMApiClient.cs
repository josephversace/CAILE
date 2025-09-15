using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Defines the contract for the client-side API service that communicates with the backend.
    /// </summary>
    public interface IIIMApiClient
    {
        Task<List<Workspace>> GetWorkspacesAsync();

        Task<Workspace> GetWorkspaceAsync(Guid workspaceId);

        Task<List<VirtualFile>> GetFilesAsync(Guid workspaceId);

        Task<VirtualFile> GetFileAsync(Guid fileId);

        Task<string> InitiateFileUploadAsync(Guid workspaceId, string fileName, string path, long fileSize, string fileHash);
    }
}
