using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Blake3;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using SmartReader;

namespace IIM.Application.Urls
{
	public class WebTools
	{
		private readonly IPlaywrightService _playwright;
		private readonly ISearchService _search;
		private readonly IFileStore _fileStore;
		private readonly CaileConfig _config;
		private readonly ILogger<WebTools> _logger;

		public WebTools(
			IPlaywrightService playwright,
			ISearchService search,
			IFileStore fileStore,
			CaileConfig config,
			ILogger<WebTools> logger)
		{
			_playwright = playwright;
			_search = search;
			_fileStore = fileStore;
			_config = config;
			_logger = logger;
		}

		[Description("Ingests a URL, extracts article content, and returns it for analysis.")]
		public async Task<string> IngestUrlAsync(string url, string workspaceId)
		{
			var capturedAtUtc = DateTime.UtcNow;
			Article? article = null;
			string extractionMethod = "";

			// Attempt 1: Playwright (handles JS-heavy sites)
			try
			{
				var capture = await _playwright.CaptureAsync(url);

				if (!string.IsNullOrWhiteSpace(capture.RawHtml))
				{
					article = Reader.ParseArticle(url, capture.RawHtml);
					extractionMethod = "playwright";
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Playwright capture failed for {Url}, trying direct fetch", url);
			}

			// Attempt 2: SmartReader direct fetch (fallback)
			if (article == null || !article.IsReadable)
			{
				try
				{
					article = await Reader.ParseArticleAsync(url);  // Fetches directly
					extractionMethod = "direct";
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Direct fetch also failed for {Url}", url);
				}
			}

			// Both failed
			if (article == null || !article.IsReadable || string.IsNullOrWhiteSpace(article.TextContent))
			{
				return FormatFailure(url, capturedAtUtc, "All extraction methods failed");
			}

			return FormatArticle(url, capturedAtUtc, article, extractionMethod);
		}

		private string FormatArticle(string url, DateTime capturedAtUtc, Article article, string method)
		{
			var sb = new StringBuilder();

			sb.AppendLine($"GROUNDING: Content captured at {capturedAtUtc:yyyy-MM-dd HH:mm:ss} UTC.");
			sb.AppendLine($"SOURCE: {url}");
			sb.AppendLine($"EXTRACTION: SUCCESS (via {method})");

			if (!string.IsNullOrEmpty(article.Title))
				sb.AppendLine($"TITLE: {article.Title}");

			if (!string.IsNullOrEmpty(article.Author))
				sb.AppendLine($"AUTHOR: {article.Author}");

			if (article.PublicationDate.HasValue)
				sb.AppendLine($"PUBLISHED: {article.PublicationDate:yyyy-MM-dd}");

			sb.AppendLine();
			sb.AppendLine("CONTENT:");
			sb.AppendLine(article.TextContent);

			return sb.ToString();
		}

		private string FormatArticle(string url, DateTime capturedAtUtc, Article article)
		{
			var sb = new StringBuilder();

			sb.AppendLine($"GROUNDING: Content captured at {capturedAtUtc:yyyy-MM-dd HH:mm:ss} UTC.");
			sb.AppendLine($"SOURCE: {url}");
			sb.AppendLine($"EXTRACTION: SUCCESS");

			if (!string.IsNullOrEmpty(article.Title))
				sb.AppendLine($"TITLE: {article.Title}");

			if (!string.IsNullOrEmpty(article.Author))
				sb.AppendLine($"AUTHOR: {article.Author}");

			if (article.PublicationDate.HasValue)
				sb.AppendLine($"PUBLISHED: {article.PublicationDate:yyyy-MM-dd}");

			if (!string.IsNullOrEmpty(article.Language))
				sb.AppendLine($"LANGUAGE: {article.Language}");

			sb.AppendLine();
			sb.AppendLine("CONTENT:");
			sb.AppendLine(article.TextContent);

			return sb.ToString();
		}

		private string FormatFailure(string url, DateTime capturedAtUtc, string reason)
		{
			return $"""
                GROUNDING: Attempted capture at {capturedAtUtc:yyyy-MM-dd HH:mm:ss} UTC.
                SOURCE: {url}
                EXTRACTION: FAILED
                REASON: {reason}
                
                WARNING: No content was retrieved. Do not summarize or describe this URL - the content is unavailable.
                """;
		}

		[Description("A multi-purpose web tool. Use this for: " +
				 "1. Web Searching: Finding information, reviews, news, or answers to questions. " +
				 "2. URL Ingestion: Extracting and reading content from specific links provided in the text. " +
				 "3. Research: Gathering data on any topic by browsing online sources. " +
				 "Pass the full user message to 'originalMessage' to ensure links are preserved.")]
		public async Task<string> WebSearchAsync(
	[Description("The specific search terms or keywords extracted from the user's request.")] string query,
	[Description("The full, unmodified user message. Required for link extraction.")] string originalMessage,
	string workspaceId = "")
		{
			var capturedAtUtc = DateTime.UtcNow;
			var urlRegex = new Regex(@"https?://[^\s""'<>]+", RegexOptions.IgnoreCase);
			var matches = urlRegex.Matches(originalMessage);

			List<IngestResult> results = new();
			List<SearchResult> rawSearchResults = new(); // Keep the original search results
			string searchContext;

			if (matches.Any())
			{
				// EXTRACT AND INGEST PATH
				var foundUrls = matches
					.Select(m => m.Value.TrimEnd('.', ',', ')', ']'))
					.Distinct()
					.ToList();

				searchContext = $"Extracted {foundUrls.Count} URL(s) from message";
				var tasks = foundUrls.Select(url => IngestWithMetadataAsync(url, workspaceId));
				results = (await Task.WhenAll(tasks)).ToList();
			}
			else
			{
				// SEARCH AND INGEST PATH
				searchContext = $"Web search for: \"{query}\"";
				rawSearchResults = (await _search.SearchAsync(query, limit: 5)).ToList();

				if (!rawSearchResults.Any())
				{
					return FormatNoResults(query, capturedAtUtc, "Search returned no results");
				}

				var tasks = rawSearchResults.Select(r => IngestWithMetadataAsync(r.Url, workspaceId));
				results = (await Task.WhenAll(tasks)).ToList();
			}

			// Separate successes from failures
			var successes = results.Where(r => r.Success).ToList();
			var failures = results.Where(r => !r.Success).ToList();

			foreach (var failure in failures)
			{
				_logger.LogWarning("Ingestion failed for {Url}: {Reason}", failure.Url, failure.Error);
			}

			// FULL CONTENT AVAILABLE
			if (successes.Any())
			{
				return FormatSuccessfulResults(query, searchContext, successes, failures.Count, capturedAtUtc);
			}

			// ALL INGESTION FAILED - but we might have useful snippets
			if (rawSearchResults.Any())
			{
				return FormatSnippetFallback(query, rawSearchResults, capturedAtUtc);
			}

			return FormatNoResults(query, capturedAtUtc,
				$"Attempted {results.Count} sources but all extractions failed");
		}

		private string FormatSnippetFallback(string query, List<SearchResult> searchResults, DateTime capturedAt)
		{
			var sb = new StringBuilder();

			sb.AppendLine($"GROUNDING: Search performed at {capturedAt:yyyy-MM-dd HH:mm:ss} UTC");
			sb.AppendLine($"QUERY: {query}");
			sb.AppendLine($"NOTE: Full page retrieval failed. Using search result snippets only.");
			sb.AppendLine($"CONFIDENCE: REDUCED - snippets may be incomplete or out of context");
			sb.AppendLine(new string('=', 60));
			sb.AppendLine();

			foreach (var result in searchResults)
			{
				sb.AppendLine($"SOURCE: {result.Url}");

				if (!string.IsNullOrEmpty(result.Title))
					sb.AppendLine($"TITLE: {result.Title}");

				if (!string.IsNullOrEmpty(result.Snippet))
				{
					sb.AppendLine($"SNIPPET: {result.Snippet}");
				}

				sb.AppendLine();
			}

			sb.AppendLine(new string('-', 40));
			sb.AppendLine("NOTE: Answer based on search snippets. Full article content was unavailable.");

			return sb.ToString();
		}

	

		private record IngestResult(
			bool Success,
			string Url,
			string? Content,
			string? Title,
			string? Author,
			DateTime? Published,
			string? Error
		);

		private async Task<IngestResult> IngestWithMetadataAsync(string url, string workspaceId)
		{
			try
			{
				var capture = await _playwright.CaptureAsync(url);
				Article? article = null;

				// Try Playwright HTML first
				if (!string.IsNullOrWhiteSpace(capture.RawHtml))
				{
					article = Reader.ParseArticle(url, capture.RawHtml);
				}

				// Fallback to direct fetch
				if (article == null || !article.IsReadable)
				{
					try
					{
						article = await Reader.ParseArticleAsync(url);
					}
					catch { }
				}

				if (article == null || !article.IsReadable || string.IsNullOrWhiteSpace(article.TextContent))
				{
					return new IngestResult(false, url, null, null, null, null, "Could not extract readable content");
				}

				return new IngestResult(
					Success: true,
					Url: url,
					Content: article.TextContent,
					Title: article.Title,
					Author: article.Author,
					Published: article.PublicationDate,
					Error: null
				);
			}
			catch (Exception ex)
			{
				return new IngestResult(false, url, null, null, null, null, ex.Message);
			}
		}

		private string FormatSuccessfulResults(
			string query,
			string searchContext,
			List<IngestResult> successes,
			int failureCount,
			DateTime capturedAt)
		{
			var sb = new StringBuilder();

			sb.AppendLine($"GROUNDING: Content captured at {capturedAt:yyyy-MM-dd HH:mm:ss} UTC");
			sb.AppendLine($"QUERY: {query}");
			sb.AppendLine($"SOURCES: {successes.Count} retrieved successfully" +
				(failureCount > 0 ? $" ({failureCount} sources unavailable)" : ""));
			sb.AppendLine(new string('=', 60));
			sb.AppendLine();

			foreach (var result in successes)
			{
				sb.AppendLine($"SOURCE: {result.Url}");

				if (!string.IsNullOrEmpty(result.Title))
					sb.AppendLine($"TITLE: {result.Title}");

				if (!string.IsNullOrEmpty(result.Author))
					sb.AppendLine($"AUTHOR: {result.Author}");

				if (result.Published.HasValue)
					sb.AppendLine($"PUBLISHED: {result.Published:yyyy-MM-dd}");

				sb.AppendLine();
				sb.AppendLine(result.Content);
				sb.AppendLine();
				sb.AppendLine(new string('-', 40));
			}

			return sb.ToString();
		}

		private string FormatNoResults(string query, DateTime capturedAt, string reason)
		{
			return $"""
        GROUNDING: Attempted at {capturedAt:yyyy-MM-dd HH:mm:ss} UTC
        QUERY: {query}
        RESULT: FAILED
        REASON: {reason}

        WARNING: No content was retrieved. Do not fabricate an answer.
        """;
		}

	}
}