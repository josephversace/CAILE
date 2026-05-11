using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

public sealed class ModelResolver : IModelResolver
{
	private readonly IModelConfigurationService _configService;
	private readonly ILogger<ModelResolver> _logger;

	public ModelResolver(
		IModelConfigurationService configService,
		ILogger<ModelResolver> logger)
	{
		_configService = configService;
		_logger = logger;
	}

	// ===========================================================
	// CHAT / REASONING (ACTIVE MODELS)
	// ===========================================================

	public async Task<ActiveModelConfig> GetPrimaryModelAsync(
		CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return cfg.Active.Primary;
	}

	public async Task<ActiveModelConfig?> GetSecondaryModelAsync(
		CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return cfg.Active.Secondary == null
			? null
			: cfg.Active.Secondary;
	}

	// ===========================================================
	// CAPABILITY-BASED RESOLUTION
	// ===========================================================

	public async Task<InfrastructureModelConfig> GetEmbeddingModelAsync(
		CancellationToken ct = default)
		=> await ResolveByCapabilityAsync(ModelCapabilities.Embeddings, ct);

	public async Task<InfrastructureModelConfig> GetVisionModelAsync(
		CancellationToken ct = default)
		=> await ResolveByCapabilityAsync(ModelCapabilities.Vision, ct);

	public async Task<InfrastructureModelConfig> GetFunctionCallingModelAsync(
		CancellationToken ct = default)
		=> await ResolveByCapabilityAsync(ModelCapabilities.Tools, ct);

	public async Task<InfrastructureModelConfig> GetIntentModelAsync(
		CancellationToken ct = default)
		=> await ResolveByCapabilityAsync(ModelCapabilities.Intent, ct);

	public async Task<InfrastructureModelConfig> GetNerModelAsync(
		CancellationToken ct = default)
		=> await ResolveByCapabilityAsync(ModelCapabilities.NER, ct);

	public async Task<InfrastructureModelConfig> GetAudioModelAsync(
		CancellationToken ct = default)
		=> await ResolveByCapabilityAsync(ModelCapabilities.Audio, ct);

	public async Task<ActiveModelConfig> ResolveActiveByModelIdAsync(string modelId, CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);

		if (cfg.Active.Primary.ModelId == modelId)
			return cfg.Active.Primary;

		if (cfg.Active.Secondary?.ModelId == modelId)
			return cfg.Active.Secondary;

		throw new KeyNotFoundException(
			$"Active model '{modelId}' is not configured.");
	}


	// ===========================================================
	// PROVIDER / DEFAULTS
	// ===========================================================

	public async Task<ProviderConfig> GetProviderAsync(
	ActiveModelConfig model,
	CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return model.ProviderOverride ?? cfg.Provider;
	}

	public async Task<InferenceDefaults> GetInferenceDefaultsAsync(
	ActiveModelConfig model,
	CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return model.Defaults ?? cfg.Defaults;
	}

	public async Task<ProviderConfig> GetProviderAsync(
		InfrastructureModelConfig model,
		CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return model.ProviderOverride ?? cfg.Provider;
	}

	public async Task<InferenceDefaults> GetInferenceDefaultsAsync(
		InfrastructureModelConfig model,
		CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return model.Defaults ?? cfg.Defaults;
	}

	// ===========================================================
	// INTERNAL HELPERS
	// ===========================================================

	private static InfrastructureModelConfig ResolveByKey(
		ModelsConfig cfg,
		string key)
	{
		if (!cfg.Infrastructure.Models.TryGetValue(key, out var model))
			throw new KeyNotFoundException($"Model '{key}' not found.");

		return model;
	}

	private async Task<InfrastructureModelConfig> ResolveByCapabilityAsync(
		ModelCapabilities capability,
		CancellationToken ct)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);

		var match = cfg.Infrastructure.Models.Values
			.FirstOrDefault(m => m.Capabilities.Contains(capability));

		if (match == null)
		{
			_logger.LogError(
				"No infrastructure model configured with capability '{Capability}'",
				capability);

			throw new InvalidOperationException(
				$"No model configured for capability '{capability}'.");
		}

		return match;
	}
}
