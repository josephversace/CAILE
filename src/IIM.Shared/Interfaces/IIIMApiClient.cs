using IIM.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IIIMApiClient
    {
        Task<InvestigationSession> CreateSessionAsync(CreateSessionRequest request);
        Task<bool> EnsureWslAsync();
        Task<ModelInfo[]> GetAvailableModelsAsync();
        Task<SystemStatus> GetSystemStatusAsync();
        Task<WslStatus> GetWslStatusAsync();
        Task<Evidence> IngestEvidenceAsync(Stream file, string fileName, EvidenceMetadata metadata);
        Task<bool> IsApiAvailableAsync();
        Task<bool> LoadModelAsync(string modelId);
        Task<InvestigationResponse> ProcessQueryAsync(string sessionId, string query);

        Task<InitiateEvidenceUploadResponse> InitiateEvidenceUploadAsync(
        InitiateEvidenceUploadRequest request,
        CancellationToken cancellationToken = default);

        Task<ConfirmEvidenceUploadResponse> ConfirmEvidenceUploadAsync(
        ConfirmEvidenceUploadRequest request,
        CancellationToken cancellationToken = default);

        Task<Settings> GetSettingsAsync();
        Task UpdateSettingsAsync(Settings settings);

        Task<TestConnectionResult> TestMinIOConnectionAsync(string endpoint);


    }
}
