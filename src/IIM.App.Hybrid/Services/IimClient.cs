
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using IIM.Shared.Models;

namespace IIM.App.Hybrid.Services;
public sealed class IimClient
{
    private readonly HttpClient _http;
    public IimClient()
    {
        _http = new HttpClient { BaseAddress = new Uri("http://localhost:5080") };
    }

    public async Task<bool> HealthAsync()
    {
        try { var s = await _http.GetStringAsync("/healthz"); return s.Contains("ok", StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }

    public Task<GpuInfo?> GetGpuInfoAsync() => _http.GetFromJsonAsync<GpuInfo>("/v1/gpu");

    public Task<HttpResponseMessage> StartModelAsync(string id, string modelPath, string engine, string provider)
        => _http.PostAsync($"/v1/run?id={Uri.EscapeDataString(id)}&modelPath={Uri.EscapeDataString(modelPath)}&engine={engine}&provider={provider}", null);

    public async Task<string> GenerateAsync(string id, string prompt)
    {
        var res = await _http.PostAsync($"/v1/generate?id={Uri.EscapeDataString(id)}&prompt={Uri.EscapeDataString(prompt)}", null);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return json != null && json.TryGetValue("text", out var t) ? t?.ToString() ?? string.Empty : string.Empty;
    }

    public Task<HttpResponseMessage> StopAllAsync() => _http.PostAsync("/v1/stop-all", null);

    // RAG helpers
    public async Task<bool> RagUploadAsync(List<IBrowserFile> files, bool sentenceMode)
    {
        using var content = new MultipartFormDataContent();
        foreach (var f in files)
        {
            var stream = f.OpenReadStream(20 * 1024 * 1024); // up to 20MB per file
            content.Add(new StreamContent(stream), "files", f.Name);
        }
        var mode = sentenceMode ? "sentences" : "fixed";
        var res = await _http.PostAsync($"/rag/upload?mode={mode}", content);
        return res.IsSuccessStatusCode;
    }

    public async Task<(string answer, List<string>? citations)> RagQueryAsync(string query, int k)
    {
        var res = await _http.PostAsJsonAsync("/agents/rag/query", new { query, k });
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<RagResponse>() ?? new RagResponse();
        return (json.answer ?? string.Empty, json.citations ?? new List<string>());
    }

    private sealed class RagResponse
    {
        public string? answer { get; set; }
        public List<string>? citations { get; set; }
    }
}
