using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Configuration;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Storage;

/// <summary>
/// SeaweedFS Filer-backed storage provider.
/// </summary>
public class SeaweedFileStore : IFileStore
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

	private static string Normalize(string path)
	{
		while (path.StartsWith("/"))
			path = path[1..];
		return path;
	}

	public async Task<byte[]> ReadAsync(string path, CancellationToken ct = default)
	{
		path = Normalize(path);
		_logger.LogDebug("SeaweedFS READ: {Path}", path);

		var response = await _http.GetAsync(path, ct);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("SeaweedFS READ failed: {Status} for {Path}", response.StatusCode, path);
			throw new InvalidOperationException(
				$"SeaweedFS READ failed ({response.StatusCode}) for '{path}'.");
		}

		return await response.Content.ReadAsByteArrayAsync(ct);
	}

	public async Task<string> WriteAsync(byte[] data, string path, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(path))
			throw new ArgumentException("File path must be provided.", nameof(path));

		path = Normalize(path);
		_logger.LogDebug("SeaweedFS WRITE: {Path} ({Size} bytes)", path, data.Length);

		using var content = new ByteArrayContent(data);
		content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

		var response = await _http.PostAsync(path, content, ct);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("SeaweedFS WRITE failed: {Status} for {Path}", response.StatusCode, path);
			throw new InvalidOperationException(
				$"SeaweedFS WRITE failed ({response.StatusCode}) for '{path}'.");
		}

		return path;
	}

	public async Task<string> WriteAsync(Stream data, string path, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(path))
			throw new ArgumentException("File path must be provided.", nameof(path));

		path = Normalize(path);
		_logger.LogDebug("SeaweedFS WRITE (stream): {Path}", path);

		using var content = new StreamContent(data);
		content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

		var response = await _http.PostAsync(path, content, ct);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("SeaweedFS WRITE failed: {Status} for {Path}", response.StatusCode, path);
			throw new InvalidOperationException(
				$"SeaweedFS WRITE failed ({response.StatusCode}) for '{path}'.");
		}

		return path;
	}

	public async Task DeleteAsync(string path, CancellationToken ct = default)
	{
		path = Normalize(path);
		_logger.LogDebug("SeaweedFS DELETE: {Path}", path);

		var response = await _http.DeleteAsync(path, ct);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogWarning("SeaweedFS DELETE failed: {Status} for {Path}", response.StatusCode, path);
			throw new InvalidOperationException(
				$"SeaweedFS DELETE failed ({response.StatusCode}) for '{path}'.");
		}
	}

	public async Task<bool> ExistsAsync(string path, CancellationToken ct = default)
	{
		path = Normalize(path);

		var request = new HttpRequestMessage(HttpMethod.Head, path);
		var response = await _http.SendAsync(request, ct);

		return response.IsSuccessStatusCode;
	}
}