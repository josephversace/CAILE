using IIM.Shared.Configuration;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IIM.Infrastructure.Foundry;

public sealed class FoundryStartupService : IHostedService
{
	private readonly ILogger<FoundryStartupService> _log;
	private readonly IFoundryEndpointProvider _endpoint;
	private readonly IFoundryStatusChecker _status;
	private readonly IFoundryModelService _modelSvc;
	private readonly CaileConfig _cfg;

	public FoundryStartupService(
		ILogger<FoundryStartupService> log,
		IFoundryEndpointProvider endpoint,
		IFoundryStatusChecker status,
		IFoundryModelService modelSvc,
		CaileConfig cfg)
	{
		_log = log;
		_endpoint = endpoint;
		_status = status;
		_modelSvc = modelSvc;
		_cfg = cfg;
	}

	public async Task StartAsync(CancellationToken ct)
	{
		_log.LogInformation("Initializing Foundry Local...");

		// 1. Start service if not running
		if (!await _status.IsServiceRunningAsync())
		{
			_log.LogWarning("Foundry not running. Attempting start...");
			await _status.StartServiceAsync();
		}

		// 2. Resolve endpoint — wait for Foundry to expose HTTP
		var resolved = await WaitForEndpointAsync(ct);
		_log.LogInformation("Foundry endpoint confirmed: {Url}", resolved);



		// 3. Apply default model template
		var activeId = _cfg.ModelTemplates.ActiveTemplateId;
		var template = _cfg.ModelTemplates.Templates[activeId];

		_log.LogInformation("Applying Foundry template: {Id}", activeId);

		await _modelSvc.ApplyTemplateAsync(template, ct);

		_log.LogInformation("Foundry initialized successfully.");
	}

	public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

	private async Task<string> WaitForEndpointAsync(CancellationToken ct)
	{
		for (int i = 0; i < 30; i++)
		{
			try
			{
				var url = _endpoint.GetBaseUrl();
				if (await _endpoint.IsOnlineAsync(ct))
					return url;
			}
			catch { /* ignore */ }

			await Task.Delay(1000, ct);
		}

		throw new Exception("Foundry Local did not start within timeout.");
	}
}
