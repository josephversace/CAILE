using IIM.Api.Services;
using IIM.Infrastructure.Ollama;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

public static class ModelRegistryEndpoints
{
	public static void MapModelRegistryEndpoints(this IEndpointRouteBuilder app)
	{
		// ===========================================================
		// MODEL CATALOG (NEW, PROVIDER-AGNOSTIC)
		// ===========================================================
		app.MapGet("/api/models/catalog", async (
		[FromServices] IModelService modelService,
		[FromServices] IModelConfigurationService configSvc,
		CancellationToken ct) =>
		{
			await modelService.EnsureInitializedAsync(ct);

			var cfg = await configSvc.GetConfigurationAsync(ct);

			var models = await modelService.GetAllWithStatusDtoAsync(ct);

			var catalog = new ModelCatalogDto
			{
				Provider = new ProviderDescriptorDto
				{
					Type = cfg.Provider.Type,
					Endpoint = modelService.InferenceEndpoint,
					RequiresApiKey = !string.IsNullOrWhiteSpace(cfg.Provider.ApiKey)
				},
				Models = models
			};

			return Results.Ok(catalog);
		})
	.WithTags("Model Catalog")
	.WithOpenApi();



		// ===========================================================
		// GROUP A: Ollama Model Registry (UNCHANGED)
		// ===========================================================
		var ollama = app.MapGroup("/api/models/ollama")
			.WithTags("Ollama Models")
			.WithOpenApi();

		ollama.MapGet("/loaded", async ([FromServices] IModelService svc, CancellationToken ct)
			=> Results.Ok(await svc.GetLoadedModelsDtoAsync(ct)));

		ollama.MapGet("/cached", async ([FromServices] IModelService svc, CancellationToken ct)
			=> Results.Ok(await svc.GetCachedModelsDtoAsync(ct)));

		ollama.MapPost("/load/{model}", async (string model, [FromServices] IModelService svc, CancellationToken ct) =>
		{
			await svc.LoadModelAsync(model, ct);
			return Results.Ok();
		});

		ollama.MapPost("/unload/{model}", async (
			string model,
			[FromServices] IModelService svc,
			[FromQuery] bool force,
			CancellationToken ct) =>
		{
			await svc.UnloadModelAsync(model, force, ct);
			return Results.Ok();
		});

		app.MapGet("/api/models/active", async (
	[FromServices] IModelService modelService,
	CancellationToken ct) =>
		{
			var active = await modelService.GetActiveSlotsAsync(ct);

			return Results.Ok(new ActiveModelsResponse
			{
				Primary = active.Primary,
				Secondary = active.Secondary
			});
		})
.WithTags("Model Registry")
.WithSummary("Get currently active models per slot")
.WithOpenApi();


		// ===========================================================
		// GROUP B: Model Configuration (UNIFIED)
		// ===========================================================
		var config = app.MapGroup("/api/models/config")
			.WithTags("Model Configuration")
			.WithOpenApi();

		// -----------------------------------------------------------
		// GET full materialized configuration
		// -----------------------------------------------------------
		config.MapGet("/", async (
			[FromServices] IModelConfigurationService svc,
			CancellationToken ct) =>
		{
			return Results.Ok(await svc.GetConfigurationAsync(ct));
		});

		// -----------------------------------------------------------
		// PUT full configuration (authoritative write)
		// -----------------------------------------------------------
		config.MapPut("/", async (
			[FromBody] ModelsConfig modelConfig,
			[FromServices] IModelConfigurationService svc,
			[FromServices] IAIAgentFactory agentFactory,
			CancellationToken ct) =>
		{
			await svc.SaveConfigurationAsync(modelConfig, ct);
			await agentFactory.ReloadModelsAsync();
			return Results.Ok();
		});

		// -----------------------------------------------------------
		// POST reset to defaults (full reset)
		// -----------------------------------------------------------
		config.MapPost("/reset", async (
			[FromServices] IModelConfigurationService svc,
			[FromServices] IAIAgentFactory agentFactory,
			CancellationToken ct) =>
		{
			await svc.ResetToDefaultsAsync(ct);
			await agentFactory.ReloadModelsAsync();
			return Results.Ok();
		});

		// ===========================================================
		// GROUP C: Infrastructure Models (KEYED)
		// ===========================================================
		var infra = config.MapGroup("/infrastructure")
			.WithTags("Infrastructure Models")
			.WithOpenApi();

		// -----------------------------------------------------------
		// GET all infrastructure models
		// -----------------------------------------------------------
		infra.MapGet("/", async (
			[FromServices] IModelConfigurationService svc,
			CancellationToken ct) =>
		{
			var cfg = await svc.GetConfigurationAsync(ct);
			return Results.Ok(cfg.Infrastructure.Models);
		});

		// -----------------------------------------------------------
		// GET infrastructure model by key
		// -----------------------------------------------------------
		infra.MapGet("/{key}", async (
			string key,
			[FromServices] IModelConfigurationService svc,
			CancellationToken ct) =>
		{
			var cfg = await svc.GetConfigurationAsync(ct);

			if (!cfg.Infrastructure.Models.TryGetValue(key, out var model))
				return Results.NotFound($"Infrastructure model '{key}' not found.");

			return Results.Ok(model);
		});

		// -----------------------------------------------------------
		// PUT infrastructure model by key
		// -----------------------------------------------------------
		infra.MapPut("/{key}", async (
			string key,
			[FromBody] InfrastructureModelConfig model,
			[FromServices] IModelConfigurationService svc,
			[FromServices] IAIAgentFactory agentFactory,
			CancellationToken ct) =>
		{
			var cfg = await svc.GetConfigurationAsync(ct);

			model.Key = key;
			cfg.Infrastructure.Models[key] = model;

			await svc.SaveConfigurationAsync(cfg, ct);
			await agentFactory.ReloadModelsAsync();

			return Results.Ok();
		});

	}
}
