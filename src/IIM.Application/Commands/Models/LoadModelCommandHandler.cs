using IIM.Core.AI;
using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Commands.Models
{
    /// <summary>
    /// Fixed handler for loading AI models
    /// </summary>
    public class LoadModelCommandHandler : IRequestHandler<LoadModelCommand, ModelHandle>
    {
        private readonly IModelOrchestrator _orchestrator;
        private readonly IMediator _mediator;
        private readonly ILogger<LoadModelCommandHandler> _logger;
        private readonly string _modelsPath;

        public LoadModelCommandHandler(
            IModelOrchestrator orchestrator,
            IMediator mediator,
            ILogger<LoadModelCommandHandler> logger)
        {
            _orchestrator = orchestrator;
            _mediator = mediator;
            _logger = logger;

            _modelsPath = Environment.GetEnvironmentVariable("IIM_MODELS_PATH")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIM", "Models");

            Directory.CreateDirectory(_modelsPath);
        }

        public async Task<ModelHandle> Handle(LoadModelCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Loading model {ModelId}", request.ModelId);

                // Check if already loaded
                if (await _orchestrator.IsModelLoadedAsync(request.ModelId, cancellationToken))
                {
                    _logger.LogWarning("Model {ModelId} is already loaded", request.ModelId);

                    var existingInfo = await _orchestrator.GetModelInfoAsync(request.ModelId, cancellationToken);

                    if (existingInfo != null)
                    {
                        return new ModelHandle
                        {
                            ModelId = request.ModelId,
                            Provider = existingInfo.Provider,
                            Type = existingInfo.Type,
                            MemoryUsage = existingInfo.MemoryUsage,
                            SessionId = Guid.NewGuid().ToString("N")
                        };
                    }
                }

                // Check resources
                var gpuStats = await _orchestrator.GetGpuStatsAsync(cancellationToken);
                _logger.LogInformation("GPU Memory: {Used}/{Total} MB",
                    gpuStats.UsedMemory / (1024 * 1024),
                    gpuStats.TotalMemory / (1024 * 1024));

                // Download if needed
                var modelPath = request.ModelPath ?? Path.Combine(_modelsPath, request.ModelId);

                if (!File.Exists(modelPath) && !string.IsNullOrEmpty(request.DownloadUrl))
                {
                    _logger.LogInformation("Downloading model from {Url}", request.DownloadUrl);

                    var downloadProgress = new Progress<DownloadProgress>(progress =>
                    {
                        _logger.LogDebug("Download: {Percent}%", progress.ProgressPercent);
                    });

                    var downloadSuccess = await _orchestrator.DownloadModelAsync(
                        request.ModelId,
                        request.DownloadUrl,
                        downloadProgress,
                        cancellationToken);

                    if (!downloadSuccess)
                    {
                        throw new InvalidOperationException($"Failed to download model {request.ModelId}");
                    }
                }

                // Create model request with only the properties it actually has
                var modelRequest = new ModelRequest
                {
                    ModelId = request.ModelId,
                    ModelType = request.ModelType,
                    Provider = request.Provider ?? DetermineProvider(request.ModelType),
                    ModelPath = modelPath
                    // Note: ModelRequest doesn't have Parameters or Device properties
                };

                // Load the model
                _logger.LogInformation("Loading model with provider {Provider}", modelRequest.Provider);

                var loadProgress = new Progress<float>(percent =>
                {
                    _logger.LogDebug("Load progress: {Percent:P}", percent);
                });

                var handle = await _orchestrator.LoadModelAsync(modelRequest, loadProgress, cancellationToken);

                // Warm up if requested
                if (request.WarmUp)
                {
                    _logger.LogInformation("Warming up model {ModelId}", request.ModelId);
                    await WarmUpModelAsync(handle, cancellationToken);
                }

                stopwatch.Stop();

                _logger.LogInformation("Model {ModelId} loaded in {ElapsedMs}ms",
                    handle.ModelId, stopwatch.ElapsedMilliseconds);

                // Publish notification
                await _mediator.Publish(new ModelLoadedNotification
                {
                    ModelId = handle.ModelId,
                    Provider = handle.Provider,
                    Type = handle.Type,
                    MemoryUsage = handle.MemoryUsage,
                    LoadTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);

                return handle;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Failed to load model {ModelId}", request.ModelId);

                await _mediator.Publish(new ModelLoadFailedNotification
                {
                    ModelId = request.ModelId,
                    Error = ex.Message,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);

                throw;
            }
        }

        private string DetermineProvider(ModelType modelType)
        {
            return modelType switch
            {
                ModelType.LLM => "Ollama",
                ModelType.Whisper => "ONNX",
                ModelType.CLIP => "ONNX",
                ModelType.Embedding => "ONNX",
                _ => "ONNX"
            };
        }

        private async Task WarmUpModelAsync(ModelHandle handle, CancellationToken cancellationToken)
        {
            try
            {
                object warmUpRequest = handle.Type switch
                {
                    ModelType.LLM => "Hello, this is a warm-up prompt.",
                    ModelType.Whisper => new byte[16000],
                    ModelType.CLIP => new byte[224 * 224 * 3],
                    ModelType.Embedding => "Warm-up text",
                    _ => "Default warm-up"
                };

                await _orchestrator.InferAsync(handle.ModelId, warmUpRequest, cancellationToken);
                _logger.LogDebug("Model {ModelId} warmed up", handle.ModelId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm up model {ModelId}", handle.ModelId);
            }
        }
    }
}