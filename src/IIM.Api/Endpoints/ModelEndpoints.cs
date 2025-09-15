using IIM.Application.Investigation;
using IIM.Core.AI;
using IIM.Core.Configuration;
using IIM.Shared.Mediator;
using IIM.Core.Templates;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

/// <summary>
/// AI model management endpoints for loading, unloading, and monitoring models
/// </summary>
public static class ModelEndpoints
{
    /// <summary>
    /// Maps all model-related endpoints for AI model lifecycle management
    /// </summary>
    public static void MapModelEndpoints(this IEndpointRouteBuilder app)
    {
        var models = app.MapGroup("/api/models")
            .WithTags("Models")
            .WithOpenApi();

        // ========================================
        // MODEL DISCOVERY & STATUS
        // ========================================

        // Get available models
        models.MapGet("/available", async (
            [FromServices] IModelOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var models = await orchestrator.GetAvailableModelsAsync(ct);
            return Results.Ok(models);
        })
        .WithName("GetAvailableModels")
        .WithSummary("Get list of all available AI models")
        .Produces<List<ModelInfo>>();

        // Get loaded models
        models.MapGet("/loaded", async (
            [FromServices] IModelOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var models = await orchestrator.GetLoadedModelsAsync(ct);
            return Results.Ok(models);
        })
        .WithName("GetLoadedModels")
        .WithSummary("Get list of currently loaded models")
        .Produces<List<ModelInfo>>();

        // Get model info
        models.MapGet("/{modelId}", async (
            string modelId,
            [FromServices] IModelOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var modelInfo = await orchestrator.GetModelInfoAsync(modelId, ct);
            return modelInfo != null
                ? Results.Ok(modelInfo)
                : Results.NotFound(new { error = $"Model {modelId} not found" });
        })
        .WithName("GetModelInfo")
        .WithSummary("Get detailed information about a specific model")
        .Produces<ModelInfo>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Get model status
        models.MapGet("/{modelId}/status", async (
            string modelId,
            [FromServices] IModelOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var isLoaded = await orchestrator.IsModelLoadedAsync(modelId, ct);
            var modelInfo = await orchestrator.GetModelInfoAsync(modelId, ct);

            var status = new ModelStatusResponse
            {
                ModelId = modelId,
                IsLoaded = isLoaded,
                Status = modelInfo?.Status ?? ModelStatus.Unknown,
                LoadedAt = modelInfo?.LoadedAt,
                MemoryUsage = modelInfo?.RequiredMemory ?? 0,
                DeviceType = modelInfo?.DeviceType ?? DeviceType.CPU,
                Metadata = modelInfo?.Parameters
            };

            return Results.Ok(status);
        })
        .WithName("GetModelStatus")
        .WithSummary("Get current status of a model")
        .Produces<ModelStatusResponse>();

        // ========================================
        // MODEL LIFECYCLE MANAGEMENT
        // ========================================

        // Load model
        models.MapPost("/{modelId}/load", async (
            string modelId,
            [FromBody] LoadModelLoadRequest? request,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new LoadModelCommand
            {
                ModelId = modelId,
                ModelPath = request?.ModelPath,
                ModelType = request?.ModelType ?? ModelType.LLM,
                ModelSize = request?.ModelSize,
                Quantization = request?.Quantization,
                ContextLength = request?.ContextLength,
                DeviceId = request?.DeviceId,
                Priority = request?.Priority,
                Parameters = request?.Parameters,
                PreloadToGpu = request?.PreloadToGpu ?? true,
                MaxMemory = request?.MaxMemory
            };

            try
            {
                var handle = await mediator.Send(command, ct);
                return Results.Ok(new
                {
                    Success = true,
                    Message = $"Model {modelId} loaded successfully",
                    Handle = handle
                });
            }
            catch (InsufficientMemoryException ex)
            {
                return Results.Problem(
                    title: "Insufficient Memory",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status507InsufficientStorage);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Model Load Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("LoadModel")
        .WithSummary("Load an AI model into memory")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status507InsufficientStorage)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Unload model
        models.MapPost("/{modelId}/unload", async (
            string modelId,
            [FromServices] IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool force = false) =>
        {
            var command = new UnloadModelCommand
            {
                ModelId = modelId,
                Force = force
            };

            var success = await mediator.Send(command, ct);
            return success
                ? Results.Ok(new { message = $"Model {modelId} unloaded successfully" })
                : Results.Problem($"Failed to unload model {modelId}");
        })
        .WithName("UnloadModel")
        .WithSummary("Unload a model from memory")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Reload model
        models.MapPost("/{modelId}/reload", async (
            string modelId,
            [FromServices] IMediator mediator,
            [FromServices] IModelOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            // Get current model info
            var modelInfo = await orchestrator.GetModelInfoAsync(modelId, ct);
            if (modelInfo == null)
            {
                return Results.NotFound(new { error = $"Model {modelId} not found" });
            }

            // Unload if loaded
            if (await orchestrator.IsModelLoadedAsync(modelId, ct))
            {
                var unloadCommand = new UnloadModelCommand { ModelId = modelId };
                await mediator.Send(unloadCommand, ct);
            }

            // Wait a moment for resources to free
            await Task.Delay(1000, ct);

            // Reload
            var loadCommand = new LoadModelCommand
            {
                ModelId = modelId,
                ModelType = modelInfo.Type,
                Parameters = modelInfo.Parameters
            };

            var handle = await mediator.Send(loadCommand, ct);
            return Results.Ok(new { message = $"Model {modelId} reloaded successfully", handle });
        })
        .WithName("ReloadModel")
        .WithSummary("Reload a model (unload then load)")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ========================================
        // MODEL RESOURCE MANAGEMENT
        // ========================================

        // Get model memory usage
        models.MapGet("/memory/usage", async (
            [FromServices] IModelOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var loadedModels = await orchestrator.GetLoadedModelsAsync(ct);
            var totalMemory = loadedModels.Sum(m => m.RequiredMemory);

            var usage = new
            {
                TotalMemoryUsed = totalMemory,
                ModelCount = loadedModels.Count,
                Models = loadedModels.Select(m => new
                {
                    m.ModelId,
                    m.RequiredMemory,
                    m.DeviceType
                }),
                Timestamp = DateTimeOffset.UtcNow
            };

            return Results.Ok(usage);
        })
        .WithName("GetModelMemoryUsage")
        .WithSummary("Get memory usage of all loaded models")
        .Produces<object>();

        //To do: Implement this
        //// Predict memory requirements
        //models.MapPost("/memory/predict", async (
        //    [FromBody] PredictMemoryRequest request,
        //    [FromServices] IModelOrchestrator orchestrator,
        //    CancellationToken ct) =>
        //{
        //    var prediction = await orchestrator.PredictMemoryRequirementsAsync(
        //        request.ModelId,
        //        request.ModelSize,
        //        request.Quantization,
        //        ct);

        //    return Results.Ok(prediction);
        //})
        //.WithName("PredictMemoryRequirements")
        //.WithSummary("Predict memory requirements for a model")
        //.Produces<MemoryPrediction>();

        // ========================================
        // MODEL CONFIGURATION
        // ========================================

        //To do implement this
        //// Update model parameters
        //models.MapPut("/{modelId}/parameters", async (
        //    string modelId,
        //    [FromBody] Dictionary<string, object> parameters,
        //    [FromServices] IModelOrchestrator orchestrator,
        //    CancellationToken ct) =>
        //{
        //    var updated = await orchestrator.UpdateModelParametersAsync(
        //        modelId,
        //        parameters,
        //        ct);

        //    return updated
        //        ? Results.Ok(new { message = "Model parameters updated" })
        //        : Results.NotFound(new { error = $"Model {modelId} not found or not loaded" });
        //})
        //.WithName("UpdateModelParameters")
        //.WithSummary("Update runtime parameters of a loaded model")
        //.RequireAuthorization()
        //.Produces<object>()
        //.ProducesProblem(StatusCodes.Status404NotFound);

        // ========================================
        // MODEL TEMPLATES
        // ========================================

        // Get model templates
        models.MapGet("/templates", async (
            [FromServices] IModelConfigurationTemplateService templateService,
            CancellationToken ct) =>
        {
            var templates = await templateService.GetTemplatesAsync(null, ct);
            return Results.Ok(templates);
        })
        .WithName("GetModelTemplates")
        .WithSummary("Get all model configuration templates")
        .Produces<List<ModelConfigurationTemplate>>();

        // Apply template
        models.MapPost("/templates/{templateId}/apply", async (
            string templateId,
            [FromServices] IModelConfigurationTemplateService templateService,
            CancellationToken ct,
            [FromQuery] string? sessionId = null) =>
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return Results.BadRequest(new { error = "SessionId is required" });
            }

            var session = await templateService.ApplyTemplateToSessionAsync(
                templateId,
                sessionId,
                ct);

            return Results.Ok(new
            {
                message = "Template applied successfully",
                session
            });
        })
        .WithName("ApplyModelTemplate")
        .WithSummary("Apply a model template to a session")
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ========================================
        // BATCH OPERATIONS
        // ========================================

        // Load multiple models
        models.MapPost("/batch/load", async (
            [FromBody] List<string> modelIds,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var results = new List<object>();

            foreach (var modelId in modelIds)
            {
                try
                {
                    var command = new LoadModelCommand
                    {
                        ModelId = modelId,
                        ModelType = ModelType.LLM
                    };

                    var handle = await mediator.Send(command, ct);
                    results.Add(new { modelId, success = true, handle });
                }
                catch (Exception ex)
                {
                    results.Add(new { modelId, success = false, error = ex.Message });
                }
            }

            return Results.Ok(results);
        })
        .WithName("BatchLoadModels")
        .WithSummary("Load multiple models in batch")
        .Produces<List<object>>();

        // Unload all models
        models.MapPost("/batch/unload-all", async (
            [FromServices] IModelOrchestrator orchestrator,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var loadedModels = await orchestrator.GetLoadedModelsAsync(ct);
            var unloadedCount = 0;

            foreach (var model in loadedModels)
            {
                try
                {
                    var command = new UnloadModelCommand
                    {
                        ModelId = model.ModelId,
                        Force = true
                    };

                    if (await mediator.Send(command, ct))
                        unloadedCount++;
                }
                catch
                {
                    // Continue unloading others
                }
            }

            return Results.Ok(new
            {
                message = $"Unloaded {unloadedCount} of {loadedModels.Count} models"
            });
        })
        .WithName("UnloadAllModels")
        .WithSummary("Unload all currently loaded models")
        .RequireAuthorization()
        .Produces<object>();
    }
}

// ========================================
// REQUEST/RESPONSE DTOs for Models
// ========================================
