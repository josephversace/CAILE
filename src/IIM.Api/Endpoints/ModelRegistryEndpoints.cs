using IIM.Api.Services;
using IIM.Infrastructure.Foundry;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

public static class ModelRegistryEndpoints
{
	public static void MapModelRegistryEndpoints(this IEndpointRouteBuilder app)
	{
		// -----------------------------------------------------------
		// GROUP A: Foundry Model Registry
		// -----------------------------------------------------------
		var foundry = app.MapGroup("/api/modelRegistry/foundry")
			.WithTags("Foundry Models")
			.WithOpenApi();

		//// AVAILABLE
		//foundry.MapGet("/available", async (
		//	IFoundryModelService svc, CancellationToken ct) =>
		//{
		//	return Results.Ok(await svc.GetAvailableModelsAsync(ct));
		//});

		// CACHED
		//foundry.MapGet("/cached", async (
		//	[FromServices] IFoundryModelService svc, CancellationToken ct) =>
		//{
		//	return Results.Ok(await svc.GetCachedModelDtosAsync(ct));
		//});

		// LOADED
		foundry.MapGet("/loaded", async (
			[FromServices] IFoundryModelService svc, CancellationToken ct) =>
		{
			return Results.Ok(await svc.GetLoadedModelsDtoAsync(ct));
		});

		// ALL (available + flags)
		//foundry.MapGet("/all", async (
		//	[FromServices] IFoundryModelService svc, CancellationToken ct) =>
		//{
		//	return Results.Ok(await svc.GetAllWithStatusAsync(ct));
		//});

		// LOAD MODEL
		foundry.MapPost("/load/{alias}", async (
		string alias,
		[FromServices] IFoundryModelService svc,
		CancellationToken ct) =>
		{
			await svc.LoadModelAsync(alias, ct);
			return Results.Ok(new { message = $"Ensured model running: {alias}" });
		});

		// UNLOAD MODEL
		foundry.MapPost("/unload/{id}", async (
			string id,
			[FromServices] IFoundryModelService svc,
			[FromQuery] bool force,
			CancellationToken ct) =>
		{
			await svc.UnloadModelAsync(id, force, ct);
			return Results.Ok(new { message = $"Unloaded {id}" });
		});


		// -----------------------------------------------------------
		// GROUP B: Model Templates (micro / mini / small / custom)
		// -----------------------------------------------------------
		var templates = app.MapGroup("/api/modelRegistry/templates")
			.WithTags("Model Templates")
			.WithOpenApi();

		// SYSTEM TEMPLATES
		templates.MapGet("/system", async (
			[FromServices] IModelConfigurationTemplateService svc,
			CancellationToken ct) =>
		{
			return Results.Ok(await svc.GetSystemTemplatesAsync(ct));
		});

		// ACTIVE TEMPLATE
		templates.MapGet("/active", async (
			[FromServices] IModelTemplateResolver resolver,
			CancellationToken ct) =>
		{
			return Results.Ok(await resolver.GetActiveTemplateAsync(ct));
		});

		// APPLY TEMPLATE (save + foundry load)
		templates.MapPost("/apply", async (
			[FromBody] ModelTemplateDto template,
			[FromServices] IModelConfigurationTemplateService templates,
			[FromServices] IFoundryModelService foundry,
			[FromServices] IAIAgentFactory agentFactory,
			CancellationToken ct) =>
		{
			await templates.SaveDefaultTemplateAsync(template, ct);

			// fire and forget — DO NOT await
			_ = Task.Run(() => foundry.ApplyTemplateAsync(template, CancellationToken.None));

			// also reload model routing, but async
			_ = Task.Run(() => agentFactory.ReloadModelsAsync());

			return Results.Ok(new { message = "Template saved. Models are loading in background." });


			return Results.Ok(new { message = "Template saved + applied." });
		});

		var local = app.MapGroup("/api/modelRegistry/local")
		   .WithTags("Local Models")
		   .WithOpenApi();

		// --------------------------
		// LIST MODELS PER SLOT
		// --------------------------
		local.MapGet("/{slot}", async (
			string slot,
			[FromServices] ILocalModelService svc,
			CancellationToken ct) =>
		{
			var list = await svc.ListModelsAsync(slot, ct);
			return Results.Ok(list);
		});

		// --------------------------
		// UPLOAD A ZIP
		// --------------------------
		local.MapPost("/upload/{slot}/{name}", async (
			string slot,
			string name,
			IFormFile file,
			[FromServices] ILocalModelService svc,
			CancellationToken ct) =>
		{
			if (file == null || file.Length == 0)
				return Results.BadRequest("File is required.");

			var info = await svc.UploadModelAsync(slot, name, file, ct);
			return Results.Ok(info);
		});
	}
}
