using System.Diagnostics;
using System.Net.Http.Json;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Foundry;

public interface IFoundryModelService
{
	Task<IReadOnlyList<FoundryModelDto>> GetAvailableModelsAsync(CancellationToken ct = default);
	Task<IReadOnlyList<FoundryModelDto>> GetCachedModelsAsync(CancellationToken ct = default);
	Task<IReadOnlyList<FoundryModelDto>> GetLoadedModelsAsync(CancellationToken ct = default);
	Task<IReadOnlyList<FoundryModelDto>> GetAllWithStatusAsync(CancellationToken ct = default);

	Task LoadModelAsync(string modelId, string? ep = null, int? ttl = null, CancellationToken ct = default);
	Task UnloadModelAsync(string modelId, bool force = false, CancellationToken ct = default);

	/// <summary>
	/// Unload all models then load all models referenced by the template
	/// using <c>foundry model run &lt;name&gt; --retain</c>.
	/// </summary>
	Task ApplyTemplateAsync(ModelTemplateDto template, CancellationToken ct = default);
}



public sealed class FoundryModelService : IFoundryModelService
{
	private readonly HttpClient _http;
	private readonly IFoundryEndpointProvider _endpoint;
	private readonly ILogger<FoundryModelService> _log;

	public FoundryModelService(
		HttpClient http,
		IFoundryEndpointProvider endpoint,
		ILogger<FoundryModelService> log)
	{
		_http = http;
		_endpoint = endpoint;
		_log = log;
	}

	/// <summary>
	/// Build a full URL for a Foundry Local REST path, using the
	/// current base URL from IFoundryEndpointProvider (no caching).
	/// </summary>
	private async Task<string> ApiAsync(string path, CancellationToken ct)
	{
		var baseUrl = _endpoint.GetBaseUrl();
		return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
	}

	// ------------------------------------------------------------------------
	// AVAILABLE MODELS (/foundry/list)
	// ------------------------------------------------------------------------
	public async Task<IReadOnlyList<FoundryModelDto>> GetAvailableModelsAsync(CancellationToken ct = default)
	{
		var url = await ApiAsync("foundry/list", ct);

		// /foundry/list returns a bare array, NOT { models: [...] }
		var resp = await _http.GetFromJsonAsync<List<FoundryCatalogModel>>(url, ct)
				   ?? new List<FoundryCatalogModel>();

		return resp.Select(MapCatalogModel).ToList();
	}

	// ------------------------------------------------------------------------
	// CACHED MODELS (from CLI: `foundry cache ls`)
	// ------------------------------------------------------------------------
	public async Task<IReadOnlyList<FoundryModelDto>> GetCachedModelsAsync(CancellationToken ct = default)
	{
		// 1. Parse CLI output
		var cachedEntries = await GetCachedModelEntriesAsync(ct);
		if (cachedEntries.Count == 0)
		{
			_log.LogInformation("No cached models detected via `foundry cache ls`.");
			return Array.Empty<FoundryModelDto>();
		}

		// 2. Get catalog from REST to enrich DTOs
		var url = await ApiAsync("foundry/list", ct);
		var catalog = await _http.GetFromJsonAsync<List<FoundryCatalogModel>>(url, ct)
					  ?? new List<FoundryCatalogModel>();

		var byName = catalog.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

		// 3. Also get loaded models so we can mark IsLoaded on cached models
		var loadedNames = await GetLoadedModelNamesAsync(ct);
		var loadedSet = new HashSet<string>(loadedNames, StringComparer.OrdinalIgnoreCase);

		var result = new List<FoundryModelDto>();

		foreach (var (alias, modelId) in cachedEntries)
		{
			FoundryModelDto dto;

			if (byName.TryGetValue(modelId, out var catalogModel))
			{
				dto = MapCatalogModel(catalogModel);
			}
			else
			{
				// Fallback if catalog doesn't know about this cached model
				dto = new FoundryModelDto
				{
					Id = !string.IsNullOrWhiteSpace(alias) ? alias : modelId,
					Alias = alias,
					DisplayName = $"{alias ?? modelId} (cached)",
					FoundryModelId = modelId,
					Device = "",
					Task = "",
					ProviderType = null,
					Version = null
				};
			}

			dto.IsLoaded = loadedSet.Contains(dto.FoundryModelId) ||
						   (!string.IsNullOrEmpty(dto.Alias) && loadedSet.Contains(dto.Alias));

			result.Add(dto);
		}

		return result;
	}

