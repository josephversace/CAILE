using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace IIM.Application.Urls;

public interface ISearchService
{
	Task<List<SearchResult>> SearchAsync(string query, int limit = 5, CancellationToken ct = default);
}


public record SearchResult(
	string Url,
	string? Title,
	string? Snippet,
	double Score,
	string? Engine,
	string? Category,
	DateTime? Published
);

public class SearXngResponse
{
	public List<SearXngResult>? Results { get; set; }
	public string? Query { get; set; }
	public int? NumberOfResults { get; set; }
}

public class SearXngResult
{
	public string? Title { get; set; }
	public string? Url { get; set; }
	public string? Content { get; set; }        // The snippet/description
	public double Score { get; set; }
	public string? Engine { get; set; }          // Which engine returned this
	public string? Category { get; set; }        // news, general, images, etc.

	[JsonPropertyName("publishedDate")]
	public string? PublishedDate { get; set; }   // Some engines return this

	[JsonPropertyName("img_src")]
	public string? ImageSource { get; set; }     // For image results
}



public class SearXngService : ISearchService
{
	private readonly HttpClient _httpClient;
	private readonly CaileConfig _config;
	private readonly ILogger<SearXngService> _logger;
	private readonly AsyncRetryPolicy<List<SearchResult>> _retryPolicy;

	public SearXngService(HttpClient httpClient, CaileConfig config, ILogger<SearXngService> logger)
	{
		_httpClient = httpClient;
		_config = config;
		_logger = logger;

		// BROWSER HEADERS: SearXNG often returns 429 if these are missing
		_httpClient.DefaultRequestHeaders.Clear();
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
		_httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
		_httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
		_httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest"); // Often used to bypass simple bot filters

		// RETRY POLICY: Wait 2s, then 4s, then 8s if 429 occurs
		_retryPolicy = Policy<List<SearchResult>>
			.Handle<HttpRequestException>(ex => ex.StatusCode == HttpStatusCode.TooManyRequests)
			.WaitAndRetryAsync(3,
				retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
				(exception, timeSpan, retryCount, context) =>
				{
					_logger.LogWarning("SearXNG Rate Limited (429). Retry {Count} in {Delay}ms", retryCount, timeSpan.TotalMilliseconds);
				});
	}

	public async Task<List<SearchResult>> SearchAsync(string query, int limit = 5, CancellationToken ct = default)
	{
		return await _retryPolicy.ExecuteAsync(async () =>
		{
			var engines = string.Join(",", _config.SearXNG.DefaultEngines);
			var url = $"{_config.SearXNG.BaseUrl}/search?q={Uri.EscapeDataString(query)}&format=json&engines={engines}&language={_config.SearXNG.Language}";

			var response = await _httpClient.GetAsync(url, ct);

			// Manual check so we can log the body if it fails
			if (response.StatusCode == HttpStatusCode.TooManyRequests)
			{
				var errorBody = await response.Content.ReadAsStringAsync(ct);
				_logger.LogError("Rate Limit Details: {Body}", errorBody);
				throw new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests);
			}

			response.EnsureSuccessStatusCode();

			var data = await response.Content.ReadFromJsonAsync<SearXngResponse>(cancellationToken: ct);
			if (data?.Results == null) return new List<SearchResult>();

			return data.Results.Take(limit).Select(MapToSearchResult).ToList();

		});
	}

	// Mapping
	private SearchResult MapToSearchResult(SearXngResult result)
	{
		DateTime? published = null;
		if (!string.IsNullOrEmpty(result.PublishedDate))
		{
			DateTime.TryParse(result.PublishedDate, out var parsed);
			published = parsed;
		}

		return new SearchResult(
			Url: result.Url ?? "",
			Title: result.Title,
			Snippet: result.Content,
			Score: result.Score,
			Engine: result.Engine,
			Category: result.Category,
			Published: published
		);
	}

}