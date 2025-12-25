using System.Net.Http.Json;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging;

// Alias to avoid conflict with EF Core's IModel
using FoundryModel = Microsoft.AI.Foundry.Local.IModel;
using LogLevel = Microsoft.Extensions.AI;

namespace IIM.Infrastructure.Foundry;

public interface IFoundryModelService
{
	Task<IReadOnlyList<FoundryModelDto>> GetAvailableModelsDtoAsync(CancellationToken ct = default);
	Task<IReadOnlyList<FoundryModelDto>> GetCachedModelsDtoAsync(CancellationToken ct = default);
	Task<IReadOnlyList<FoundryModelDto>> GetLoadedModelsDtoAsync(CancellationToken ct = default);
	Task<IReadOnlyList<FoundryModelDto>> GetAllWithStatusDtoAsync(CancellationToken ct = default);
	Task LoadModelAsync(string modelId, CancellationToken ct = default);
	Task UnloadModelAsync(string modelId, bool force = false, CancellationToken ct = default);
	Task<string> GetLoadedModelForAliasAsync(string alias, CancellationToken ct = default);
	Task ApplyTemplateAsync(ModelTemplateDto template, CancellationToken ct = default);
	Task<IReadOnlyList<(string Alias, string ModelId)>> GetCachedModelsAsync();
	Task EnsureInitializedAsync(CancellationToken ct = default);

	string BaseUrl { get; }
	string InferenceEndpoint { get; }
}

public sealed class FoundryModelService : IFoundryModelService, IAsyncDisposable
{
	private readonly HttpClient _http;
	private readonly ILogger<FoundryModelService> _log;
	private readonly string _baseUrl;
	private readonly SemaphoreSlim _initLock = new(1, 1);

	private bool _initialized;

	public FoundryModelService(
		HttpClient http,
		ILogger<FoundryModelService> log,
		string baseUrl = "http://127.0.0.1:5273")
	{
		_http = http;
		_log = log;
		_baseUrl = baseUrl.TrimEnd('/');
	}

	public string BaseUrl => _baseUrl;
	public string InferenceEndpoint => $"{_baseUrl}/v1";

	// ════════════════════════════════════════════════════════════════
	// INITIALIZATION
	// ════════════════════════════════════════════════════════════════

	public async Task EnsureInitializedAsync(CancellationToken ct = default)
	{
		if (_initialized)
			return;

		await _initLock.WaitAsync(ct);
		try
		{
			if (_initialized)
				return;

			var config = new Configuration
			{
				AppName = "iim",
				Web = new Configuration.WebService
				{
					Urls = _baseUrl
				}
			};

			try
			{
				await FoundryLocalManager.CreateAsync(config, _log, ct);
			}
			catch (FoundryLocalException ex) when (ex.Message.Contains("already been created"))
			{
				_log.LogDebug("FoundryLocalManager already initialized.");
			}

			var mgr = FoundryLocalManager.Instance;
			await mgr.EnsureEpsDownloadedAsync();
			await mgr.StartWebServiceAsync();

			_initialized = true;
			_log.LogInformation("FoundryLocalManager initialized with endpoint {Url}", _baseUrl);
		}
		finally
		{
			_initLock.Release();
		}
	}

	private async Task<FoundryLocalManager> GetManagerAsync(CancellationToken ct = default)
	{
		await EnsureInitializedAsync(ct);
		return FoundryLocalManager.Instance;
	}

	private async Task<ICatalog> GetCatalogAsync(CancellationToken ct = default)
	{
		var mgr = await GetManagerAsync(ct);
		return await mgr.GetCatalogAsync();
	}

	// ════════════════════════════════════════════════════════════════
	// MODEL LOADING
	// ════════════════════════════════════════════════════════════════

	public async Task LoadModelAsync(string modelId, CancellationToken ct = default)
	{
		var catalog = await GetCatalogAsync(ct);

		// 1. Check if already loaded
		var loaded = await catalog.GetLoadedModelsAsync();
		var alreadyLoaded = loaded.FirstOrDefault(m =>
			(m.Alias?.Equals(modelId, StringComparison.OrdinalIgnoreCase) ?? false) ||
			m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));

		if (alreadyLoaded != null)
		{
			_log.LogDebug("Model {Model} already loaded.", modelId);
			return;
		}

