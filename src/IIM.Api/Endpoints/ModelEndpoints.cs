using IIM.Application.Commands.Models;
using IIM.Core.AI;
using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Shared.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Minio.Exceptions;
using OpenAI.ObjectModels.ResponseModels.ModelResponseModels;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Api.Endpoints;

public static class ModelEndpoints
{
    public static void MapModelEndpoints(this IEndpointRouteBuilder app)
    {
        var models = app.MapGroup("/api/v1/models")
            .RequireAuthorization();

        // Load a model
        models.MapPost("/load", async (
            [FromServices] IMediator mediator,
            [FromBody] ModelLoadRequest request) =>
        {
            var command = new LoadModelCommand
            {
                ModelId = request.ModelId,
                ModelPath = request.ModelPath,
                Provider = request.Provider,
                Options = request.Options
            };

            var result = await mediator.Send(command);

            var response = new ModelOperationResponse(
                Success: result.Success,
                Message: result.Message,
                ModelInfo: MapToModelInfo(result.Model)
            );

            return Results.Ok(response);
        })
        .WithName("LoadModel")
        .Produces<ModelOperationResponse>(200)
        .Produces<ErrorResponse>(400);

        // Unload a model
        models.MapPost("/{modelId}/unload", async (
            string modelId,
            [FromServices] IMediator mediator) =>
        {
            var command = new UnloadModelCommand { ModelId = modelId };
            var result = await mediator.Send(command);

            var response = new ModelOperationResponse(
                Success: result.Success,
                Message: result.Message,
                ModelInfo: null
            );

            return Results.Ok(response);
        })
        .WithName("UnloadModel")
        .Produces<ModelOperationResponse>(200);

        // Get all available models
        models.MapGet("/", async ([FromServices] IModelOrchestrator orchestrator) =>
        {
            var models = await orchestrator.GetAvailableModelsAsync();
            var stats = await orchestrator.GetStatsAsync();

            var response = new ModelListResponse(
                Models: models.Select(MapToModelInfo).ToList(),
                TotalMemoryUsage: stats.TotalMemoryUsage,
                AvailableMemory: stats.AvailableMemory,
                LoadedCount: stats.LoadedModels,
                AvailableCount: models.Length
            );

            return Results.Ok(response);
        })
        .WithName("GetModels")
        .Produces<ModelListResponse>(200);

        // Get specific model information
        models.MapGet("/{modelId}", async (
            string modelId,
            [FromServices] IModelOrchestrator orchestrator) =>
        {
            var model = await orchestrator.GetModelAsync(modelId);
            if (model == null)
            {
                return Results.NotFound(new ErrorResponse(
                    ErrorCode: "MODEL_NOT_FOUND",
                    Message: $"Model {modelId} not found"
                ));
            }

            return Results.Ok(MapToModelInfo(model));
        })
        .WithName("GetModel")
        .Produces<ModelInfo>(200)
        .Produces<ErrorResponse>(404);

        // Configure a model
        models.MapPost("/{modelId}/configure", async (
            string modelId,
            [FromBody] ModelConfigurationRequest request,
            [FromServices] IMediator mediator) =>
        {
            var command = new ConfigureModelCommand
            {
                ModelId = modelId,
                Parameters = request.Parameters
            };

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("ConfigureModel")
        .Produces<ModelOperationResponse>(200);

        // Get model statistics
        models.MapGet("/{modelId}/stats", async (
            string modelId,
            [FromServices] IModelOrchestrator orchestrator) =>
        {
            var stats = await orchestrator.GetModelStatsAsync(modelId);
            if (stats == null)
            {
                return Results.NotFound(new ErrorResponse(
                    ErrorCode: "MODEL_NOT_FOUND",
                    Message: $"Model {modelId} not found"
                ));
            }

            return Results.Ok(stats);
        })
        .WithName("GetModelStats")
        .Produces<ModelStats>(200)
        .Produces<ErrorResponse>(404);
    }

    // Helper method to map internal model to DTO
    private static ModelInfo MapToModelInfo(Model model)
    {
        return new ModelInfo(
            Id: model.Id,
            Name: model.Name,
            Type: model.Type,
            Provider: model.Provider,
            Status: model.Status,
            MemoryUsage: model.MemoryUsage,
            LoadedPath: model.LoadedPath,
            LoadedAt: model.LoadedAt,
            Capabilities: new ModelCapabilities(
                MaxContextLength: model.MaxContextLength,
                SupportedLanguages: model.SupportedLanguages,
                SpecialFeatures: model.SpecialFeatures,
                SupportsStreaming: model.SupportsStreaming,
                SupportsFineTuning: model.SupportsFineTuning,
                SupportsMultiModal: model.SupportsMultiModal,
                CustomCapabilities: model.CustomCapabilities
            ),
            Metadata: model.Metadata
        );
    }
}