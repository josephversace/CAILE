// IIM.Infrastructure/Models/ModelResolver.cs
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Models;

public class ModelResolver : IModelResolver
{
	private readonly IModelConfigurationService _configService;
	private readonly CaileConfig _config;
	private readonly ILogger<ModelResolver> _logger;

	public ModelResolver(
		IModelConfigurationService configService,
		CaileConfig config,
		ILogger<ModelResolver> logger)
	{
		_configService = configService;
		_config = config;
		_logger = logger;
	}

	public async Task<ActiveModelConfig> GetPrimaryModelAsync(CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return cfg.Active.Primary;
	}

	public async Task<ActiveModelConfig?> GetSecondaryModelAsync(CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return cfg.Active.Secondary;
	}

	public async Task<EmbeddingModelConfig> GetEmbeddingModelAsync(CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return cfg.Infrastructure.Embedding;
	}

	public async Task<ModelConfig?> GetFunctionCallingModelAsync(CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return cfg.Tools.FunctionCalling;
	}

	public async Task<ModelConfig?> GetVisionModelAsync(CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);

		// Prefer dedicated vision model
		if (cfg.Infrastructure.Vision != null)
			return cfg.Infrastructure.Vision;

		// Fall back to Primary if it supports vision
		if (cfg.Active.Primary.SupportsVision)
		{
			return new ModelConfig
			{
				ModelId = cfg.Active.Primary.ModelId,
				Temperature = cfg.Active.Primary.Temperature,
				MaxTokens = cfg.Active.Primary.MaxTokens,
				TopP = cfg.Active.Primary.TopP
			};
		}

		return null;
	}

	public async Task<LocalModelConfig?> GetNerModelAsync(CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return cfg.Infrastructure.NER;
	}

	public async Task<LocalModelConfig?> GetAudioModelAsync(CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);
		return cfg.Infrastructure.Audio;
	}

	public async Task<ModelConfig> GetIntentModelAsync(CancellationToken ct = default)
	{
		var cfg = await _configService.GetConfigurationAsync(ct);

		// Use dedicated intent model if configured, otherwise fall back to Primary
		if (cfg.Tools.Intent != null && !string.IsNullOrEmpty(cfg.Tools.Intent.ModelId))
		{
			return cfg.Tools.Intent;
		}

		// Fall back to Primary with intent-optimized settings
		return new ModelConfig
		{
			ModelId = cfg.Active.Primary.ModelId,
			Temperature = 0.0,
			MaxTokens = 20
		};
	}

	public ProviderConfig GetProvider() => _config.Models.Provider;

	public InferenceDefaults GetDefaults() => _config.Models.Defaults;
}