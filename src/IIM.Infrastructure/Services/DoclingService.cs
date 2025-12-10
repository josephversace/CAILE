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

	public async Task<DoclingResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
	{
		using var content = new MultipartFormDataContent();
		var streamContent = new StreamContent(fileStream);
		content.Add(streamContent, "file", fileName);

		// Request both markdown and json output
		content.Add(new StringContent("[\"md\", \"json\", \"text\"]"), "to_formats");
		content.Add(new StringContent("true"), "do_ocr");

		var endpoint = $"{_baseUrl.TrimEnd('/')}/v1/convert/file";

		try
		{
			var response = await _http.PostAsync(endpoint, content, ct);

			if (!response.IsSuccessStatusCode)
			{
				var errorBody = await response.Content.ReadAsStringAsync(ct);
				_logger.LogError("Docling parse failed: {Status} {Reason} - {Body}",
					response.StatusCode, response.ReasonPhrase, errorBody);

				throw new InvalidOperationException(
					$"Docling parse failed with status {response.StatusCode}.");
			}

			var doclingResponse = await response.Content.ReadFromJsonAsync<DoclingResponse>(JsonOptions, ct);

			if (doclingResponse == null)
				throw new InvalidOperationException("Docling returned null response.");

			if (!doclingResponse.IsSuccess)
			{
				_logger.LogWarning("Docling returned status {Status} with errors: {Errors}",
					doclingResponse.Status, string.Join(", ", doclingResponse.Errors ?? []));
			}

			return MapToResult(doclingResponse);
		}
		catch (Exception ex) when (ex is not InvalidOperationException)
		{
			_logger.LogError(ex, "Error calling Docling");
			throw;
		}
	}

	private static DoclingResult MapToResult(DoclingResponse response)
	{
		var doc = response.Document;
		var jsonDoc = doc?.JsonContent;

		return new DoclingResult
		{
			// Primary content
			Markdown = doc?.MarkdownContent ?? "",
			Text = doc?.TextContent ?? "",
			Html = doc?.HtmlContent ?? "",

			// Structured document
			Document = jsonDoc,

			// Metadata
			Status = response.Status,
			ProcessingTimeSeconds = response.ProcessingTime,
			Errors = response.Errors ?? [],

			// Computed stats
			PageCount = jsonDoc?.Pages?.Count ?? 0,
			TextBlockCount = jsonDoc?.Texts?.Count ?? 0,
			TableCount = jsonDoc?.Tables?.Count ?? 0,
			PictureCount = jsonDoc?.Pictures?.Count ?? 0
		};
	}
}