	// ------------------------------------------------------------------------
	// LOADED MODELS (REST: /openai/loadedmodels)
	// ------------------------------------------------------------------------
	public async Task<IReadOnlyList<FoundryModelDto>> GetLoadedModelsAsync(CancellationToken ct = default)
	{
		// Reuse GetAllWithStatusAsync to avoid double REST calls
		var all = await GetAllWithStatusAsync(ct);
		return all.Where(m => m.IsLoaded).ToList();
	}

	// ------------------------------------------------------------------------
	// ALL MODELS WITH STATUS (available + loaded)
	// ------------------------------------------------------------------------
	public async Task<IReadOnlyList<FoundryModelDto>> GetAllWithStatusAsync(CancellationToken ct = default)
	{
		var available = await GetAvailableModelsAsync(ct);

		// Get loaded names
		var loadedNames = await GetLoadedModelNamesAsync(ct);
		var loadedSet = new HashSet<string>(loadedNames, StringComparer.OrdinalIgnoreCase);

		foreach (var m in available)
		{
			if (loadedSet.Contains(m.DisplayName) ||
				(!string.IsNullOrEmpty(m.Alias) && loadedSet.Contains(m.Alias)))
			{
				m.IsLoaded = true;
			}
		}

		return available;
	}

	private async Task<List<string>> GetLoadedModelNamesAsync(CancellationToken ct)
	{
		var url = await ApiAsync("openai/loadedmodels", ct);

		var loaded = await _http.GetFromJsonAsync<List<string>>(url, ct)
					 ?? new List<string>();

		return loaded;
	}

	// ------------------------------------------------------------------------
	// LOAD MODEL (REST: /openai/load/{name})
	//
	// NOTE:
	//  - This is a generic “load” for ad-hoc operations.
	//  - Template application uses CLI `foundry model run ... --retain`
	//    so models are pinned.
	// ------------------------------------------------------------------------
	public async Task LoadModelAsync(string modelId, string? ep = null, int? ttl = null, CancellationToken ct = default)
	{
		var qs = new List<string>();

		if (ttl.HasValue)
			qs.Add($"ttl={ttl.Value}");

		if (!string.IsNullOrWhiteSpace(ep))
			qs.Add($"ep={Uri.EscapeDataString(ep)}");

		var suffix = qs.Count > 0 ? $"?{string.Join("&", qs)}" : "";

		var url = await ApiAsync($"openai/load/{Uri.EscapeDataString(modelId)}{suffix}", ct);

		_log.LogInformation("Loading Foundry model {Model} via REST {Url}", modelId, url);
		var resp = await _http.GetAsync(url, ct);
		resp.EnsureSuccessStatusCode();
	}

	// ------------------------------------------------------------------------
	// UNLOAD MODEL (REST: /openai/unload/{name})
	// ------------------------------------------------------------------------
	public async Task UnloadModelAsync(string modelId, bool force = false, CancellationToken ct = default)
	{
		var qs = force ? "?force=true" : "";
		var url = await ApiAsync($"openai/unload/{Uri.EscapeDataString(modelId)}{qs}", ct);

		_log.LogInformation("Unloading Foundry model {Model} via REST {Url}", modelId, url);
		var resp = await _http.GetAsync(url, ct);
		resp.EnsureSuccessStatusCode();
	}

	private async Task UnloadAllAsync(CancellationToken ct)
	{
		var url = await ApiAsync("openai/unloadall", ct);

		_log.LogInformation("Unloading ALL Foundry models via REST {Url}", url);
		var resp = await _http.GetAsync(url, ct);
		resp.EnsureSuccessStatusCode();
	}

	// ------------------------------------------------------------------------
	// APPLY TEMPLATE
	//
	// - Unloads ALL models via /openai/unloadall
	// - For each required Foundry model:
	//     foundry model run {modelId} --retain
	//
	// This ensures all template models are pinned in memory simultaneously.
	// ------------------------------------------------------------------------
	public async Task ApplyTemplateAsync(ModelTemplateDto template, CancellationToken ct = default)
	{
		if (template.Models is null)
			throw new InvalidOperationException("Template has no model definitions");

		// ------------------------------------------------------------
		// 1. Unload ALL currently loaded models
		// ------------------------------------------------------------
		var unloadAllUrl = await ApiAsync("openai/unloadall", ct);

		_log.LogInformation("Applying template: unloading all existing models via {Url}", unloadAllUrl);

		try
		{
			var unloadResp = await _http.GetAsync(unloadAllUrl);
			unloadResp.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "Failed to unload all current models.");
			throw;
		}