		// 2. Check cached models (returns ModelVariant)
		var cached = await catalog.GetCachedModelsAsync();
		var cachedCandidates = cached
			.Where(m =>
				(m.Alias?.Equals(modelId, StringComparison.OrdinalIgnoreCase) ?? false) ||
				m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase))
			.ToList();

		ModelVariant? variant = null;

		if (cachedCandidates.Count > 0)
		{
			variant = SelectBestVariant(cachedCandidates);
			_log.LogDebug("Found cached model variant: {Id}", variant.Id);
		}
		else
		{
			// 3. Not cached - need to download
			_log.LogInformation("Model {Model} not in cache, attempting to download...", modelId);

			var catalogModel = await catalog.GetModelAsync(modelId);

			if (catalogModel == null)
			{
				var allModels = await catalog.ListModelsAsync();
				catalogModel = allModels.FirstOrDefault(m =>
					(m.Alias?.Equals(modelId, StringComparison.OrdinalIgnoreCase) ?? false) ||
					m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));
			}

			if (catalogModel == null)
			{
				throw new InvalidOperationException(
					$"Model {modelId} not found in Foundry catalog.");
			}

			_log.LogInformation("Downloading model {Model}...", catalogModel.Id);

			variant = SelectBestVariant(catalogModel.Variants);

			await variant.DownloadAsync(
				progress =>
				{
					if (progress % 10 < 1)
					{
						_log.LogDebug("Download progress: {Progress:F1}%", progress);
					}
				},
				ct);

			_log.LogInformation("Model {Model} downloaded.", catalogModel.Id);

			// After download, get the variant
			cached = await catalog.GetCachedModelsAsync();
			variant = cached.FirstOrDefault(m =>
				(m.Alias?.Equals(modelId, StringComparison.OrdinalIgnoreCase) ?? false) ||
				m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));

			if (variant == null)
			{
				throw new InvalidOperationException(
					$"Model {modelId} downloaded but not found in cache.");
			}
		}

		// 4. Load the model
		_log.LogInformation("Loading model {Alias} → {Id}...", modelId, variant.Id);

		try
		{
			await variant.LoadAsync(ct);
			_log.LogInformation("Model loaded: {Id}", variant.Id);
		
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "Failed to load model {Model}.", variant.Id);
			throw;
		}
	}

	public async Task UnloadModelAsync(string modelId, bool force = false, CancellationToken ct = default)
	{
		var catalog = await GetCatalogAsync(ct);
		var loaded = await catalog.GetLoadedModelsAsync();

		var model = loaded.FirstOrDefault(m =>
			(m.Alias?.Equals(modelId, StringComparison.OrdinalIgnoreCase) ?? false) ||
			m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));

		if (model == null)
		{
			_log.LogWarning("Model {Model} not currently loaded.", modelId);
			return;
		}

		await model.UnloadAsync(ct);
		_log.LogInformation("Model {Model} unloaded.", modelId);
	}

	// ════════════════════════════════════════════════════════════════
	// VARIANT SELECTION
	// ════════════════════════════════════════════════════════════════

	private ModelVariant SelectBestVariant(List<ModelVariant> candidates)
	{
		if (candidates.Count == 1)
			return candidates[0];

		// 1. NPU - XDNA 2 should work
		var npuVariant = candidates.FirstOrDefault(m =>
			m.Id.Contains("npu", StringComparison.OrdinalIgnoreCase));

		if (npuVariant != null)
		{
			_log.LogInformation("Using NPU variant: {Id}", npuVariant.Id);
			return npuVariant;
		}

		// 2. CPU - Zen 5 fallback (skip GPU/DirectML - not supported in SDK on .NET 10)
		var cpuVariant = candidates.FirstOrDefault(m =>
			m.Id.Contains("cpu", StringComparison.OrdinalIgnoreCase));

		if (cpuVariant != null)
		{
			_log.LogInformation("Using CPU variant (NPU not available): {Id}", cpuVariant.Id);
			return cpuVariant;
		}

		// 3. Last resort
		_log.LogWarning("No NPU/CPU variant found, trying first available: {Id}", candidates[0].Id);
		return candidates[0];
	}

	// ════════════════════════════════════════════════════════════════
	// QUERIES
	// ════════════════════════════════════════════════════════════════

	public async Task<IReadOnlyList<(string Alias, string ModelId)>> GetCachedModelsAsync()
	{
		var catalog = await GetCatalogAsync();
		var cached = await catalog.GetCachedModelsAsync();
		return cached.Select(m => (m.Alias ?? "", m.Id)).ToList();
	}

	private async Task<IReadOnlyList<(string Alias, string ModelId)>> GetLoadedModelsInternalAsync(
		CancellationToken ct = default)
	{
		var catalog = await GetCatalogAsync(ct);
		var loaded = await catalog.GetLoadedModelsAsync();
		return loaded.Select(m => (m.Alias ?? "", m.Id)).ToList();
	}

	public async Task<string> GetLoadedModelForAliasAsync(string alias, CancellationToken ct = default)
	{
		var catalog = await GetCatalogAsync(ct);
		var loaded = await catalog.GetLoadedModelsAsync();

		if (loaded.Count == 0)
			throw new InvalidOperationException("No Foundry models are currently loaded.");

		var match = loaded.FirstOrDefault(m =>
			m.Alias?.Equals(alias, StringComparison.OrdinalIgnoreCase) ?? false);

		if (match != null)
			return match.Id;

		// Fall back to cached
		var cached = await GetCachedModelsAsync();
		var candidates = cached
			.Where(m => m.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
			.Select(m => m.ModelId)
			.ToList();

		if (candidates.Count > 0)
		{
			return candidates.FirstOrDefault(v => v.Contains("npu", StringComparison.OrdinalIgnoreCase))
				?? candidates.FirstOrDefault(v => v.Contains("cpu", StringComparison.OrdinalIgnoreCase))
				?? candidates[0];
		}

		throw new InvalidOperationException($"No model found for alias {alias}.");
	}

	// ════════════════════════════════════════════════════════════════
	// DTO QUERIES
	// ════════════════════════════════════════════════════════════════

	public async Task<IReadOnlyList<FoundryModelDto>> GetAvailableModelsDtoAsync(CancellationToken ct = default)
	{
		var catalog = await GetCatalogAsync(ct);
		var models = await catalog.ListModelsAsync();

		return models.Select(m => new FoundryModelDto
		{
			Id = m.Alias ?? m.Id,
			Alias = m.Alias,
			DisplayName = m.Id,
			FoundryModelId = m.Id
		}).ToList();
	}

	public async Task<IReadOnlyList<FoundryModelDto>> GetCachedModelsDtoAsync(CancellationToken ct = default)
	{
		var loaded = await GetLoadedModelsInternalAsync(ct);
		var loadedAliases = loaded
			.Select(m => m.Alias)
			.Where(a => !string.IsNullOrEmpty(a))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		var cached = await GetCachedModelsAsync();

		return cached.Select(m => new FoundryModelDto
		{
			Id = !string.IsNullOrWhiteSpace(m.Alias) ? m.Alias : m.ModelId,
			Alias = m.Alias,
			DisplayName = $"{m.Alias ?? m.ModelId} (cached)",
			FoundryModelId = m.ModelId,
			IsLoaded = loadedAliases.Contains(m.Alias)
		}).ToList();
	}

	public async Task<IReadOnlyList<FoundryModelDto>> GetLoadedModelsDtoAsync(CancellationToken ct = default)
	{
		var all = await GetAllWithStatusDtoAsync(ct);
		return all.Where(m => m.IsLoaded).ToList();
	}

	public async Task<IReadOnlyList<FoundryModelDto>> GetAllWithStatusDtoAsync(CancellationToken ct = default)
	{
		var available = await GetAvailableModelsDtoAsync(ct);
		var loaded = await GetLoadedModelsInternalAsync(ct);

		var loadedAliases = loaded
			.Where(l => !string.IsNullOrWhiteSpace(l.Alias))
			.Select(l => l.Alias)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		foreach (var model in available)
		{
			model.IsLoaded = model.Alias != null && loadedAliases.Contains(model.Alias);
		}

		return available;
	}

	// ════════════════════════════════════════════════════════════════
	// TEMPLATE
	// ════════════════════════════════════════════════════════════════

	public async Task ApplyTemplateAsync(ModelTemplateDto template, CancellationToken ct = default)
	{
		if (template.Models is null)
			throw new InvalidOperationException("Template has no model definitions.");

		var required = template
			.GetAllSlots()
			.Where(m => !string.IsNullOrWhiteSpace(m.FoundryModelId))
			.Select(m => m.FoundryModelId!)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (required.Count == 0)
		{
			_log.LogWarning("Template has no model slots requiring loading.");
			return;
		}

		foreach (var modelId in required)
		{
			try
			{
				await LoadModelAsync(modelId, ct);
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Failed to load model {Model} from template.", modelId);
			}
		}

		_log.LogInformation("Template applied successfully.");
	}

	// ════════════════════════════════════════════════════════════════
	// CLEANUP
	// ════════════════════════════════════════════════════════════════

	public async ValueTask DisposeAsync()
	{
		if (_initialized)
		{
			try
			{
				var mgr = FoundryLocalManager.Instance;
				await mgr.StopWebServiceAsync();
			}
			catch { }
		}

		_initLock.Dispose();
	}
}