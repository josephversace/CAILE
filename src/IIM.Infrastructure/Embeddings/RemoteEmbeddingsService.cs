using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


using IIM.Shared.Interfaces;
using IIM.Shared.Models;

namespace IIM.Infrastructure.Embeddings;



/// <summary>
/// Remote embedding service implementation connecting to FastAPI service in WSL
/// </summary>
public class RemoteEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RemoteEmbeddingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _defaultTextModel = "all-MiniLM-L6-v2";
    private readonly string _defaultImageModel = "clip-ViT-B-32";

    /// <summary>
    /// Initializes a new instance of the RemoteEmbeddingService
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for creating clients</param>
    /// <param name="logger">Logger for diagnostic output</param>
    public RemoteEmbeddingService(IHttpClientFactory httpClientFactory, ILogger<RemoteEmbeddingService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("embed");
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc/>
    public async Task<float[]> EmbedTextAsync(string text, string? model = null, CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                text = text,
                model = model ?? _defaultTextModel
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/embed/text",
                request,
                _jsonOptions,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Embedding service error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(_jsonOptions, ct);

            _logger.LogDebug("Generated text embedding with {Dimensions} dimensions",
                result?.Embedding?.Length ?? 0);

            return result?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text embedding");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<float[]>> BatchEmbedTextAsync(List<string> texts, string? model = null, CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                texts = texts,
                model = model ?? _defaultTextModel
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/embed/batch/text",
                request,
                _jsonOptions,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Batch embedding error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<BatchEmbeddingResponse>(_jsonOptions, ct);

            _logger.LogDebug("Generated {Count} text embeddings", result?.Embeddings?.Count ?? 0);

            return result?.Embeddings ?? new List<float[]>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating batch text embeddings");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<float[]> EmbedImageAsync(byte[] imageData, string? model = null, CancellationToken ct = default)
    {
        try
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageData), "file", "image.jpg");

            if (!string.IsNullOrEmpty(model))
            {
                content.Add(new StringContent(model), "model");
            }
            else
            {
                content.Add(new StringContent(_defaultImageModel), "model");
            }

            var response = await _httpClient.PostAsync("/embed/image", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Image embedding error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(_jsonOptions, ct);

            _logger.LogDebug("Generated image embedding with {Dimensions} dimensions",
                result?.Embedding?.Length ?? 0);

            return result?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image embedding");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<float[]>> BatchEmbedImagesAsync(List<byte[]> images, string? model = null, CancellationToken ct = default)
    {
        try
        {
            var content = new MultipartFormDataContent();

            for (int i = 0; i < images.Count; i++)
            {
                content.Add(new ByteArrayContent(images[i]), $"files", $"image_{i}.jpg");
            }

            content.Add(new StringContent(model ?? _defaultImageModel), "model");

            var response = await _httpClient.PostAsync("/embed/batch/images", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Batch image embedding error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<BatchEmbeddingResponse>(_jsonOptions, ct);

            _logger.LogDebug("Generated {Count} image embeddings", result?.Embeddings?.Count ?? 0);

            return result?.Embeddings ?? new List<float[]>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating batch image embeddings");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<float[]> EmbedMultiModalAsync(string? text, byte[]? imageData, string? model = null, CancellationToken ct = default)
    {
        try
        {
            if (text == null && imageData == null)
            {
                throw new ArgumentException("At least one of text or image must be provided");
            }

            var content = new MultipartFormDataContent();

            if (!string.IsNullOrEmpty(text))
            {
                content.Add(new StringContent(text), "text");
            }

            if (imageData != null)
            {
                content.Add(new ByteArrayContent(imageData), "file", "image.jpg");
            }

            content.Add(new StringContent(model ?? "clip-ViT-B-32"), "model");

            var response = await _httpClient.PostAsync("/embed/multimodal", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Multi-modal embedding error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(_jsonOptions, ct);

            _logger.LogDebug("Generated multi-modal embedding with {Dimensions} dimensions",
                result?.Embedding?.Length ?? 0);

            return result?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating multi-modal embedding");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<EmbeddingModelInfo>> GetAvailableModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/models", ct);

            if (!response.IsSuccessStatusCode)
            {
                return new List<EmbeddingModelInfo>();
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingModelsResponse>(_jsonOptions, ct);

            return result?.Models ?? new List<EmbeddingModelInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available models");
            return new List<EmbeddingModelInfo>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> LoadModelAsync(string modelName, CancellationToken ct = default)
    {
        try
        {
            var request = new { model = modelName };
            var response = await _httpClient.PostAsJsonAsync("/models/load", request, _jsonOptions, ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading model {Model}", modelName);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UnloadModelAsync(string modelName, CancellationToken ct = default)
    {
        try
        {
            var request = new { model = modelName };
            var response = await _httpClient.PostAsJsonAsync("/models/unload", request, _jsonOptions, ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading model {Model}", modelName);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<EmbeddingServiceInfo> GetInfoAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/info", ct);

            if (!response.IsSuccessStatusCode)
            {
                return new EmbeddingServiceInfo {
                    Status= "error",
                    LoadedModels= new List<EmbeddingModelInfo>(),
                    TotalMemoryUsage= 0,
                    RequestsProcessed= 0,
                    AverageLatencyMs= 0,
                    Version= "unknown"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingServiceInfo>(_jsonOptions, ct);

            return result ?? new EmbeddingServiceInfo
            {
                Status = "unknown",
                LoadedModels = new List<EmbeddingModelInfo>(),
                TotalMemoryUsage = 0,
                RequestsProcessed = 0,
                AverageLatencyMs = 0,
                Version = "unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service info");
            return new EmbeddingServiceInfo {
                Status= "error",
                LoadedModels= new List<EmbeddingModelInfo>(),
                TotalMemoryUsage= 0,
                RequestsProcessed= 0,
                AverageLatencyMs= 0,
                Version= "unknown"
            };
        }
    }
}