		// ------------------------------------------------------------
		// 2. Build the list of required model IDs
		// ------------------------------------------------------------
		var required = template
			.GetAllSlots()
			.Where(m => !string.IsNullOrWhiteSpace(m.FoundryModelId))
			.Select(m => m.FoundryModelId!)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (required.Count == 0)
		{
			_log.LogWarning("Template has no model slots requiring model loading.");
			return;
		}

		// ------------------------------------------------------------
		// 3. Load each model with "infinite" retention
		//    This emulates: `foundry model run MODEL --retain`
		// ------------------------------------------------------------
		foreach (var modelId in required)
		{
			string normalizedModel = modelId.Replace(" ", "");
			
			var url = await ApiAsync($"openai/load/{Uri.EscapeDataString(normalizedModel)}?ttl=999999", ct);

			_log.LogInformation("Applying template: loading model {ModelId} via {Url}", modelId, url);

			try
			{
				var resp = await _http.GetAsync(url);
				resp.EnsureSuccessStatusCode();

				_log.LogInformation("Model {ModelId} successfully loaded and retained.", modelId);
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Failed to load model {ModelId}", modelId);
				throw;
			}
		}

		_log.LogInformation("Template applied successfully.");
	}


	// ------------------------------------------------------------------------
	// MAPPING: Foundry catalog → DTO
	// ------------------------------------------------------------------------
	private static FoundryModelDto MapCatalogModel(FoundryCatalogModel m)
	{
		var device = m.Runtime?.DeviceType ?? "Unknown/CPU";
		var task = m.Task ?? string.Empty;

		long sizeBytes = (long)(m.FileSizeMb * 1024 * 1024);

		// heuristics
		bool isCoder =
		(m.Name ?? "").Contains("coder", StringComparison.OrdinalIgnoreCase) ||
		(task?.Contains("code", StringComparison.OrdinalIgnoreCase) ?? false) ||
		(task?.Contains("coding", StringComparison.OrdinalIgnoreCase) ?? false) ||
		(task?.Contains("programming", StringComparison.OrdinalIgnoreCase) ?? false) ||
		(m.ModelType ?? "").Contains("code", StringComparison.OrdinalIgnoreCase);

		bool isEmbed =
	(task?.Contains("embed", StringComparison.OrdinalIgnoreCase) ?? false) ||
	(task?.Contains("embedding", StringComparison.OrdinalIgnoreCase) ?? false) ||
	(task?.Contains("vectorize", StringComparison.OrdinalIgnoreCase) ?? false) ||
	(m.ModelType ?? "").Contains("embedding", StringComparison.OrdinalIgnoreCase) ||
	(m.ModelType ?? "").Contains("embeddings", StringComparison.OrdinalIgnoreCase) ||
	(m.ModelType ?? "").Contains("embed", StringComparison.OrdinalIgnoreCase) ||
	(m.Name ?? "").Contains("embed", StringComparison.OrdinalIgnoreCase);



		bool isVision = (task.Contains("vision", StringComparison.OrdinalIgnoreCase) ||
						 task.Contains("image", StringComparison.OrdinalIgnoreCase));
		bool isMultimodal =
	task.Contains("multimodal", StringComparison.OrdinalIgnoreCase) ||
	task.Contains("vision", StringComparison.OrdinalIgnoreCase) ||
	task.Contains("image", StringComparison.OrdinalIgnoreCase) ||
	task.Contains("audio", StringComparison.OrdinalIgnoreCase) ||
	task.Contains("video", StringComparison.OrdinalIgnoreCase) ||
	(m.ModelType ?? "").Contains("multimodal", StringComparison.OrdinalIgnoreCase) ||
	(m.ModelType ?? "").Contains("vision", StringComparison.OrdinalIgnoreCase) ||
	(m.Name ?? "").Contains("vlm", StringComparison.OrdinalIgnoreCase) ||
	(m.Name ?? "").Contains("multimodal", StringComparison.OrdinalIgnoreCase);


		// choose short ID: alias if present, else full name
		var id = !string.IsNullOrWhiteSpace(m.Alias) ? m.Alias! : m.Name;

		var display = $"{m.DisplayName ?? m.Name} ({device})";

		return new FoundryModelDto
		{
			Id = id,
			Alias = m.Alias,
			DisplayName = m.Name,
			FoundryModelId = m.DisplayName,
			Device = m.ModelType,
			Task = task,
			License = m.License,
			ProviderType = m.ProviderType,
			Version = m.Version,
			FileSizeMb = m.FileSizeMb,

			SupportsToolCalling = m.SupportsToolCalling,
			SupportsChat = task.Contains("chat", StringComparison.OrdinalIgnoreCase) ||
						   task.Contains("completion", StringComparison.OrdinalIgnoreCase),
			SupportsCoding = isCoder,
			SupportsEmbedding = isEmbed,
			SupportsVision = isVision,
			SupportsMultimodal = isMultimodal
		};
	}

	// ------------------------------------------------------------------------
	// CLI: `foundry cache ls`
	// ------------------------------------------------------------------------
	private async Task<List<(string Alias, string ModelId)>> GetCachedModelEntriesAsync(CancellationToken ct)
	{
		var result = new List<(string Alias, string ModelId)>();

		var psi = new ProcessStartInfo
		{
			FileName = "foundry",
			Arguments = "cache ls",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var proc = new Process { StartInfo = psi };

		try
		{
			if (!proc.Start())
			{
				_log.LogError("Failed to start `foundry cache ls` process.");
				return result;
			}
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "Error starting `foundry cache ls`. Is the Foundry CLI installed and on PATH?");
			return result;
		}

		var stdoutTask = proc.StandardOutput.ReadToEndAsync();
		var stderrTask = proc.StandardError.ReadToEndAsync();

		await proc.WaitForExitAsync(ct);

		var stdout = await stdoutTask;
		var stderr = await stderrTask;

		if (proc.ExitCode != 0)
		{
			_log.LogWarning("`foundry cache ls` exited with code {Code}. stderr: {Err}", proc.ExitCode, stderr);
			return result;
		}

		// Parse lines like:
		// "💾 deepseek-r1-14b                                   deepseek-r1-distill-qwen-14b-generic-gpu:3"
		var lines = stdout
			.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			.ToList();

		foreach (var rawLine in lines)
		{
			var line = rawLine.Trim();
			if (string.IsNullOrWhiteSpace(line))
				continue;

			// Skip header lines
			if (line.StartsWith("Models cached", StringComparison.OrdinalIgnoreCase) ||
				line.StartsWith("Alias", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			//// Lines with data usually start with "💾"
			//if (line.StartsWith("💾"))
			//{
			//	line = line.TrimStart("💾").Trim();
			//}

			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2)
				continue;

			var modelId = parts[^1];
			var alias = string.Join(' ', parts.Take(parts.Length - 1));

			result.Add((alias, modelId));
		}

		return result;
	}

	// ------------------------------------------------------------------------
	// CLI: `foundry model run {modelId} --retain`
	//
	// Returns true/false only (per your choice).
	// ------------------------------------------------------------------------
	private async Task<bool> RunFoundryModelRunRetainAsync(string modelId, CancellationToken ct)
	{
		var psi = new ProcessStartInfo
		{
			FileName = "foundry",
			Arguments = $"model run {modelId} --retain",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var proc = new Process { StartInfo = psi };

		try
		{
			if (!proc.Start())
			{
				_log.LogError("Failed to start `foundry model run {Model} --retain`.", modelId);
				return false;
			}
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "Error starting `foundry model run {Model} --retain`. Is Foundry CLI installed?", modelId);
			return false;
		}

		var stdoutTask = proc.StandardOutput.ReadToEndAsync();
		var stderrTask = proc.StandardError.ReadToEndAsync();

		await proc.WaitForExitAsync(ct);

		var stdout = await stdoutTask;
		var stderr = await stderrTask;

		if (proc.ExitCode != 0)
		{
			_log.LogError(
				"Foundry CLI `model run {Model} --retain` failed with exit code {Code}. stderr: {Err}",
				modelId, proc.ExitCode, stderr);
			return false;
		}

		_log.LogInformation("Foundry CLI `model run {Model} --retain` succeeded. Output: {Out}", modelId, stdout);
		return true;
	}
}


