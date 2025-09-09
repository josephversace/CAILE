
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
    public interface IIIMApiClient
    {
        Task<InvestigationSession> CreateSessionAsync(CreateSessionRequest request);
        Task<bool> EnsureWslAsync();
        Task<ModelInfo[]> GetAvailableModelsAsync();
        Task<SystemStatus> GetSystemStatusAsync();
        Task<WslStatus> GetWslStatusAsync();
        Task<ManagedFile> IngestFileAsync(Stream file, string fileName, FileMetadata metadata);
        Task<bool> IsApiAvailableAsync();
        Task<bool> LoadModelAsync(string modelId);
        Task<InvestigationResponse> ProcessQueryAsync(string sessionId, string query);

        Task<InitiateFileUploadResponse> InitiateEvidenceUploadAsync(
        InitiateFileUploadRequest request,
        CancellationToken cancellationToken = default);

        Task<ConfirmFileUploadResponse> ConfirmFileUploadAsync(
        ConfirmFileUploadRequest request,
        CancellationToken cancellationToken = default);

        Task<Settings> GetSettingsAsync();
        Task UpdateSettingsAsync(Settings settings);

        Task<TestConnectionResult> TestMinIOConnectionAsync(string endpoint);


    }
}
