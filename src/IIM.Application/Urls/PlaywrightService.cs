using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Urls;

public interface IPlaywrightService
{
	Task<WebCaptureResult> CaptureAsync(
		string url,
		bool screencapture = false,
		CancellationToken ct = default);
}

public record WebCaptureResult(
	string AriaSnapshot,
	string Screenshot,
	string RawHtml,
	string PageTitle);

public class PlaywrightService : IPlaywrightService
{
	private readonly HttpClient _httpClient;
	private readonly CaileConfig _config;
	private readonly ILogger<PlaywrightService> _logger;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		MaxDepth = 512
	};


	public PlaywrightService(HttpClient httpClient, CaileConfig config, ILogger<PlaywrightService> logger)
	{
		_httpClient = httpClient;
		_config = config;
		_logger = logger;

		_httpClient.Timeout = Timeout.InfiniteTimeSpan;

	}

	public async Task<WebCaptureResult> CaptureAsync(
		string url,
		bool screencapture = false,
		CancellationToken ct = default)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out _))
			throw new ArgumentException("Invalid URL", nameof(url));

		_logger.LogDebug(
			"Requesting playwright capture for {Url} (screenshot={Screenshot})",
			url,
			screencapture);

		var requestBody = new PlaywrightRenderRequest(
			url,
			"networkidle",
			_config.Playwright.RenderTimeoutSeconds * 1000,
			screencapture
				? new[] { "ariaSnapshot", "screenshot", "html" }
				: new[] { "ariaSnapshot", "html" }
		);

		var response = await _httpClient.PostAsJsonAsync(
			$"{_config.Playwright.BaseUrl}/render",
			requestBody,
			ct);

		response.EnsureSuccessStatusCode();

		var json = await response.Content.ReadAsStringAsync(ct);

		PlaywrightApiResponse data;
		try
		{
			data = JsonSerializer.Deserialize<PlaywrightApiResponse>(json, JsonOptions)
				?? throw new InvalidOperationException("Playwright API returned null response");
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Failed to deserialize Playwright response. Payload length={Length}",
				json.Length);
			throw;
		}

		return new WebCaptureResult(
			data.AriaSnapshot ?? string.Empty,
			screencapture ? data.ScreenshotBase64 ?? string.Empty : string.Empty,
			data.Html ?? string.Empty,
			data.Title ?? "Untitled Page"
		);
	}

	private sealed record PlaywrightRenderRequest(
	string Url,
	string WaitUntil,
	int Timeout,
	string[] Outputs
);

	private class PlaywrightApiResponse
	{
		// 2. Map the new property name from the API
		[JsonPropertyName("ariaSnapshot")]
		public string? AriaSnapshot { get; set; }

		[JsonPropertyName("screenshot")]
		public string? ScreenshotBase64 { get; set; }

		[JsonPropertyName("html")]
		public string? Html { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }
	}
}