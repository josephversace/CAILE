using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Plugins.Security.Implementations;

public class RateLimitedHttpClient : ISecureHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RateLimitedHttpClient> _logger;
    private readonly SemaphoreSlim _rateLimiter;

    public RateLimitedHttpClient(HttpClient httpClient, ILogger<RateLimitedHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _rateLimiter = new SemaphoreSlim(5, 5); // Allow 5 concurrent requests
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            return JsonSerializer.Deserialize<T>(response);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
{
    await _rateLimiter.WaitAsync(cancellationToken);
    try
    {
        return await _httpClient.SendAsync(request, cancellationToken);
    }
    finally
    {
        _rateLimiter.Release();
    }
}

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken cancellationToken = default)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(responseJson);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            return await _httpClient.GetByteArrayAsync(url, cancellationToken);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}
