using IIM.Shared.Configuration;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Models;

public class ModelTemplateResolver : IModelTemplateResolver
{
	private readonly IModelConfigurationTemplateService _templates;
	private readonly CaileConfig _config;
	private readonly ILogger<ModelTemplateResolver> _logger;

	public ModelTemplateResolver(
		IModelConfigurationTemplateService templates,
		CaileConfig config,
		ILogger<ModelTemplateResolver> logger)
	{
		_templates = templates;
		_config = config;
		_logger = logger;
	}

	public async Task<ModelTemplateDto> GetActiveTemplateAsync(CancellationToken ct = default)
	{
		// Uses the ModelConfigurationTemplateService, not config directly
		var template = await _templates.GetDefaultTemplateAsync(ct);

		if (template == null)
			throw new InvalidOperationException("No active model template could be resolved.");

		_logger.LogDebug("Resolved active model template: {Id}", template.Id);

		return template;
	}

	public async Task<ModelTemplateDto> GetTemplateAsync(string id, CancellationToken ct = default)
	{
		var all = await _templates.GetSystemTemplatesAsync(ct);
		var match = all.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

		if (match == null)
			throw new KeyNotFoundException($"Template '{id}' not found.");

		return match;
	}
}
