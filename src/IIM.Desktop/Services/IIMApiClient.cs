using IIM.Application.Commands.Investigation;
using IIM.Application.Commands.Models;
using IIM.Shared.DTOs;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;



/// <summary>
/// Main API client for communicating with the IIM backend.
/// Handles all HTTP communication between the desktop client and API server.
/// </summary>
public class IIMApiClient : IIIMApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IIMApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of the IIMApiClient.
    /// </summary>
    /// <param name="httpClient">Configured HttpClient from DI container</param>
    /// <param name="logger">Logger for diagnostic output</param>
    public IIMApiClient(HttpClient httpClient, ILogger<IIMApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // ========================================
    // Investigation endpoints
    // ========================================

    /// <summary>
    /// Creates a new investigation session on the API server.
    /// </summary>
    /// <param name="request">Session creation request containing case details</param>
    /// <returns>Created investigation session with generated ID</returns>
    /// <exception cref="HttpRequestException">Thrown when API call fails</exception>
    public async Task<InvestigationSession> CreateSessionAsync(CreateSessionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/investigation/session", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InvestigationSession>();
    }

    /// <summary>
    /// Processes a query within an investigation session.
    /// Sends the query to the API for RAG processing and returns the response.
    /// </summary>
    /// <param name="sessionId">ID of the active investigation session</param>
    /// <param name="query">User's query text</param>
    /// <returns>Investigation response containing answer and citations</returns>
    /// <exception cref="HttpRequestException">Thrown when API call fails</exception>
    public async Task<InvestigationResponse> ProcessQueryAsync(string sessionId, string query)
    {
        var command = new ProcessInvestigationCommand { SessionId = sessionId, Query = query };
        var response = await _httpClient.PostAsJsonAsync("/api/investigation/query", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InvestigationResponse>();
    }

    // ========================================
    // Evidence endpoints
    // ========================================

    /// <summary>
    /// Ingests evidence file into the system with chain of custody tracking.
    /// Uploads file and metadata to API for processing and storage.
    /// </summary>
    /// <param name="file">File stream containing evidence data</param>
    /// <param name="fileName">Original filename of the evidence</param>
    /// <param name="metadata">Evidence metadata including case number and collection details</param>
    /// <returns>Evidence record with generated ID and hash</returns>
    /// <exception cref="HttpRequestException">Thrown when upload fails</exception>
    public async Task<Evidence> IngestEvidenceAsync(Stream file, string fileName, EvidenceMetadata metadata)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file), "file", fileName);
        content.Add(new StringContent(metadata.CaseNumber), "caseNumber");
        content.Add(new StringContent(metadata.CollectedBy), "collectedBy");

        var response = await _httpClient.PostAsync("/api/evidence/ingest", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Evidence>();
    }

    public async Task<InitiateEvidenceUploadResponse> InitiateEvidenceUploadAsync(
       InitiateEvidenceUploadRequest request,
       CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/evidence/initiate",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InitiateEvidenceUploadResponse>();
    }

    public async Task<ConfirmEvidenceUploadResponse> ConfirmEvidenceUploadAsync(
        ConfirmEvidenceUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/evidence/confirm",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConfirmEvidenceUploadResponse>();
    }

    // ========================================
    // Model endpoints
    // ========================================

    /// <summary>
    /// Retrieves list of available AI models from the API server.
    /// </summary>
    /// <returns>Array of available model information</returns>
    /// <exception cref="HttpRequestException">Thrown when API call fails</exception>
    public async Task<ModelInfo[]> GetAvailableModelsAsync()
    {
        var response = await _httpClient.GetAsync("/api/models/available");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ModelInfo[]>();
    }

    /// <summary>
    /// Requests the API server to load a specific AI model into memory.
    /// </summary>
    /// <param name="modelId">Unique identifier of the model to load</param>
    /// <returns>True if model loaded successfully, false otherwise</returns>
    public async Task<bool> LoadModelAsync(string modelId)
    {
        var command = new LoadModelCommand { ModelId = modelId };
        var response = await _httpClient.PostAsJsonAsync("/api/v1/models/load", command);
        return response.IsSuccessStatusCode;
    }

    // ========================================
    // System endpoints
    // ========================================

    /// <summary>
    /// Gets current system status including memory usage and loaded models.
    /// </summary>
    /// <returns>System status information</returns>
    /// <exception cref="HttpRequestException">Thrown when API call fails</exception>
    public async Task<SystemStatus> GetSystemStatusAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/stats");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SystemStatus>();
    }

    /// <summary>
    /// Checks if the API server is available and responding.
    /// Used for health checks and connection validation.
    /// </summary>
    /// <returns>True if API is available, false otherwise</returns>
    public async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/healthz");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ========================================
    // WSL endpoints
    // ========================================

    /// <summary>
    /// Gets the current status of WSL2 and related services.
    /// </summary>
    /// <returns>WSL status information</returns>
    /// <exception cref="HttpRequestException">Thrown when API call fails</exception>
    public async Task<WslStatus> GetWslStatusAsync()
    {
        var response = await _httpClient.GetAsync("/api/wsl/status");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WslStatus>();
    }

    /// <summary>
    /// Ensures WSL2 is properly configured and running.
    /// Triggers WSL setup on the API server if needed.
    /// </summary>
    /// <returns>True if WSL is ready, false otherwise</returns>
    public async Task<bool> EnsureWslAsync()
    {
        var response = await _httpClient.PostAsync("/api/wsl/ensure", null);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Gets current application settings
    /// </summary>
    public async Task<SettingsDto> GetSettingsAsync()
    {
        var response = await _httpClient.GetAsync("/api/settings");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SettingsDto>()
            ?? new SettingsDto();
    }

    /// <summary>
    /// Updates application settings
    /// </summary>
    public async Task UpdateSettingsAsync(SettingsDto settings)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/settings", settings);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Tests MinIO connection
    /// </summary>
    public async Task<TestConnectionResult> TestMinIOConnectionAsync(string endpoint)
    {
        var request = new { Endpoint = endpoint };
        var response = await _httpClient.PostAsJsonAsync("/api/settings/test/minio", request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TestConnectionResult>()
                ?? new TestConnectionResult { Success = false, Error = "Unknown error" };
        }

        return new TestConnectionResult
        {
            Success = false,
            Error = "Connection test failed"
        };
    }
}

