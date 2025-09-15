using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace IIM.Desktop.Services
{
    /// <summary>
    /// Concrete implementation of the API client for the desktop application.
    /// Handles HTTP communication and JSON serialization/deserialization.
    /// </summary>
    public class IIMApiClient : IIIMApiClient
    {
        private readonly HttpClient _httpClient;

        public IIMApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Workspace>> GetWorkspacesAsync(CancellationToken ct)
        {
            // Assuming an endpoint like GET /api/workspaces
            return await _httpClient.GetFromJsonAsync<List<Workspace>>("api/workspaces");
        }

        public async Task<Workspace> GetWorkspaceAsync(Guid workspaceId, CancellationToken ct)
        {
            // Assuming an endpoint like GET /api/workspaces/{id}
            return await _httpClient.GetFromJsonAsync<Workspace>($"api/workspaces/{workspaceId}");
        }

        public async Task<List<VirtualFile>> GetFilesAsync(Guid workspaceId, CancellationToken ct)
        {
            // Assuming an endpoint like GET /api/workspaces/{id}/files
            return await _httpClient.GetFromJsonAsync<List<VirtualFile>>($"api/workspaces/{workspaceId}/files");
        }

        public async Task<VirtualFile> GetFileAsync(Guid fileId, CancellationToken ct)
        {
            // Assuming an endpoint like GET /api/files/{id}
            return await _httpClient.GetFromJsonAsync<VirtualFile>($"api/files/{fileId}");
        }

        public async Task<InitiateFileUploadResponse> InitiateFileUploadAsync(Guid workspaceId, string fileName, string path, long fileSize, string fileHash, CancellationToken ct)
        {
            var request = new
            {
                WorkspaceId = workspaceId,
                FileName = fileName,
                Path = path,
                FileSize = fileSize,
                FileHash = fileHash,
                CancellationToken = ct
            };

            // Assuming an endpoint like POST /api/files/initiate-upload
            var response = await _httpClient.PostAsJsonAsync("api/files/initiate-upload", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<InitiateFileUploadResponse>();
            return result;
        }

        public Task<VirtualFile?> ConfirmFileUploadAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    // A helper DTO for deserializing the response from the initiate upload endpoint.
    public class UploadInitiationResponse
    {
        public string UploadUrl { get; set; }
        public Guid FileId { get; set; }
    }
}
