using System.Text.Json;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Models;

public sealed class ModelConfigurationService : IModelConfigurationService
{
	private const string ModelsKey = "Models";

	private readonly ILogger<ModelConfigurationService> _logger;
	private readonly IConfigRepository _settings;
	private readonly ModelsConfig _defaults;

	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};

	public ModelConfigurationService(
		ILogger<ModelConfigurationService> logger,
		IConfigRepository settingsStore,
		CaileConfig cfg)
	{
		_logger = logger;
		_settings = settingsStore;
		_defaults = cfg.Models;
	}

	// ===========================================================
	// READ
	// ===========================================================

	public async Task<ModelsConfig> GetConfigurationAsync(
		CancellationToken ct = default)
	{
		// 1. Settings are authoritative after first write
		var stored = await _settings.GetJsonAsync<ModelsConfig>(ModelsKey, ct);
		if (stored != null)
			return stored;

		// 2. First-run bootstrap (materialize defaults)
		var materialized = Clone(_defaults);

		await _settings.SetJsonAsync(
			ModelsKey,
			materialized,
			category: "Models",
			ct);

		_logger.LogInformation(
			"Models configuration initialized from defaults.");

		return materialized;
	}



	// ===========================================================
	// WRITE (FULL MATERIALIZED CONFIG)
	// ===========================================================

	public async Task SaveConfigurationAsync(
		ModelsConfig config,
		CancellationToken ct = default)
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));

		Validate(config);

		await _settings.SetJsonAsync(
			ModelsKey,
			config,
			category: "Models",
			ct);

		_logger.LogInformation("Models configuration saved.");
	}


	// ===========================================================
	// RESET
	// ===========================================================

	public async Task ResetToDefaultsAsync(
		CancellationToken ct = default)
	{
		var materialized = Clone(_defaults);

		await _settings.SetJsonAsync(
			ModelsKey,
			materialized,
			category: "Models",
			ct);

		_logger.LogInformation(
			"Models configuration reset to defaults.");
	}


	// ===========================================================
	// VALIDATION (FAIL FAST)
	// ===========================================================

	private static void Validate(ModelsConfig cfg)
	{
		if (cfg.Infrastructure.Models.Count == 0)
			throw new InvalidOperationException(
				"No infrastructure models configured.");

		foreach (var (key, model) in cfg.Infrastructure.Models)
		{
			if (string.IsNullOrWhiteSpace(model.Key))
				throw new InvalidOperationException(
					$"Infrastructure model '{key}' is missing Key.");

			if (!string.Equals(key, model.Key, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException(
					$"Infrastructure model key mismatch: '{key}' != '{model.Key}'");

			if (string.IsNullOrWhiteSpace(model.ModelId) &&
				string.IsNullOrWhiteSpace(model.LocalPath))
				throw new InvalidOperationException(
					$"Infrastructure model '{key}' has neither ModelId nor LocalPath.");
		}

		if (string.IsNullOrWhiteSpace(cfg.Active.Primary.ModelId))
			throw new InvalidOperationException(
				"Active.Primary.ModelKey is required.");

		if (!cfg.Infrastructure.Models.ContainsKey(cfg.Active.Primary.ModelId))
			throw new InvalidOperationException(
				$"Active.Primary.ModelKey '{cfg.Active.Primary.ModelId}' does not exist.");
	}

	// ===========================================================
	// UTIL
	// ===========================================================

	private T Clone<T>(T src) where T : class
	{
		var json = JsonSerializer.Serialize(src, _jsonOptions);
		return JsonSerializer.Deserialize<T>(json, _jsonOptions)
			?? throw new InvalidOperationException("Clone failed.");
	}
}
