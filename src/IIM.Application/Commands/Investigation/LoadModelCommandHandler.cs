using IIM.Application.Commands.Models;
using IIM.Core.AI;
using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsufficientMemoryException = IIM.Core.Models.InsufficientMemoryException;

namespace IIM.Application.Commands.Models
{
    /// <summary>
    /// Handler for loading AI models into memory.
    /// Coordinates with IModelOrchestrator to manage model lifecycle and resources.
    /// Publishes notifications for audit trail and UI updates.
    /// </summary>
    public class LoadModelCommandHandler : IRequestHandler<LoadModelCommand, ModelHandle>
    {
        private readonly IModelOrchestrator _orchestrator;
        private readonly IMediator _mediator;
        private readonly ILogger<LoadModelCommandHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the LoadModelCommandHandler.
        /// </summary>
        /// <param name="orchestrator">Model orchestrator for actual loading</param>
        /// <param name="mediator">Mediator for publishing events</param>
        /// <param name="logger">Logger for diagnostics</param>
        public LoadModelCommandHandler(
            IModelOrchestrator orchestrator,
            IMediator mediator,
            ILogger<LoadModelCommandHandler> logger)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the LoadModelCommand by delegating to the model orchestrator.
        /// Validates input, checks if model is already loaded, and publishes notifications.
        /// </summary>
        /// <param name="request">Load model command with model details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ModelHandle for the loaded model</returns>
        public async Task<ModelHandle> Handle(LoadModelCommand request, CancellationToken cancellationToken)
        {
            // Validate request
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ModelId))
            {
                throw new ArgumentException("ModelId is required", nameof(request));
            }

            try
            {
                _logger.LogInformation(
                    "Processing LoadModelCommand for model {ModelId} of type {ModelType}",
                    request.ModelId,
                    request.ModelType);

                // Check if model is already loaded
                var isLoaded = await _orchestrator.IsModelLoadedAsync(request.ModelId, cancellationToken);
                if (isLoaded)
                {
                    _logger.LogWarning("Model {ModelId} is already loaded, returning existing handle", request.ModelId);

                    // Get existing model info
                    var modelInfo = await _orchestrator.GetModelInfoAsync(request.ModelId, cancellationToken);
                    if (modelInfo != null)
                    {
                        // Return existing handle
                        return new ModelHandle
                        {
                            ModelId = modelInfo.ModelId,
                            SessionId = modelInfo.SessionId ?? Guid.NewGuid().ToString(),
                            LoadedAt = modelInfo.LoadedAt ?? DateTimeOffset.UtcNow,
                            State = ModelState.Ready,
                            MemoryUsage = modelInfo.RequiredMemory,
                            Metadata = modelInfo.Metadata ?? new Dictionary<string, object>()
                        };
                    }
                }

                // Publish loading started notification
                await _mediator.Publish(new ModelLoadingStartedNotification
                {
                    ModelId = request.ModelId,
                    ModelType = request.ModelType,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);

                // Create progress reporter for UI updates
                var progress = new Progress<float>(percent =>
                {
                    // Fire and forget progress update
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _mediator.Publish(new ModelLoadingProgressNotification
                            {
                                ModelId = request.ModelId,
                                Progress = percent,
                                Timestamp = DateTimeOffset.UtcNow
                            }, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to publish progress notification");
                        }
                    });
                });

                // Convert command to ModelRequest for orchestrator
                var modelRequest = new ModelRequest
                {
                    ModelId = request.ModelId,
                    ModelPath = request.ModelPath,
                    ModelType = request.ModelType,
                    ModelSize = request.ModelSize,
                    Quantization = request.Quantization ?? "default",
                    ContextSize = request.ContextLength ?? 2048,
                    DeviceId = request.DeviceId,
                    Priority = request.Priority ?? ModelPriority.Balanced,
                    CustomOptions = request.Parameters,
                    Options = request.Parameters
                };

                // Load the model through orchestrator
                var handle = await _orchestrator.LoadModelAsync(
                    modelRequest,
                    progress,
                    cancellationToken);

                // Validate handle
                if (handle == null)
                {
                    throw new InvalidOperationException($"Failed to load model {request.ModelId}: null handle returned");
                }

                _logger.LogInformation(
                    "Model {ModelId} loaded successfully. SessionId: {SessionId}, Memory: {Memory:N0} MB",
                    handle.ModelId,
                    handle.SessionId,
                    handle.MemoryUsage / (1024 * 1024));

                // Publish loaded notification for audit and UI updates
                await _mediator.Publish(new ModelLoadedNotification
                {
                    ModelId = request.ModelId,
                    SessionId = handle.SessionId,
                    ModelType = request.ModelType,
                    MemoryUsage = handle.MemoryUsage,
                    LoadedAt = handle.LoadedAt,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);

                // Check resource thresholds and publish warning if needed
                var totalMemory = await _orchestrator.GetTotalMemoryUsageAsync(cancellationToken);
                const long WarningThreshold = 100L * 1024 * 1024 * 1024; // 100GB

                if (totalMemory > WarningThreshold)
                {
                    await _mediator.Publish(new ResourceThresholdExceededNotification
                    {
                        ResourceType = "Memory",
                        CurrentValue = totalMemory,
                        ThresholdValue = WarningThreshold,
                        ModelId = request.ModelId,
                        Timestamp = DateTimeOffset.UtcNow
                    }, cancellationToken);

                    _logger.LogWarning(
                        "Memory usage exceeded warning threshold. Current: {Current:N0} GB, Threshold: {Threshold:N0} GB",
                        totalMemory / (1024 * 1024 * 1024),
                        WarningThreshold / (1024 * 1024 * 1024));
                }

                return handle;
            }
            catch (InsufficientMemoryException ex)
            {
                _logger.LogError(ex,
                    "Insufficient memory to load model {ModelId}. Required: {Required:N0} MB, Available: {Available:N0} MB",
                    request.ModelId,
                    ex.Context?["required"] as long? / (1024 * 1024),
                    ex.Context?["available"] as long? / (1024 * 1024));

                // Publish failure notification
                await PublishFailureNotification(request.ModelId, "Insufficient memory", cancellationToken);

                throw;
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Model file not found for {ModelId} at path: {Path}",
                    request.ModelId, request.ModelPath);

                // Publish failure notification
                await PublishFailureNotification(request.ModelId, "Model file not found", cancellationToken);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model {ModelId}", request.ModelId);

                // Publish failure notification
                await PublishFailureNotification(request.ModelId, ex.Message, cancellationToken);

                throw;
            }
        }

        /// <summary>
        /// Publishes a model loading failure notification.
        /// Used for audit trail and UI error display.
        /// </summary>
        private async Task PublishFailureNotification(
            string modelId,
            string reason,
            CancellationToken cancellationToken)
        {
            try
            {
                await _mediator.Publish(new ModelLoadingFailedNotification
                {
                    ModelId = modelId,
                    Reason = reason,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish failure notification for model {ModelId}", modelId);
            }
        }
    }





}




