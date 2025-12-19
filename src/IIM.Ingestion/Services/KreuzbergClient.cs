using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services
{
	public interface IKreuzbergClient
	{
		Task<KreuzbergExtractionResult> ExtractAsync(
			byte[] content,
			string fileName,
			string mimetype,
			CancellationToken ct);
	}


	public sealed class KreuzbergClient : IKreuzbergClient
	{
		private readonly HttpClient _http;
		private readonly ILogger<KreuzbergClient> _logger;

		public KreuzbergClient(HttpClient http, ILogger<KreuzbergClient> logger)
		{
			_http = http;
			_logger = logger;
		}

		public async Task<KreuzbergExtractionResult> ExtractAsync(
			byte[] content,
			string fileName,
			string mimetype,
			CancellationToken ct)
		{
			using var form = new MultipartFormDataContent();

			var fileContent = new ByteArrayContent(content.ToArray());
			fileContent.Headers.ContentType =
				new System.Net.Http.Headers.MediaTypeHeaderValue(mimetype);

			form.Add(fileContent, "files", fileName);

			using var response = await _http.PostAsync("/extract", form, ct);


			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(ct);
				_logger.LogError(
					"Kreuzberg error {Status}: {Body}",
					response.StatusCode,
					body
				);

				throw new HttpRequestException(
					$"Kreuzberg returned {(int)response.StatusCode}: {body}");
			}


			var results = await response.Content
	.ReadFromJsonAsync<List<KreuzbergExtractionResult>>(cancellationToken: ct);

			if (results == null || results.Count == 0)
				throw new InvalidOperationException("Empty response from Kreuzberg.");

			return results[0];


		
		}
	}

	public sealed class KreuzbergExtractionResult
	{
		[JsonPropertyName("content")]
		public string Text { get; init; } = string.Empty;

		[JsonPropertyName("mime_type")]
		public string? MimeType { get; init; }

		[JsonPropertyName("metadata")]
		public IDictionary<string, object>? Metadata { get; init; }

		[JsonPropertyName("detected_languages")]
		public List<string>? DetectedLanguages { get; init; }

		[JsonPropertyName("tables")]
		public object? Tables { get; init; }

		[JsonPropertyName("chunks")]
		public object? Chunks { get; init; }

		[JsonPropertyName("images")]
		public object? Images { get; init; }
	}


}
