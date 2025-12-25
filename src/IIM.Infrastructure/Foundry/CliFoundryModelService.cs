using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using IIM.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Foundry;

public sealed class CliFoundryModelService : IFoundryModelService
{
	private readonly HttpClient _http;
	private readonly ILogger<CliFoundryModelService> _log;

	private string? _baseUrl;
	private readonly Dictionary<string, string> _aliasToVariant = new(StringComparer.OrdinalIgnoreCase);

	private static readonly Regex RootUrlRegex =
	new(@"(https?://[0-9\.]+:[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public CliFoundryModelService(HttpClient http, ILogger<CliFoundryModelService> log)
	{
		_http = http;
		_log = log;
	}

	// ─────────────────────────────────────────────────────────────
	// Public surface
	// ─────────────────────────────────────────────────────────────

	public string BaseUrl => _baseUrl
		?? throw new InvalidOperationException("Foundry service not initialized");

	public string InferenceEndpoint => $"{BaseUrl}/v1";

	public async Task EnsureInitializedAsync(CancellationToken ct = default)
	{
		if (_baseUrl != null)
			return;

		_log.LogInformation("Restarting Foundry via CLI");

		await RestartFoundryAsync(ct);

		// Check if already running
		var existingUrl = DetectViaCli();

		if (existingUrl != null && await IsHealthyAsync(existingUrl, ct))
		{
			_baseUrl = existingUrl;
			_log.LogInformation("Foundry already running at {Url}", _baseUrl);
		}
		else
		{
			// Need to start/restart
			await RestartFoundryAsync(ct);
			_baseUrl = DetectViaCli()
				?? throw new InvalidOperationException("Failed to detect Foundry URL after start");
		}

		await RefreshCacheAsync(ct);

		_log.LogInformation("Foundry initialized at {Url}", _baseUrl);
	}

	public async Task LoadModelAsync(string alias, CancellationToken ct = default)
	{
		EnsureReady();

		var variantId = await ResolveVariantAsync(alias, ct);

		_log.LogInformation("Loading model {Variant}", variantId);

		var url = $"{BaseUrl}/openai/load/{Uri.EscapeDataString(variantId)}?ttl=999999";
		var resp = await _http.GetAsync(url, ct);

		if (!resp.IsSuccessStatusCode)
		{
			var body = await resp.Content.ReadAsStringAsync(ct);
			throw new InvalidOperationException(
				$"Failed loading model {variantId}: {resp.StatusCode}\n{body}");
		}
	}

	public async Task UnloadModelAsync(string alias, bool force = false, CancellationToken ct = default)
	{
		EnsureReady();

		var variantId = await ResolveVariantAsync(alias, ct);
		var url = $"{BaseUrl}/openai/unload/{Uri.EscapeDataString(variantId)}";

		if (force)
			url += "?force=true";

		_log.LogInformation("Unloading model {Variant}", variantId);

		await _http.GetAsync(url, ct);
	}

	public async Task<IReadOnlyList<(string Alias, string ModelId)>> GetCachedModelsAsync()
	{
		await RefreshCacheAsync();
		return _aliasToVariant.Select(kv => (kv.Key, kv.Value)).ToList();
	}

	public Task<IReadOnlyList<FoundryModelDto>> GetAvailableModelsDtoAsync(CancellationToken ct = default)
		=> throw new NotSupportedException("CLI-only service");

	public Task<IReadOnlyList<FoundryModelDto>> GetCachedModelsDtoAsync(CancellationToken ct = default)
		=> throw new NotSupportedException("CLI-only service");

	public Task<IReadOnlyList<FoundryModelDto>> GetLoadedModelsDtoAsync(CancellationToken ct = default)
		=> throw new NotSupportedException("CLI-only service");

	public Task<IReadOnlyList<FoundryModelDto>> GetAllWithStatusDtoAsync(CancellationToken ct = default)
		=> throw new NotSupportedException("CLI-only service");

	public Task<string> GetLoadedModelForAliasAsync(string alias, CancellationToken ct = default)
		=> ResolveVariantAsync(alias, ct);

	public Task ApplyTemplateAsync(ModelTemplateDto template, CancellationToken ct = default)
		=> throw new NotSupportedException("Template orchestration handled elsewhere");

	// ─────────────────────────────────────────────────────────────
	// Core mechanics
	// ─────────────────────────────────────────────────────────────

	private void EnsureReady()
	{
		if (_baseUrl == null)
			throw new InvalidOperationException("Foundry not initialized");
	}

	private async Task RestartFoundryAsync(CancellationToken ct)
	{
		_log.LogInformation("Stopping Foundry service...");
		await RunCliAsync("service stop", ct, timeout: TimeSpan.FromSeconds(10), ignoreErrors: true);

		// Give it a moment to fully stop
		await Task.Delay(1000, ct);

		_log.LogInformation("Starting Foundry service...");

		// service start doesn't exit - it runs in background
		// Just fire it and don't wait
		await RunCliAsync("service start", ct, waitForExit: false);

		// Wait for service to become healthy
		for (int i = 0; i < 30; i++)
		{
			await Task.Delay(500, ct);

			var url = DetectViaCli();
			if (url != null && await IsHealthyAsync(url, ct))
			{
				_log.LogInformation("Foundry service started.");
				return;
			}
		}

		throw new TimeoutException("Foundry service did not start within 15 seconds.");
	}

	private async Task<bool> IsHealthyAsync(string baseUrl, CancellationToken ct)
	{
		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(TimeSpan.FromSeconds(2));

			// Foundry uses /openai/status, not /health
			var response = await _http.GetAsync($"{baseUrl}/openai/status", cts.Token);
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	private async Task<string> ResolveServiceUrlAsync(CancellationToken ct)
	{
		var output = await RunCliAsync("service status", ct);

		// Example:
		// 🟢 Model management service is running on http://127.0.0.1:50912/openai/status
		var marker = "http://";
		var idx = output.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

		if (idx < 0)
			throw new InvalidOperationException("Unable to detect Foundry service URL");

		var end = output.IndexOf("/openai", idx, StringComparison.OrdinalIgnoreCase);
		if (end < 0)
			throw new InvalidOperationException("Malformed Foundry status output");

		return output.Substring(idx, end - idx);
	}

	private async Task RefreshCacheAsync(CancellationToken ct = default)
	{
		_aliasToVariant.Clear();

		var output = await RunCliAsync("cache ls", ct);

		foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			// Skip header lines, empty lines, or lines that don't look like model entries
			var trimmed = line.Trim();
			if (string.IsNullOrEmpty(trimmed))
				continue;

			// Remove any emoji/icon at the start (they're multi-byte UTF-8)
			// Look for the pattern: <icon> <alias> <variant>
			// The variant typically ends with :1, :2, etc.

			// Find the variant (ends with :N pattern like ":1")
			var colonIdx = trimmed.LastIndexOf(':');
			if (colonIdx < 0)
				continue;

			// Check if what follows the colon is a number
			var afterColon = trimmed[(colonIdx + 1)..];
			if (!int.TryParse(afterColon.Trim(), out _))
				continue;

			// Now parse: everything after first whitespace-run is the content
			// Remove leading emoji by finding first ASCII letter
			var startIdx = 0;
			for (int i = 0; i < trimmed.Length; i++)
			{
				var c = trimmed[i];
				if (char.IsLetterOrDigit(c) && c < 128)
				{
					startIdx = i;
					break;
				}
			}

			var content = trimmed[startIdx..].Trim();

			// Split into parts - last part is the variant ID
			var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2)
				continue;

			var variant = parts[^1];  // Last part: e.g., "Mistral-7B-Instruct-v0-2-vitis-npu:1"
			var alias = string.Join(' ', parts[..^1]);  // Everything else: e.g., "mistral-7b-v0.2"

			if (!string.IsNullOrEmpty(alias) && !string.IsNullOrEmpty(variant))
			{
				_aliasToVariant[alias] = variant;
				_log.LogDebug("Cached model: {Alias} → {Variant}", alias, variant);
			}
		}

		_log.LogInformation("Cached models: {Count}", _aliasToVariant.Count);
	}

