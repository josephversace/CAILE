// IIM.Infrastructure/Models/ModelConfigurationService.cs
using System.Text.Json;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Models;

public class ModelConfigurationService : IModelConfigurationService
{
	private readonly ILogger<ModelConfigurationService> _logger;
	private readonly IConfigRepository _settings;
	private readonly CaileConfig _cfg;

	private const string ActiveModelsKey = "Models.ActiveOverride";

	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
	};

	public ModelConfigurationService(
		ILogger<ModelConfigurationService> logger,
		IConfigRepository settingsStore,
		CaileConfig cfg)
	{
		_logger = logger;
		_settings = settingsStore;
		_cfg = cfg;
	}

	public async Task<ModelsConfig> GetConfigurationAsync(CancellationToken ct = default)
	{
		// Start with base config from appsettings
		var config = Clone(_cfg.Models);

		// Check for DB override of Active models
		var activeOverride = await _settings.GetJsonAsync<ActiveModelsConfig>(ActiveModelsKey, ct);
		if (activeOverride != null)
		{
			_logger.LogDebug("Applying active models override from database");
			config.Active = activeOverride;
		}

		return config;
	}

	public async Task SaveActiveModelsAsync(ActiveModelsConfig active, CancellationToken ct = default)
	{
		if (active == null)
			throw new ArgumentNullException(nameof(active));

		if (string.IsNullOrWhiteSpace(active.Primary?.ModelId))
			throw new ArgumentException("Primary model is required.");

		await _settings.SetJsonAsync(ActiveModelsKey, active, "Models", ct);
		_logger.LogInformation("Active models saved: Primary={Primary}, Secondary={Secondary}",
			active.Primary.ModelId,
			active.Secondary?.ModelId ?? "none");
	}

	public async Task ResetActiveModelsAsync(CancellationToken ct = default)
	{
		await _settings.DeleteAsync(ActiveModelsKey, ct);
		_logger.LogInformation("Active models reset to defaults from appsettings");
	}

	private T Clone<T>(T src) where T : class
	{
		var json = JsonSerializer.Serialize(src, _jsonOptions);
		return JsonSerializer.Deserialize<T>(json, _jsonOptions)
			   ?? throw new InvalidOperationException("Clone failed.");
	}
}