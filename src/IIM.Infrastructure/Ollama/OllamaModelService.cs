
using IIM.Shared.Dtos;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;

namespace IIM.Infrastructure.Ollama;

public sealed class OllamaModelService : IModelService, IAsyncDisposable
{
    private readonly ILogger<OllamaModelService> _log;
    private readonly string _baseUrl;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private OllamaApiClient? _client;
    private bool _initialized;

    // Track which models we've "loaded" (warmed up with keep_alive)
    // Track which models we've "loaded" (warmed up with keep_alive)
    private readonly HashSet<string> _loadedModels = new(StringComparer.OrdinalIgnoreCase);

    // Track which model is loaded in each slot (for unload-before-load logic)
    private string? _primaryModel;
    private string? _secondaryModel;

    public OllamaModelService(ILogger<OllamaModelService> log, string baseUrl = "http://localhost:11434")
    {
        _log = log;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    // ─────────────────────────────────────────────────────────────
    // IModelService implementation
    // ─────────────────────────────────────────────────────────────

    public string BaseUrl => _baseUrl;

    // Ollama's OpenAI-compatible endpoint
    public string InferenceEndpoint => $"{_baseUrl}/v1";

    public async Task EnsureInitializedAsync(CancellationToken ct = default)
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized)
                return;

            _log.LogInformation("Initializing Ollama client at {Url}", _baseUrl);

            _client = new OllamaApiClient(new Uri(_baseUrl));