	private async Task<string> ResolveVariantAsync(string alias, CancellationToken ct)
	{
		if (_aliasToVariant.TryGetValue(alias, out var variant))
			return variant;

		_log.LogInformation("Downloading model {Alias}", alias);
		await RunCliAsync($"model download {alias}", ct, timeout: TimeSpan.FromMinutes(30));

		await RefreshCacheAsync(ct);

		if (_aliasToVariant.TryGetValue(alias, out variant))
			return variant;

		throw new InvalidOperationException($"Model {alias} not found after download");
	}

	// ─────────────────────────────────────────────────────────────
	// CLI helper
	// ─────────────────────────────────────────────────────────────

	private string? DetectViaCli()
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = "foundry",
				Arguments = "service status",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var proc = Process.Start(psi);
			if (proc == null)
				return null;

			var output = proc.StandardOutput.ReadToEnd();
			proc.WaitForExit(1500);

			_log.LogDebug("Foundry CLI output: {Output}", output);

			var match = RootUrlRegex.Match(output);
			return match.Success ? Normalize(match.Groups[1].Value) : null;
		}
		catch
		{
			return null;
		}
	}

	private static string Normalize(string url) =>
	RootUrlRegex.Match(url).Success
		? RootUrlRegex.Match(url).Groups[1].Value.TrimEnd('/')
		: url.TrimEnd('/');

	private async Task<string> RunCliAsync(
		string args,
		CancellationToken ct,
		TimeSpan? timeout = null,
		bool ignoreErrors = false,
		bool waitForExit = true)
	{
		timeout ??= TimeSpan.FromSeconds(30);

		var psi = new ProcessStartInfo
		{
			FileName = "foundry",
			Arguments = args,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var proc = Process.Start(psi);
		if (proc == null)
			throw new InvalidOperationException("Failed to start foundry process");

		if (!waitForExit)
		{
			// Fire and forget - just wait a bit for startup
			await Task.Delay(500, ct);
			return "";
		}

		var outputBuilder = new StringBuilder();
		var errorBuilder = new StringBuilder();

		// Read async to avoid deadlock
		var outputTask = proc.StandardOutput.ReadToEndAsync(ct);
		var errorTask = proc.StandardError.ReadToEndAsync(ct);

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(timeout.Value);

		try
		{
			await proc.WaitForExitAsync(cts.Token);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			// Timeout - kill the process
			try { proc.Kill(); } catch { }

			if (!ignoreErrors)
				throw new TimeoutException($"Foundry command timed out: {args}");
		}

		var output = await outputTask;
		var error = await errorTask;

		if (proc.ExitCode != 0 && !ignoreErrors)
		{
			_log.LogWarning("Foundry command failed: {Args}\n{Error}", args, error);
		}

		return output;
	}



}
