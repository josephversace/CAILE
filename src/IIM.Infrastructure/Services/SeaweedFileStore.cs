using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Configuration;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Storage;

public sealed class SeaweedFileStore : IFileStore
{
	private readonly HttpClient _http;
	private readonly ILogger<SeaweedFileStore> _logger;

	public SeaweedFileStore(
		HttpClient http,
		CaileConfig config,
		ILogger<SeaweedFileStore> logger)
	{
		_http = http;
		_logger = logger;

		var filerUrl = config.SeaweedFS?.FilerUrl
			?? throw new InvalidOperationException("SeaweedFS:FilerUrl is not configured.");

		_http.BaseAddress = new Uri(filerUrl.TrimEnd('/') + "/");
	}

	// ------------------------------
	// Public API
	// ------------------------------

	public async Task WriteAsync(
		string collection,
		string objectKey,
		Stream data,
		CancellationToken ct = default)
	{
		Validate(collection, objectKey);

		await EnsureCollectionAsync(collection, ct);

		var path = $"{collection}/{objectKey}";

		using var form = new MultipartFormDataContent();

		var fileContent = new StreamContent(data);
		fileContent.Headers.ContentType =
			new MediaTypeHeaderValue("application/octet-stream");

		// SeaweedFS filer expects a named part
		form.Add(fileContent, "file", objectKey);

		using var resp = await _http.PostAsync(path, form, ct);

		if (!resp.IsSuccessStatusCode)
		{
			var error = await resp.Content.ReadAsStringAsync(ct);
			throw new InvalidOperationException(
				$"SeaweedFS WRITE failed ({resp.StatusCode}) for '{path}': {error}");
		}
	}

	public async Task<byte[]> ReadAsync(
		string collection,
		string objectKey,
		CancellationToken ct = default)
	{
		Validate(collection, objectKey);

		var path = $"{collection}/{objectKey}";

		using var resp = await _http.GetAsync(path, ct);

		if (!resp.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(
				$"SeaweedFS READ failed ({resp.StatusCode}) for '{path}'.");
		}

		return await resp.Content.ReadAsByteArrayAsync(ct);
	}

	public async Task DeleteAsync(
		string collection,
		string objectKey,
		CancellationToken ct = default)
	{
		Validate(collection, objectKey);

		var path = $"{collection}/{objectKey}";

		using var resp = await _http.DeleteAsync(path, ct);

		if (!resp.IsSuccessStatusCode &&
			resp.StatusCode != HttpStatusCode.NotFound)
		{
			throw new InvalidOperationException(
				$"SeaweedFS DELETE failed ({resp.StatusCode}) for '{path}'.");
		}
	}

	public async Task<bool> ExistsAsync(
		string collection,
		string objectKey,
		CancellationToken ct = default)
	{
		Validate(collection, objectKey);

		var path = $"{collection}/{objectKey}";

		using var req = new HttpRequestMessage(HttpMethod.Get, path);
		req.Headers.Range = new RangeHeaderValue(0, 0);

		using var resp = await _http.SendAsync(
			req,
			HttpCompletionOption.ResponseHeadersRead,
			ct);

		return resp.IsSuccessStatusCode;
	}

	public async Task PromoteAsync(
		string sourceCollection,
		string destinationCollection,
		string objectKey,
		CancellationToken ct = default)
	{
		Validate(sourceCollection, objectKey);
		Validate(destinationCollection, objectKey);

		await EnsureCollectionAsync(destinationCollection, ct);

		var sourcePath = $"{sourceCollection}/{objectKey}";
		var destinationPath = $"{destinationCollection}/{objectKey}";

		var url = $"{sourcePath}?mv=/{destinationPath}";

		using var req = new HttpRequestMessage(HttpMethod.Post, url);

		using var resp = await _http.SendAsync(req, ct);

		if (!resp.IsSuccessStatusCode)
		{
			var error = await resp.Content.ReadAsStringAsync(ct);
			throw new InvalidOperationException(
				$"SeaweedFS PROMOTE failed ({resp.StatusCode}) {sourcePath} → {destinationPath}: {error}");
		}
	}

	// ------------------------------
	// Internals
	// ------------------------------

	private async Task EnsureCollectionAsync(
		string collection,
		CancellationToken ct)
	{
		using var req = new HttpRequestMessage(
			HttpMethod.Put,
			$"{collection}/?mkdir=true");

		using var resp = await _http.SendAsync(req, ct);

		if (!resp.IsSuccessStatusCode &&
			resp.StatusCode != HttpStatusCode.Conflict)
		{
			var error = await resp.Content.ReadAsStringAsync(ct);
			throw new InvalidOperationException(
				$"Failed to create collection '{collection}': {resp.StatusCode} {error}");
		}
	}

	private static void Validate(string collection, string objectKey)
	{
		if (string.IsNullOrWhiteSpace(collection))
			throw new ArgumentException("Collection is required.", nameof(collection));

		if (string.IsNullOrWhiteSpace(objectKey))
			throw new ArgumentException("Object key is required.", nameof(objectKey));

		// Remove this check to allow nested paths like "abc123/file.pdf"
		// if (collection.Contains('/') || objectKey.Contains('/'))
		//     throw new ArgumentException("Collection and objectKey must be single segments.");

		// Only validate collection is a single segment
		if (collection.Contains('/'))
			throw new ArgumentException("Collection must be a single segment.", nameof(collection));
	}
}
