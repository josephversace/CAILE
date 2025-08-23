using IIM.Core.AI;
using IIM.Core.Inference;
using IIM.Core.Models;
using IIM.Shared.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Minio.Exceptions;
using System;
using System.Threading.Tasks;

namespace IIM.Api.Endpoints;

public static class InferenceEndpoints
{
    public static void MapInferenceEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");

        // Text generation endpoint
        api.MapPost("/generate", async (
            [FromBody] GenerateRequest request,
            [FromServices] IInferencePipeline pipeline,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                // Map DTO to internal pipeline request
                var pipelineRequest = new InferencePipelineRequest
                {
                    ModelId = request.ModelId,
                    Input = request.Prompt,
                    Parameters = request.Parameters,
                    Tags = request.Tags
                };

                var result = await pipeline.ExecuteAsync<string>(pipelineRequest);

                // Return DTO response
                var response = new GenerateResponse(
                    Text: result,
                    ModelId: request.ModelId,
                    Timestamp: DateTimeOffset.UtcNow,
                    TokensUsed: null, // TODO: Get from pipeline
                    InferenceTime: null // TODO: Get from pipeline
                );

                return Results.Ok(response);
            }
            catch (ModelNotLoadedException ex)
            {
                logger.LogWarning(ex, "Model not found: {ModelId}", request.ModelId);
                return Results.NotFound(new ErrorResponse(
                    ErrorCode: "MODEL_NOT_FOUND",
                    Message: ex.Message
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Generation failed for model {ModelId}", request.ModelId);
                return Results.Problem(new ErrorResponse(
                    ErrorCode: "GENERATION_FAILED",
                    Message: "Generation failed",
                    Details: ex.Message
                ));
            }
        })
        .WithName("Generate")
        .WithOpenApi()
        .Produces<GenerateResponse>(200)
        .Produces<ErrorResponse>(404)
        .Produces<ErrorResponse>(500);

        // Batch inference endpoint
        api.MapPost("/inference/batch", async (
            [FromBody] BatchInferenceRequest request,
            [FromServices] IInferencePipeline pipeline,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var results = new List<InferenceResponse>();

                // Process batch in parallel or sequentially based on request
                if (request.Parallel)
                {
                    var tasks = request.Inputs.Select(async input =>
                    {
                        var pipelineRequest = new InferencePipelineRequest
                        {
                            ModelId = request.ModelId,
                            Input = input,
                            Parameters = request.Parameters
                        };

                        var result = await pipeline.ExecuteAsync<object>(pipelineRequest);
                        return new InferenceResponse(
                            ModelId: request.ModelId,
                            Output: result,
                            InferenceTime: TimeSpan.Zero, // TODO: Track actual time
                            TokensUsed: null,
                            Metadata: null
                        );
                    });

                    results.AddRange(await Task.WhenAll(tasks));
                }
                else
                {
                    foreach (var input in request.Inputs)
                    {
                        var pipelineRequest = new InferencePipelineRequest
                        {
                            ModelId = request.ModelId,
                            Input = input,
                            Parameters = request.Parameters
                        };

                        var result = await pipeline.ExecuteAsync<object>(pipelineRequest);
                        results.Add(new InferenceResponse(
                            ModelId: request.ModelId,
                            Output: result,
                            InferenceTime: TimeSpan.Zero, // TODO: Track actual time
                            TokensUsed: null,
                            Metadata: null
                        ));
                    }
                }

                return Results.Ok(new BatchInferenceResponse(
                    Results: results,
                    TotalCount: results.Count,
                    SuccessCount: results.Count,
                    FailureCount: 0
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Batch inference failed");
                return Results.Problem(new ErrorResponse(
                    ErrorCode: "BATCH_INFERENCE_FAILED",
                    Message: "Batch inference failed",
                    Details: ex.Message
                ));
            }
        })
        .WithName("BatchInference")
        .WithOpenApi()
        .Produces<BatchInferenceResponse>(200)
        .Produces<ErrorResponse>(500);

        // Single inference endpoint
        api.MapPost("/inference", async (
            [FromBody] InferenceRequest request,
            [FromServices] IInferencePipeline pipeline,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var pipelineRequest = new InferencePipelineRequest
                {
                    ModelId = request.ModelId,
                    Input = request.Input,
                    Parameters = request.Parameters,
                    Tags = request.Tags,
                    Priority = request.Priority,
                    Stream = request.Stream
                };

                var startTime = DateTimeOffset.UtcNow;
                var result = await pipeline.ExecuteAsync<object>(pipelineRequest);
                var inferenceTime = DateTimeOffset.UtcNow - startTime;

                var response = new InferenceResponse(
                    ModelId: request.ModelId,
                    Output: result,
                    InferenceTime: inferenceTime,
                    TokensUsed: null, // TODO: Get from pipeline
                    Metadata: new Dictionary<string, object>
                    {
                        ["priority"] = request.Priority,
                        ["stream"] = request.Stream
                    }
                );

                return Results.Ok(response);
            }
            catch (ModelNotLoadedException ex)
            {
                return Results.NotFound(new ErrorResponse(
                    ErrorCode: "MODEL_NOT_FOUND",
                    Message: ex.Message
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Inference failed");
                return Results.Problem(new ErrorResponse(
                    ErrorCode: "INFERENCE_FAILED",
                    Message: "Inference failed",
                    Details: ex.Message
                ));
            }
        })
        .WithName("Inference")
        .WithOpenApi()
        .Produces<InferenceResponse>(200)
        .Produces<ErrorResponse>(404)
        .Produces<ErrorResponse>(500);

        // Get pipeline statistics
        api.MapGet("/inference/stats", (IInferencePipeline pipeline) =>
        {
            var stats = pipeline.GetStats();

            var response = new InferencePipelineStats(
                TotalRequests: stats.TotalRequests,
                CompletedRequests: stats.CompletedRequests,
                FailedRequests: stats.FailedRequests,
                PendingRequests: stats.PendingRequests,
                ActiveRequests: stats.ActiveRequests,
                AverageLatencyMs: stats.AverageLatencyMs,
                P95LatencyMs: stats.P95LatencyMs,
                P99LatencyMs: stats.P99LatencyMs,
                RequestsByModel: stats.RequestsByModel
            );

            return Results.Ok(response);
        })
        .WithName("GetInferenceStats")
        .WithOpenApi()
        .Produces<InferencePipelineStats>(200);
    }
}