using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Configuration;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Docling;

public class DoclingService : IDoclingService
{
	private readonly HttpClient _http;
	private readonly ILogger<DoclingService> _logger;
	private readonly string _baseUrl;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public DoclingService(
		HttpClient http,
		CaileConfig config,
		ILogger<DoclingService> logger)
	{
		_http = http;
		_logger = logger;

		var dc = config.Docling ?? throw new InvalidOperationException("Missing Docling configuration.");
		_baseUrl = dc.BaseUrl ?? throw new InvalidOperationException("Docling BaseUrl not configured.");

		if (dc.TimeoutSeconds > 0)
			_http.Timeout = TimeSpan.FromSeconds(dc.TimeoutSeconds);
	}

	public async Task<DoclingDocument> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
	{
		using var content = new MultipartFormDataContent();
		var streamContent = new StreamContent(fileStream);
		content.Add(streamContent, "file", fileName);

		var endpoint = $"{_baseUrl.TrimEnd('/')}/upload";

		var response = await _http.PostAsync(endpoint, content, ct);

		if (!response.IsSuccessStatusCode)
		{
			var errorBody = await response.Content.ReadAsStringAsync(ct);
			_logger.LogError("Docling parse failed: {Status} - {Body}",
				response.StatusCode, errorBody);
			throw new InvalidOperationException(
				$"Docling parse failed with status {response.StatusCode}.");
		}

		var document = await response.Content.ReadFromJsonAsync<DoclingDocument>(ct)
			?? throw new InvalidOperationException("Docling returned null response.");

		return document;
	}
	
}