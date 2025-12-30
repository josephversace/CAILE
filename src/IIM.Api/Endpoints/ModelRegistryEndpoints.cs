// IIM.Api/Endpoints/ModelRegistryEndpoints.cs
using IIM.Api.Services;
using IIM.Infrastructure.Ollama;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

public static class ModelRegistryEndpoints
{
	public static void MapModelRegistryEndpoints(this IEndpointRouteBuilder app)
	{
		// -----------------------------------------------------------
		// GROUP A: Ollama Model Registry
		// -----------------------------------------------------------
		var ollama = app.MapGroup("/api/models/ollama")
			.WithTags("Ollama Models")
			.WithOpenApi();

		// LOADED
		ollama.MapGet("/loaded", async (
			[FromServices] IModelService svc,
			CancellationToken ct) =>
		{
			return Results.Ok(await svc.GetLoadedModelsDtoAsync(ct));
		});

		// CACHED (locally available)
		ollama.MapGet("/cached", async (
			[FromServices] IModelService svc,
			CancellationToken ct) =>
		{
			return Results.Ok(await svc.GetCachedModelsDtoAsync(ct));
		});

		// LOAD MODEL
		ollama.MapPost("/load/{model}", async (
			string model,
			[FromServices] IModelService svc,
			CancellationToken ct) =>
		{
			await svc.LoadModelAsync(model, ct);
			return Results.Ok(new { message = $"Model loaded: {model}" });
		});

		// UNLOAD MODEL
		ollama.MapPost("/unload/{model}", async (
			string model,
			[FromServices] IModelService svc,
			[FromQuery] bool force,
			CancellationToken ct) =>
		{
			await svc.UnloadModelAsync(model, force, ct);
			return Results.Ok(new { message = $"Unloaded {model}" });
		});

		// -----------------------------------------------------------
		// GROUP B: Model Configuration
		// -----------------------------------------------------------
		var config = app.MapGroup("/api/models/config")
			.WithTags("Model Configuration")
			.WithOpenApi();

		// GET current configuration
		config.MapGet("/", async (
			[FromServices] IModelConfigurationService svc,
			CancellationToken ct) =>
		{
			return Results.Ok(await svc.GetConfigurationAsync(ct));
		});

		// GET active models only
		config.MapGet("/active", async (
			[FromServices] IModelResolver resolver,
			CancellationToken ct) =>
		{
			var primary = await resolver.GetPrimaryModelAsync(ct);
			var secondary = await resolver.GetSecondaryModelAsync(ct);

			return Results.Ok(new
			{
				primary = primary,
				secondary = secondary
			});
		});

		// UPDATE active models
		config.MapPut("/active", async (
			[FromBody] ActiveModelsConfig active,
			[FromServices] IModelConfigurationService configSvc,
			[FromServices] IModelService modelSvc,
			[FromServices] IAIAgentFactory agentFactory,
			CancellationToken ct) =>
		{
			// Save to DB
			await configSvc.SaveActiveModelsAsync(active, ct);

			// Load models in background
			_ = Task.Run(async () =>
			{
				try
				{
					await modelSvc.LoadModelForSlotAsync(active.Primary.ModelId, "primary", CancellationToken.None);

					if (active.Secondary != null && !string.IsNullOrEmpty(active.Secondary.ModelId))
					{
						await modelSvc.LoadModelForSlotAsync(active.Secondary.ModelId, "secondary", CancellationToken.None);
					}

					await agentFactory.ReloadModelsAsync();
				}
				catch (Exception)
				{
					// Log error - fire and forget
				}
			});

			return Results.Ok(new { message = "Active models updated. Loading in background." });
		});

		// RESET to defaults
		config.MapPost("/reset", async (
			[FromServices] IModelConfigurationService configSvc,
			[FromServices] IAIAgentFactory agentFactory,
			CancellationToken ct) =>
		{
			await configSvc.ResetActiveModelsAsync(ct);
			await agentFactory.ReloadModelsAsync();

			return Results.Ok(new { message = "Reset to default configuration." });
		});

		// -----------------------------------------------------------
		// GROUP C: Local ONNX Models
		// -----------------------------------------------------------
		var local = app.MapGroup("/api/models/local")
			.WithTags("Local Models")
			.WithOpenApi();

		// LIST MODELS PER SLOT
		local.MapGet("/{slot}", async (
			string slot,
			[FromServices] ILocalModelService svc,
			CancellationToken ct) =>
		{
			var list = await svc.ListModelsAsync(slot, ct);
			return Results.Ok(list);
		});

		// UPLOAD A ZIP
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

		// -----------------------------------------------------------
		// GROUP D: Chat Model Selection (for ChatPanel dropdowns)
		// -----------------------------------------------------------
		var chatModels = app.MapGroup("/api/models")
			.WithTags("Chat Models")
			.WithOpenApi();

		// Get multimodal models for Primary slot
		chatModels.MapGet("/primary", async (
			[FromServices] IModelService svc,
			CancellationToken ct) =>
		{
			var models = await svc.GetPrimaryModelsAsync(ct);
			return Results.Ok(models);
		});

		// Get reasoning models for Secondary slot
		chatModels.MapGet("/secondary", async (
			[FromServices] IModelService svc,
			CancellationToken ct) =>
		{
			var models = await svc.GetSecondaryModelsAsync(ct);
			return Results.Ok(models);
		});

		// Get currently active models (simple response for UI)
		chatModels.MapGet("/active", (
			[FromServices] IAIAgentFactory factory) =>
		{
			return Results.Ok(new
			{
				primary = factory.CurrentChatModel,
				secondary = factory.CurrentReasoningModel
			});
		});

		// Pull a model from Ollama registry
		chatModels.MapPost("/pull/{model}", async (
			string model,
			[FromServices] IModelService svc,
			CancellationToken ct) =>
		{
			try
			{
				await svc.LoadModelAsync(model, ct);
				return Results.Ok(new { success = true, message = $"Model {model} pulled and loaded" });
			}
			catch (Exception ex)
			{
				return Results.BadRequest(new { success = false, message = ex.Message });
			}
		});

		// Load model into a specific slot
		chatModels.MapPost("/load/{slot}/{model}", async (
			string slot,
			string model,
			[FromServices] IModelService modelSvc,
			[FromServices] IModelConfigurationService configSvc,
			[FromServices] IAIAgentFactory agentFactory,
			CancellationToken ct) =>
		{
			try
			{
				// 1. Load the model (unloads previous in that slot)
				await modelSvc.LoadModelForSlotAsync(model, slot, ct);

				// 2. Update and persist the configuration
				var config = await configSvc.GetConfigurationAsync(ct);

				var newActive = new ActiveModelsConfig
				{
					Primary = config.Active.Primary,
					Secondary = config.Active.Secondary
				};

				switch (slot.ToLowerInvariant())
				{
					case "primary":
					case "chat":
						newActive.Primary = new ActiveModelConfig
						{
							ModelId = model,
							Temperature = config.Active.Primary.Temperature,
							MaxTokens = config.Active.Primary.MaxTokens,
							TopP = config.Active.Primary.TopP,
							SystemPrompt = config.Active.Primary.SystemPrompt,
							SupportsVision = config.Active.Primary.SupportsVision
						};
						break;
					case "secondary":
					case "reasoning":
						newActive.Secondary = new ActiveModelConfig
						{
							ModelId = model,
							Temperature = config.Active.Secondary?.Temperature,
							MaxTokens = config.Active.Secondary?.MaxTokens,
							TopP = config.Active.Secondary?.TopP,
							SystemPrompt = config.Active.Secondary?.SystemPrompt,
							SupportsVision = false
						};
						break;
				}

				await configSvc.SaveActiveModelsAsync(newActive, ct);

				// 3. Reload the AI agents
				await agentFactory.ReloadModelsAsync();

				return Results.Ok(new { success = true, message = $"Loaded {model} into {slot} slot" });
			}
			catch (Exception ex)
			{
				return Results.BadRequest(new { success = false, message = ex.Message });
			}
		});

		// Unload a slot (set to none)
		chatModels.MapPost("/unload/{slot}", async (
			string slot,
			[FromServices] IModelService modelSvc,
			[FromServices] IModelConfigurationService configSvc,
			[FromServices] IAIAgentFactory agentFactory,
			CancellationToken ct) =>
		{
			try
			{
				// Only secondary can be unloaded
				if (slot.ToLowerInvariant() is not ("secondary" or "reasoning"))
				{
					return Results.BadRequest(new { success = false, message = "Only secondary slot can be unloaded" });
				}

				await modelSvc.UnloadSlotAsync(slot, ct);

				// Update config
				var config = await configSvc.GetConfigurationAsync(ct);
				var newActive = new ActiveModelsConfig
				{
					Primary = config.Active.Primary,
					Secondary = null
				};
				await configSvc.SaveActiveModelsAsync(newActive, ct);

				await agentFactory.ReloadModelsAsync();

				return Results.Ok(new { success = true, message = $"Unloaded {slot} slot" });
			}
			catch (Exception ex)
			{
				return Results.BadRequest(new { success = false, message = ex.Message });
			}
		});
	}
}