using System.Text.Json;
using IIM.Infrastructure.Data;
using IIM.Shared.Configuration;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Templates
{
	public class ModelTemplateService : IModelConfigurationTemplateService
	{
		private readonly ILogger<ModelTemplateService> _logger;
		private readonly IConfigRepository _settings;
		private readonly CaileConfig _cfg;

		private const string DefaultTemplateKey = "ModelTemplates.ActiveTemplate";

		private readonly Dictionary<string, ModelTemplateDto> _systemTemplates =
			new(StringComparer.OrdinalIgnoreCase);

		private readonly JsonSerializerOptions _jsonOptions = new()
		{
			WriteIndented = true,
			PropertyNameCaseInsensitive = true,
		};

		public ModelTemplateService(
			ILogger<ModelTemplateService> logger,
			IConfigRepository settingsStore,
			CaileConfig cfg)
		{
			_logger = logger;
			_settings = settingsStore;
			_cfg = cfg;

			LoadSystemTemplates();
		}

		// ---------------------------------------------------------
		// Default template: Try DB → fallback to installer/config
		// ---------------------------------------------------------
		public async Task<ModelTemplateDto?> GetDefaultTemplateAsync(CancellationToken ct = default)
		{
			// 1. Try the DB override
			var dbTemplate = await _settings.GetJsonAsync<ModelTemplateDto>(DefaultTemplateKey, ct);

			if (dbTemplate != null)
			{
				try
				{
					ValidateTemplate(dbTemplate);
					return dbTemplate;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex,
						"Stored default template invalid. Falling back to system template.");
				}
			}

			// 2. Fallback to system template
			var activeId = _cfg.ModelTemplates.ActiveTemplateId
						   ?? _cfg.Deployment.Tier     // tie to Deployment Tier
						   ?? "micro";

			if (_systemTemplates.TryGetValue(activeId, out var sys))
				return Clone(sys);

			return null;
		}

		// ---------------------------------------------------------
		// Save default template to DB
		// ---------------------------------------------------------
		public async Task SaveDefaultTemplateAsync(ModelTemplateDto template, CancellationToken ct = default)
		{
			if (template == null)
				throw new ArgumentNullException(nameof(template));

			ValidateTemplate(template);

			await _settings.SetJsonAsync(DefaultTemplateKey, template, "Models", ct);
			_logger.LogInformation("Default model template saved: {Id}", template.Id);
		}

		// ---------------------------------------------------------
		// System templates: loaded from CaileConfig.ModelTemplates.Templates
		// ---------------------------------------------------------
		public Task<List<ModelTemplateDto>> GetSystemTemplatesAsync(CancellationToken ct = default)
		{
			return Task.FromResult(
				_systemTemplates.Values
					.Select(Clone)
					.OrderBy(t => t.Name)
					.ToList()
			);
		}

		// ---------------------------------------------------------
		// Load templates from unified CaileConfig
		// ---------------------------------------------------------
		private void LoadSystemTemplates()
		{
			var src = _cfg.ModelTemplates.Templates;

			if (src == null || src.Count == 0)
			{
				_logger.LogWarning("No model templates defined in configuration.");
				return;
			}

			foreach (var (id, dto) in src)
			{
				var template = dto ?? new ModelTemplateDto { Id = id, Name = id };

				if (string.IsNullOrWhiteSpace(template.Id))
					template.Id = id;

				ValidateTemplate(template);

				_systemTemplates[template.Id] = Clone(template);

				_logger.LogInformation("Loaded system model template: {Id}", template.Id);
			}
		}

		// ---------------------------------------------------------
		// Validation rules
		// ---------------------------------------------------------
		private void ValidateTemplate(ModelTemplateDto template)
		{
			if (string.IsNullOrWhiteSpace(template.Name))
				throw new ArgumentException("Template must have a name.");

			if (template.Models == null)
				throw new ArgumentException("Template must include a Models object.");

			bool hasAny =
				template.Models.Chat != null ||
				template.Models.Reasoning != null ||
				template.Models.Coding != null ||
				template.Models.Embedding != null ||
				template.Models.Vision != null ||
				template.Models.Multimodal != null;

			if (!hasAny)
				throw new ArgumentException("Template must define at least one model slot.");

			template.EnabledTools ??= new List<string>();
		}

		// ---------------------------------------------------------
		// Deep clone for safety
		// ---------------------------------------------------------
		private ModelTemplateDto Clone(ModelTemplateDto src)
		{
			var json = JsonSerializer.Serialize(src, _jsonOptions);
			return JsonSerializer.Deserialize<ModelTemplateDto>(json, _jsonOptions)
				   ?? throw new InvalidOperationException("Template clone failed.");
		}
	}
}
