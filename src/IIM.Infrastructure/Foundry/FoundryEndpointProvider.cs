using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;

namespace IIM.Infrastructure.Foundry;

public sealed class FoundryEndpointProvider : IFoundryEndpointProvider
{
	private readonly ILogger<FoundryEndpointProvider> _log;
	private readonly HttpClient _http;
	private readonly string? _configured;

	private static readonly Regex RootUrlRegex =
		new(@"(https?://[0-9\.]+:[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public FoundryEndpointProvider(
		ILogger<FoundryEndpointProvider> log,
		HttpClient http,
		IOptions<ModelTemplatesConfig> opts)
	{
		_log = log;
		_http = http;
		_configured = opts.Value.FoundryEndpoint?.TrimEnd('/');
	}

	//---------------------------------------------------------------------
	// API — ALWAYS RETURNS THE TRUE LIVE ENDPOINT
	//---------------------------------------------------------------------
	public string GetBaseUrl()
	{
		// 1. Validate configured endpoint (only accept if service is live)
		if (_configured != null && IsLiveAsync(_configured).GetAwaiter().GetResult())
		{
			_log.LogInformation("Foundry endpoint using configured: {Url}", _configured);
			return Normalize(_configured);
		}

		// 2. Try CLI detection
		var viaCli = DetectViaCli();
		if (!string.IsNullOrWhiteSpace(viaCli))
		{
			_log.LogInformation("Foundry endpoint detected via CLI: {Url}", viaCli);
			return Normalize(viaCli);
		}

		// 3. Nothing found
		throw new InvalidOperationException(
			"Foundry endpoint not found. Service is not running.");
	}

	public async Task<bool> IsOnlineAsync(CancellationToken ct = default)
	{
		try
		{
			var url = GetBaseUrl();
			return await IsLiveAsync(url, ct);
		}
		catch
		{
			return false;
		}
	}

	//---------------------------------------------------------------------
	// INTERNAL VALIDATION
	//---------------------------------------------------------------------
	private async Task<bool> IsLiveAsync(string url, CancellationToken ct = default)
	{
		try
		{
			var resp = await _http.GetAsync($"{url}/openai/status", ct);
			return resp.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	//---------------------------------------------------------------------
	// CLI detection
	//---------------------------------------------------------------------
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

	public void Reset() { }
}
