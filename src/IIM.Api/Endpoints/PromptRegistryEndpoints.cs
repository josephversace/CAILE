using IIM.Shared.Interfaces;
using IIM.Shared.Models.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

public static class PromptRegistryEndpoints
{
	public static void MapPromptRegistryEndpoints(this IEndpointRouteBuilder app)
	{
		var prompts = app.MapGroup("/api/prompts")
			.WithTags("Prompt Configuration")
			.WithOpenApi();

		// -----------------------------------------------------------
		// LIST ALL PROMPTS
		// -----------------------------------------------------------
		prompts.MapGet("/", async (
			[FromServices] IPromptStore store,
			CancellationToken ct) =>
		{
			var all = await store.GetAllAsync(ct);
			return Results.Ok(all.Values.OrderBy(p => p.Id));
		});

		// -----------------------------------------------------------
		// GET ONE PROMPT
		// -----------------------------------------------------------
		prompts.MapGet("/{id}", async (
			string id,
			[FromServices] IPromptStore store,
			CancellationToken ct) =>
		{
			var prompt = await store.GetAsync(id, ct);
			return prompt is null
				? Results.NotFound()
				: Results.Ok(prompt);
		});

		// -----------------------------------------------------------
		// CREATE / UPDATE PROMPT
		// -----------------------------------------------------------
		prompts.MapPut("/{id}", async (
			string id,
			[FromBody] PromptDefinition prompt,
			[FromServices] IPromptStore store,
			CancellationToken ct) =>
		{
			if (string.IsNullOrWhiteSpace(prompt.Content))
				return Results.BadRequest("Prompt content cannot be empty.");

			if (!string.Equals(id, prompt.Id, StringComparison.Ordinal))
				return Results.BadRequest("Prompt ID mismatch.");

			await store.SaveAsync(prompt, ct);
			return Results.Ok(prompt);
		});

		prompts.MapGet("/{id}/effective", async (
			string id,
			[FromServices] IPromptStore store,
			CancellationToken ct) =>
		{
			// 1. Try stored prompt
			var stored = await store.GetAsync(id, ct);
			if (stored != null)
			{
				return Results.Ok(new EffectivePrompt
				{
					Definition = stored,
					IsDefault = false,
					IsOverridden = true
				});
			}

			// 2. Fall back to built-in defaults
			var def = id switch
			{
				"chat.default" => new PromptDefinition
				{
					Id = id,
					Content = PromptDefaults.DefaultChat
				},
				"reasoning.default" => new PromptDefinition
				{
					Id = id,
					Content = PromptDefaults.DefaultReasoning
				},
				"analysis.text.default" => new PromptDefinition
				{
					Id = id,
					Content = PromptDefaults.DefaultTextAnalysis
				},
				"analysis.image.default" => new PromptDefinition
				{
					Id = id,
					Content = PromptDefaults.DefaultImageAnalysis
				},
				_ => null
			};

			if (def == null)
				return Results.NotFound();

			return Results.Ok(new EffectivePrompt
			{
				Definition = def,
				IsDefault = true,
				IsOverridden = false
			});
		});

		// -----------------------------------------------------------
		// DELETE / RESET PROMPT
		// -----------------------------------------------------------
		prompts.MapDelete("/{id}", async (
			string id,
			[FromServices] IPromptStore store,
			CancellationToken ct) =>
		{
			await store.DeleteAsync(id, ct);
			return Results.Ok(new { message = $"Prompt '{id}' deleted." });
		});
	}
}
