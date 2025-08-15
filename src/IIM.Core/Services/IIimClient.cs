using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace IIM.Core.Services
{
    // API Client belongs in Core as it's business logic
    public interface IIimClient
    {
        Task<string> GetStatusAsync();
        Task<bool> ConnectAsync(string endpoint);
        Task DisconnectAsync();

        Task<bool> HealthAsync();
        Task<GpuInfo> GetGpuInfoAsync();
        Task<T> GenerateAsync<T>(string modelId, object input);
        Task StopAllAsync();
    }

    public class IimClient : IIimClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IimClient> _logger;

        public IimClient(HttpClient httpClient, ILogger<IimClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> GetStatusAsync()
        {
            var response = await _httpClient.GetStringAsync("/api/status");
            return response;
        }

        public async Task<bool> ConnectAsync(string endpoint)
        {
            _httpClient.BaseAddress = new Uri(endpoint);
            return true;
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<bool> HealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<GpuInfo> GetGpuInfoAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<GpuInfo>("/v1/gpu");
            return response ?? new GpuInfo();
        }

        public async Task<T> GenerateAsync<T>(string modelId, object input)
        {
            var response = await _httpClient.PostAsJsonAsync("/v1/generate", new { modelId, input });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<T>();
            return result!;
        }

        public async Task StopAllAsync()
        {
            await _httpClient.PostAsync("/v1/models/stop-all", null);
        }
    }
}