            // Verify connectivity by listing models
            var models = await _client.ListLocalModelsAsync(ct);
            _log.LogInformation("Ollama connected. Found {Count} local models", models.Count());

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task LoadModelAsync(string modelId, CancellationToken ct = default)
    {
        EnsureReady();

        _log.LogInformation("Loading model {Model} into Ollama", modelId);

        // Check if model exists locally, pull if not
        var localModels = await _client!.ListLocalModelsAsync(ct);
        var exists = localModels.Any(m =>
            m.Name.Equals(modelId, StringComparison.OrdinalIgnoreCase) ||
            m.Name.StartsWith(modelId + ":", StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            _log.LogInformation("Model {Model} not found locally, pulling...", modelId);
            await PullModelAsync(modelId, ct);
        }

        // Warm up the model by sending a minimal request with keep_alive=-1
        // This loads it into VRAM and keeps it there
        try
        {
            await _client.GenerateAsync(new GenerateRequest
            {
                Model = modelId,
                Prompt = "hi",
                Options = new RequestOptions { NumPredict = 1 },
                KeepAlive = "1h"  // Keep loaded indefinitely
            }, ct).ToListAsync(ct);

            _loadedModels.Add(modelId);
            _log.LogInformation("Model {Model} loaded and warmed up", modelId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to warm up model {Model}, may load on first request", modelId);
        }
    }

    /// <summary>
    /// Loads a model into a specific slot, unloading the previous model in that slot first.
    /// Also persists the change to the active template.
    /// </summary>
    public async Task LoadModelForSlotAsync(string modelId, string slot, CancellationToken ct = default)
    {
	
		EnsureReady();

        // Determine which model to unload based on slot
        string? modelToUnload = slot.ToLowerInvariant() switch
        {
            "primary" or "chat" => _primaryModel,
            "secondary" or "reasoning" => _secondaryModel,
            _ => null
        };

        // Unload previous model in this slot if different
        if (!string.IsNullOrEmpty(modelToUnload) &&
            !modelToUnload.Equals(modelId, StringComparison.OrdinalIgnoreCase))
        {
            _log.LogInformation("Unloading {Old} from {Slot} slot before loading {New}",
                modelToUnload, slot, modelId);
            await UnloadModelAsync(modelToUnload, force: true, ct);
            await Task.Delay(500, ct); // Let VRAM free up
        }

        // Load the new model
        await LoadModelCoreAsync(modelId, ct);

        // Track which slot has this model
        switch (slot.ToLowerInvariant())
        {
            case "primary":
            case "chat":
                _primaryModel = modelId;
                break;
            case "secondary":
            case "reasoning":
                _secondaryModel = modelId;
                break;
			default:
				throw new ArgumentException($"Unknown model slot '{slot}'", nameof(slot));

		}
	}

    private async Task LoadModelCoreAsync(string modelId, CancellationToken ct)
    {

		if (_loadedModels.Contains(modelId))
        {
            _log.LogInformation("Model {Model} already loaded", modelId);
            return;
        }

        _log.LogInformation("Loading model {Model} into Ollama", modelId);

        // Check if model exists locally, pull if not
        var localModels = await _client!.ListLocalModelsAsync(ct);
        var exists = localModels.Any(m =>
            m.Name.Equals(modelId, StringComparison.OrdinalIgnoreCase) ||
            m.Name.StartsWith(modelId + ":", StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            _log.LogInformation("Model {Model} not found locally, pulling...", modelId);
            await PullModelAsync(modelId, ct);
        }

        // Warm up the model
        try
        {
            await _client.GenerateAsync(new GenerateRequest
            {
                Model = modelId,
                Prompt = "hi",
                Options = new RequestOptions { NumPredict = 1 },
                KeepAlive = "1h"
            }, ct).ToListAsync(ct);

            _loadedModels.Add(modelId);
            _log.LogInformation("Model {Model} loaded and warmed up", modelId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to warm up model {Model}", modelId);
        }
    }

    public async Task UnloadModelAsync(string modelId, bool force = false, CancellationToken ct = default)
    {
        EnsureReady();

        _log.LogInformation("Unloading model {Model}", modelId);

        // Send a request with keep_alive=0 to unload immediately
        try
        {
            await _client!.GenerateAsync(new GenerateRequest
            {
                Model = modelId,
                Prompt = "",
                KeepAlive = "0"  // Unload immediately
            }, ct).ToListAsync(ct);

            _loadedModels.Remove(modelId);
            _log.LogInformation("Model {Model} unloaded", modelId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to unload model {Model}", modelId);
        }
    }

    /// <summary>
    /// Unloads the model in a specific slot and clears the slot tracking.
    /// </summary>
    public async Task UnloadSlotAsync(string slot, CancellationToken ct = default)
    {
        EnsureReady();

        var normalizedSlot = slot.ToLowerInvariant();

        string? modelToUnload = normalizedSlot switch
        {
            "primary" or "chat" => _primaryModel,
            "secondary" or "reasoning" => _secondaryModel,
            _ => null
        };

        if (!string.IsNullOrEmpty(modelToUnload))
        {
            _log.LogInformation("Unloading {Model} from {Slot} slot", modelToUnload, slot);
            await UnloadModelAsync(modelToUnload, force: true, ct);
        }

        // Clear the slot tracking
        switch (normalizedSlot)
        {
            case "primary":
            case "chat":
                _primaryModel = null;
                break;
            case "secondary":
            case "reasoning":
                _secondaryModel = null;
                break;
        }

        _log.LogInformation("{Slot} slot cleared", slot);
    }

    public Task<string> GetLoadedModelForAliasAsync(string alias, CancellationToken ct = default)
    {
        // Ollama uses model names directly, no alias → variant mapping needed
        // Just return the alias as-is (e.g., "gemma3:27b" stays "gemma3:27b")
        return Task.FromResult(alias);
    }

    public async Task<IReadOnlyList<ModelCatalogEntryDto>> GetAvailableModelsDtoAsync(CancellationToken ct = default)
    {
        // For Ollama, "available" could mean models in the library
        // This would require scraping ollama.com/library - not practical
        // Return empty or same as cached
        return await GetCachedModelsDtoAsync(ct);
    }

    public async Task<IReadOnlyList<ModelCatalogEntryDto>> GetCachedModelsDtoAsync(CancellationToken ct = default)
    {
        EnsureReady();

        var models = await _client!.ListLocalModelsAsync(ct);

		return models.Select(m =>
		{

			var capabilities = InferCapabilitiesHeuristically(m.Name);

			return new ModelCatalogEntryDto
			{
				// ===================================================
				// IDENTITY
				// ===================================================
				Key = ExtractShortName(m.Name),     // canonical CAILE key
				ModelId = m.Name,                  // Ollama inference id
				Alias = ExtractShortName(m.Name),

				// ===================================================
				// DISPLAY
				// ===================================================
				DisplayName = m.Name,
				RawName = m.Name,

				// ===================================================
				// PROVIDER / RUNTIME
				// ===================================================
				ProviderType = "ollama",
				Backend = "ollama",
				Device = "auto",                   // Ollama abstracts this
				IsLoaded = _loadedModels.Contains(m.Name),

				// ===================================================
				// CAPABILITIES
				// ===================================================
				Capabilities = capabilities,

				// ===================================================
				// SIZE / METADATA
				// ===================================================
				FileSizeMb = m.Size / (1024.0 * 1024.0),
				License = null,
				Version = null
			};
		})
	.ToList();

	}

	public async Task<IReadOnlyList<ModelCatalogEntryDto>> GetLoadedModelsDtoAsync(CancellationToken ct = default)
    {
        EnsureReady();

        var runningModels = await _client!.ListRunningModelsAsync(ct);

        return runningModels.Select(m =>
		{

			var capabilities = InferCapabilitiesHeuristically(m.Name);

			return new ModelCatalogEntryDto
			{
				// ===================================================
				// IDENTITY
				// ===================================================
				Key = ExtractShortName(m.Name),     // canonical CAILE key
				ModelId = m.Name,                  // Ollama inference id
				Alias = ExtractShortName(m.Name),

				// ===================================================
				// DISPLAY
				// ===================================================
				DisplayName = m.Name,
				RawName = m.Name,

				// ===================================================
				// PROVIDER / RUNTIME
				// ===================================================
				ProviderType = "ollama",
				Backend = "ollama",
				Device = "auto",                   // Ollama abstracts this
				IsLoaded = _loadedModels.Contains(m.Name),

				// ===================================================
				// CAPABILITIES
				// ===================================================
				Capabilities = capabilities,

				// ===================================================
				// SIZE / METADATA
				// ===================================================
				FileSizeMb = m.Size / (1024.0 * 1024.0),
				License = null,
				Version = null
			};
		})
	.ToList();
	}

    public async Task<IReadOnlyList<ModelCatalogEntryDto>> GetAllWithStatusDtoAsync(CancellationToken ct = default)
    {
        var cached = await GetCachedModelsDtoAsync(ct);
        var running = await _client!.ListRunningModelsAsync(ct);
        var runningNames = running.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Update IsLoaded status
        foreach (var model in cached)
        {
            model.IsLoaded = runningNames.Contains(model.RawName);
        }

        return cached;
    }

    // ═══════════════════════════════════════════════════════════════
    // MODEL FILTERING FOR CHAT UI
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns multimodal models suitable for Primary chat slot.
    /// Filters to recommended families only.
    /// </summary>
    public async Task<IReadOnlyList<ModelCatalogEntryDto>> GetPrimaryModelsAsync(CancellationToken ct = default)
    {
        var all = await GetCachedModelsDtoAsync(ct);
        return all.Where(m => IsRecommendedPrimary(m.ModelId)).ToList();
    }

    /// <summary>
    /// Returns chat models suitable for Secondary/reasoning slot.
    /// Excludes Chinese model families.
    /// </summary>
    public async Task<IReadOnlyList<ModelCatalogEntryDto>> GetSecondaryModelsAsync(CancellationToken ct = default)
    {
        var all = await GetCachedModelsDtoAsync(ct);
        return all.Where(m => m.Capabilities.Contains(ModelCapabilities.Text) && !IsExcludedFamily(m.ModelId)).ToList();
    }

    /// <summary>
    /// Checks if a model belongs to the recommended primary (multimodal) families.
    /// </summary>
    public static bool IsRecommendedPrimary(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
            return false;

        var normalized = modelName.ToLowerInvariant();

        foreach (var family in RecommendedPrimaryModels)
        {
            if (normalized.StartsWith(family, StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains(family, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a model belongs to an excluded (Chinese) family.
    /// </summary>
    public static bool IsExcludedFamily(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
            return false;

        var normalized = modelName.ToLowerInvariant();

        foreach (var family in ExcludedModelFamilies)
        {
            if (normalized.StartsWith(family, StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains(family, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public async Task<IReadOnlyList<(string Alias, string ModelId)>> GetCachedModelsAsync()
    {
        EnsureReady();

        var models = await _client!.ListLocalModelsAsync();

        return models
            .Select(m => (Alias: ExtractShortName(m.Name), ModelId: m.Name))
            .ToList();
    }

	public async Task<(string? Primary, string? Secondary)> GetActiveSlotsAsync(
		CancellationToken ct = default)
	{
		await EnsureInitializedAsync(ct);

		// If slots are empty, do NOT guess — let the agent factory hydrate
		if (_primaryModel == null && _secondaryModel == null)
		{
			_log.LogInformation("Active slots not initialized yet");
		}

		return (_primaryModel, _secondaryModel);
	}




	// ─────────────────────────────────────────────────────────────
	// Ollama-specific helpers
	// ─────────────────────────────────────────────────────────────

	private async Task PullModelAsync(string modelId, CancellationToken ct)
    {
        _log.LogInformation("Pulling model {Model} from Ollama registry...", modelId);

        await foreach (var status in _client!.PullModelAsync(modelId, ct))
        {
            if (!string.IsNullOrEmpty(status.Status))
            {
                _log.LogDebug("Pull {Model}: {Status}", modelId, status.Status);
            }
        }

        _log.LogInformation("Model {Model} pulled successfully", modelId);
    }

    private void EnsureReady()
    {
        if (!_initialized || _client == null)
            throw new InvalidOperationException("Ollama service not initialized. Call EnsureInitializedAsync first.");
    }

    // Multimodal models for Primary (non-Chinese)
    private static readonly HashSet<string> RecommendedPrimaryModels = new(StringComparer.OrdinalIgnoreCase)
{
    "gemma3",           // Google
    "llama3",  // Meta
    "llava",            // UW-Madison + Microsoft
    "llava-llama3",     // Community
    "nemotron", // Nvidia
    "mistral-mixtral",
    "phi"// Mistral
};

    // Chinese model families to exclude
    private static readonly HashSet<string> ExcludedModelFamilies = new(StringComparer.OrdinalIgnoreCase)
{
    "deepseek", "yi", "minicpm", "glm", "baichuan", "internlm"
};

    /// <summary>
    /// Extracts short name from Ollama model name.
    /// e.g., "gemma3:27b" → "gemma3:27b", "phi4:latest" → "phi4"
    /// </summary>
    private static string ExtractShortName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return fullName;

        // Remove ":latest" suffix only
        if (fullName.EndsWith(":latest", StringComparison.OrdinalIgnoreCase))
            return fullName[..^7];

        return fullName;
    }

	private static IReadOnlyList<ModelCapabilities> InferCapabilitiesHeuristically(string name)
	{
		var caps = new HashSet<ModelCapabilities>();

    
        if (name.Contains("gemma3")) {

            if (name.Contains("gemma3:1b"))
            {
                caps.Add(ModelCapabilities.Text);
            }
            else
            {
                caps.Add(ModelCapabilities.Text);
                caps.Add(ModelCapabilities.MultiModal);
                caps.Add(ModelCapabilities.Vision);


            }
        }

        if (name.Contains("nemotron"))
        {
			caps.Add(ModelCapabilities.Text);
		}
        // ============================
        // EMBEDDINGS
        // ============================
        if (name.Contains("embed") || name.Contains("nomic") || name.Contains("bert"))
        {
            caps.Add(ModelCapabilities.Embeddings);
            return caps.ToList(); // embeddings are exclusive

        }

		// ============================
		// VISION / MULTIMODAL
		// ============================
		if (name.Contains("vision") || name.Contains("llava") || name.Contains("moondream"))
		{
			caps.Add(ModelCapabilities.Vision);
		
		}

		// ============================
		// AUDIO
		// ============================
		if (name.Contains("whisper"))
		{
			caps.Add(ModelCapabilities.Audio);
		}

       

		// ============================
		// TOOLS (best-effort)
		// ============================
		if (
			name.Contains("llama3:8b") ||
			name.Contains("mistral") ||
			name.Contains("phi")
		)
		{
			caps.Add(ModelCapabilities.Text);
			caps.Add(ModelCapabilities.Tools);
		}

		// ============================
		// DEFAULT FALLBACK
		// ============================
		if (caps.Count == 0)
		{
			caps.Add(ModelCapabilities.Text);
		}

		return caps.ToList();
	}


	// ─────────────────────────────────────────────────────────────
	// Disposal
	// ─────────────────────────────────────────────────────────────

	public ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        // OllamaApiClient doesn't implement IDisposable
        return ValueTask.CompletedTask;
    }
}