using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Models;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace IIM.Core.AI
{
    /// <summary>
    /// Default implementation of IModelOrchestrator for managing AI model lifecycle
    /// Handles model loading/unloading, memory management, and inference coordination
    /// </summary>
    public class DefaultModelOrchestrator : IModelOrchestrator
    {
        private readonly ILogger<DefaultModelOrchestrator> _logger;
        private readonly ConcurrentDictionary<string, LoadedModel> _loadedModels = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private readonly string _modelsPath;

        // Memory management
        private const long MaxMemoryBytes = 120L * 1024 * 1024 * 1024; // 120GB for Framework laptop
        private const long MemoryWarningThreshold = 100L * 1024 * 1024 * 1024; // Warn at 100GB

        // Events
        public event EventHandler<ModelLoadedEventArgs>? ModelLoaded;
        public event EventHandler<ModelUnloadedEventArgs>? ModelUnloaded;
        public event EventHandler<ModelErrorEventArgs>? ModelError;
        public event EventHandler<ResourceThresholdEventArgs>? ResourceThresholdExceeded;

        public DefaultModelOrchestrator(ILogger<DefaultModelOrchestrator> logger)
        {
            _logger = logger;
            _modelsPath = Environment.GetEnvironmentVariable("IIM_MODELS_PATH")
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");

            if (!Directory.Exists(_modelsPath))
            {
                Directory.CreateDirectory(_modelsPath);
                _logger.LogInformation("Created models directory at {Path}", _modelsPath);
            }
        }

        /// <summary>
        /// Loads a model into memory with resource management
        /// </summary>
        public async Task<ModelHandle> LoadModelAsync(
            ModelRequest request,
            IProgress<float>? progress = null,
            CancellationToken cancellationToken = default)
        {
            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("Loading model {ModelId} of type {Type}",
                    request.ModelId, request.ModelType);

                // Check if already loaded
                if (_loadedModels.ContainsKey(request.ModelId))
                {
                    _logger.LogInformation("Model {ModelId} already loaded", request.ModelId);
                    return _loadedModels[request.ModelId].Handle;
                }

                // Estimate memory requirement
                var estimatedMemory = EstimateModelMemory(request);
                var currentMemory = GetCurrentMemoryUsage();

                // Check memory availability
                if (currentMemory + estimatedMemory > MaxMemoryBytes)
                {
                    _logger.LogWarning("Insufficient memory for model {ModelId}. Required: {Required}GB, Available: {Available}GB",
                        request.ModelId,
                        estimatedMemory / (1024 * 1024 * 1024),
                        (MaxMemoryBytes - currentMemory) / (1024 * 1024 * 1024));

                    // Try to free memory
                    await OptimizeMemoryAsync(cancellationToken);

                    // Check again
                    currentMemory = GetCurrentMemoryUsage();
                    if (currentMemory + estimatedMemory > MaxMemoryBytes)
                    {
                        throw new InvalidOperationException(
                            $"Cannot load model {request.ModelId}. Insufficient memory.");
                    }
                }

                // Simulate model loading with progress
                var stopwatch = Stopwatch.StartNew();
                for (int i = 0; i <= 100; i += 10)
                {
                    progress?.Report(i / 100f);
                    await Task.Delay(100, cancellationToken); // Simulate loading time
                }

                // Create loaded model entry using EXISTING ModelHandle from IIM.Core.Models
                var handle = new ModelHandle
                {
                    ModelId = request.ModelId,
                    SessionId = Guid.NewGuid().ToString("N"),
                    Provider = request.Provider ?? DetermineProvider(request.ModelType),
                    Type = request.ModelType,
                    MemoryUsage = estimatedMemory,
                    LoadedAt = DateTimeOffset.UtcNow
                };

                var loadedModel = new LoadedModel
                {
                    Handle = handle,
                    Configuration = new ModelConfiguration
                    {
                        ModelId = request.ModelId,
                        Provider = handle.Provider,
                        Type = request.ModelType,
                        Status = ModelStatus.Loaded,
                        MemoryUsage = estimatedMemory,
                        LoadedPath = request.ModelPath,
                        LoadedAt = DateTimeOffset.UtcNow,
                        Parameters = request.Options ?? new Dictionary<string, object>()
                    },
                    Process = null, // Would be actual process in production
                    LastAccessed = DateTimeOffset.UtcNow
                };

                _loadedModels[request.ModelId] = loadedModel;
                stopwatch.Stop();

                // Raise event
                ModelLoaded?.Invoke(this, new ModelLoadedEventArgs
                {
                    ModelId = request.ModelId,
                    Type = request.ModelType,
                    MemoryUsage = estimatedMemory,
                    LoadTime = stopwatch.Elapsed
                });

                // Check memory warning threshold
                if (currentMemory + estimatedMemory > MemoryWarningThreshold)
                {
                    ResourceThresholdExceeded?.Invoke(this, new ResourceThresholdEventArgs
                    {
                        ResourceType = "Memory",
                        CurrentUsage = (currentMemory + estimatedMemory) / (float)MaxMemoryBytes * 100,
                        Threshold = MemoryWarningThreshold / (float)MaxMemoryBytes * 100,
                        Recommendation = "Consider unloading unused models"
                    });
                }

                _logger.LogInformation("Model {ModelId} loaded successfully in {Time}ms",
                    request.ModelId, stopwatch.ElapsedMilliseconds);

                return handle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model {ModelId}", request.ModelId);
                ModelError?.Invoke(this, new ModelErrorEventArgs
                {
                    ModelId = request.ModelId,
                    Error = ex.Message,
                    Exception = ex
                });
                throw;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        /// <summary>
        /// Performs inference using a loaded model
        /// Uses EXISTING models from IIM.Core.Models
        /// </summary>
        public async Task<InferenceResult> InferAsync(string modelId, object input, CancellationToken ct = default)
        {
            if (!_loadedModels.TryGetValue(modelId, out var loadedModel))
            {
                throw new InvalidOperationException($"Model {modelId} is not loaded");
            }

            var stopwatch = Stopwatch.StartNew();
            loadedModel.LastAccessed = DateTimeOffset.UtcNow;
            loadedModel.AccessCount++;

            try
            {
                // Route to appropriate inference method based on model type
                object output = loadedModel.Configuration.Type switch
                {
                    ModelType.Whisper => await InferWhisperAsync(input, loadedModel, ct),
                    ModelType.CLIP => await InferCLIPAsync(input, loadedModel, ct),
                    ModelType.Embedding => await InferEmbeddingAsync(input, loadedModel, ct),
                    ModelType.LLM => await InferLLMAsync(input, loadedModel, ct),
                    _ => throw new NotSupportedException($"Model type {loadedModel.Configuration.Type} not supported")
                };

                stopwatch.Stop();

                // Calculate metrics
                var tokensProcessed = EstimateTokens(input, loadedModel.Configuration.Type);
                var tokensPerSecond = tokensProcessed / stopwatch.Elapsed.TotalSeconds;

                return new InferenceResult
                {
                    ModelId = modelId,
                    Output = output,
                    InferenceTime = stopwatch.Elapsed,
                    TokensProcessed = tokensProcessed,
                    TokensPerSecond = tokensPerSecond
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inference failed for model {ModelId}", modelId);
                throw;
            }
        }

        /// <summary>
        /// Unloads a model from memory
        /// </summary>
        public async Task<bool> UnloadModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (_loadedModels.TryRemove(modelId, out var model))
            {
                _logger.LogInformation("Unloading model {ModelId}", modelId);

                // Clean up resources
                model.Process?.Kill();
                model.Process?.Dispose();

                // Simulate cleanup time
                await Task.Delay(100, cancellationToken);

                ModelUnloaded?.Invoke(this, new ModelUnloadedEventArgs
                {
                    ModelId = modelId,
                    Reason = "Manual unload"
                });

                return true;
            }

            return false;
        }

        public Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_loadedModels.ContainsKey(modelId));
        }

        public Task<List<ModelConfiguration>> GetLoadedModelsAsync(CancellationToken cancellationToken = default)
        {
            var configs = _loadedModels.Values
                .Select(m => m.Configuration)
                .ToList();
            return Task.FromResult(configs);
        }

        public Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            // In production, this would scan available models from disk/registry
            var available = new List<ModelConfiguration>
            {
                new ModelConfiguration { ModelId = "whisper-base", Type = ModelType.Whisper, Status = ModelStatus.Available },
                new ModelConfiguration { ModelId = "clip-vit-base", Type = ModelType.CLIP, Status = ModelStatus.Available },
                new ModelConfiguration { ModelId = "bge-base-en", Type = ModelType.Embedding, Status = ModelStatus.Available },
                new ModelConfiguration { ModelId = "llama-3.1-8b", Type = ModelType.LLM, Status = ModelStatus.Available },
            };

            // Add loaded models
            available.AddRange(_loadedModels.Values.Select(m => m.Configuration));

            return Task.FromResult(available.DistinctBy(m => m.ModelId).ToList());
        }

        public Task<ModelConfiguration?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (_loadedModels.TryGetValue(modelId, out var model))
            {
                return Task.FromResult<ModelConfiguration?>(model.Configuration);
            }
            return Task.FromResult<ModelConfiguration?>(null);
        }

        public Task<bool> UpdateModelParametersAsync(string modelId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            if (_loadedModels.TryGetValue(modelId, out var model))
            {
                foreach (var param in parameters)
                {
                    model.Configuration.Parameters[param.Key] = param.Value;
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<GpuStats> GetGpuStatsAsync(CancellationToken cancellationToken = default)
        {
            // Returns EXISTING GpuStats from the interface
            return Task.FromResult(new GpuStats
            {
                DeviceName = "AMD Radeon (Framework Laptop)",
                TotalMemory = 24L * 1024 * 1024 * 1024,
                UsedMemory = GetCurrentMemoryUsage(),
                AvailableMemory = MaxMemoryBytes - GetCurrentMemoryUsage(),
                UtilizationPercent = (_loadedModels.Count > 0) ? 45.5f : 0f,
                TemperatureCelsius = 65.0f,
                PowerWatts = 95.0f,
                IsROCmAvailable = CheckROCmSupport(),
                IsDirectMLAvailable = CheckDirectMLSupport()
            });
        }

        public Task<ModelResourceUsage> GetModelResourceUsageAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (_loadedModels.TryGetValue(modelId, out var model))
            {
                // Returns EXISTING ModelResourceUsage from the interface
                return Task.FromResult(new ModelResourceUsage
                {
                    ModelId = modelId,
                    MemoryBytes = model.Handle.MemoryUsage,
                    VramBytes = model.Handle.MemoryUsage * 8 / 10, // Estimate 80% in VRAM
                    CpuPercent = 10.0f,
                    GpuPercent = 25.0f,
                    ActiveSessions = 1,
                    Uptime = DateTimeOffset.UtcNow - model.Handle.LoadedAt
                });
            }

            return Task.FromResult(new ModelResourceUsage { ModelId = modelId });
        }

        public async Task<bool> OptimizeMemoryAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Optimizing memory by unloading least recently used models");

            // Find LRU models
            var lruModels = _loadedModels.Values
                .OrderBy(m => m.LastAccessed)
                .Take(_loadedModels.Count / 3) // Unload bottom third
                .ToList();

            foreach (var model in lruModels)
            {
                await UnloadModelAsync(model.Handle.ModelId, cancellationToken);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return true;
        }

        public Task<long> GetTotalMemoryUsageAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetCurrentMemoryUsage());
        }

        public Task<ModelStats> GetStatsAsync()
        {
            // Uses EXISTING ModelStats from IIM.Core.Models
            var stats = new ModelStats
            {
                LoadedModels = _loadedModels.Count,
                TotalMemoryUsage = GetCurrentMemoryUsage(),
                AvailableMemory = MaxMemoryBytes - GetCurrentMemoryUsage(),
                Models = _loadedModels.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ModelInfo
                    {
                        ModelId = kvp.Key,
                        Type = kvp.Value.Configuration.Type,
                        MemoryUsage = kvp.Value.Handle.MemoryUsage,
                        AccessCount = kvp.Value.AccessCount,
                        LastAccessed = kvp.Value.LastAccessed,
                        LoadTime = TimeSpan.FromSeconds(1), // Mock
                        AverageTokensPerSecond = 100 // Mock
                    })
            };

            return Task.FromResult(stats);
        }

        public async Task<bool> DownloadModelAsync(string modelId, string source, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Downloading model {ModelId} from {Source}", modelId, source);

            // Simulate download progress using EXISTING DownloadProgress
            for (int i = 0; i <= 100; i += 10)
            {
                progress?.Report(new DownloadProgress
                {
                    ModelId = modelId,
                    TotalBytes = 5_000_000_000, // 5GB
                    DownloadedBytes = 5_000_000_000 * i / 100,
                    ProgressPercent = i,
                    SpeedMBps = 50.0f,
                    EstimatedTimeRemaining = TimeSpan.FromSeconds((100 - i) * 10)
                });
                await Task.Delay(500, cancellationToken);
            }

            return true;
        }

        public Task<bool> DeleteModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            var modelPath = Path.Combine(_modelsPath, modelId);
            if (Directory.Exists(modelPath))
            {
                Directory.Delete(modelPath, true);
                _logger.LogInformation("Deleted model {ModelId}", modelId);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<long> GetModelSizeAsync(string modelId, CancellationToken cancellationToken = default)
        {
            // Estimate based on model ID
            var size = modelId.ToLowerInvariant() switch
            {
                var m when m.Contains("tiny") => 100L * 1024 * 1024,
                var m when m.Contains("small") => 500L * 1024 * 1024,
                var m when m.Contains("base") => 1L * 1024 * 1024 * 1024,
                var m when m.Contains("large") => 5L * 1024 * 1024 * 1024,
                var m when m.Contains("7b") || m.Contains("8b") => 7L * 1024 * 1024 * 1024,
                var m when m.Contains("13b") => 13L * 1024 * 1024 * 1024,
                _ => 2L * 1024 * 1024 * 1024
            };

            return Task.FromResult(size);
        }

        // Private helper methods
        private long GetCurrentMemoryUsage()
        {
            return _loadedModels.Values.Sum(m => m.Handle.MemoryUsage);
        }

        private long EstimateModelMemory(ModelRequest request)
        {
            // Estimate based on model size and type
            var baseMemory = request.ModelSize.ToLowerInvariant() switch
            {
                "tiny" => 100L * 1024 * 1024,
                "small" => 500L * 1024 * 1024,
                "base" => 1L * 1024 * 1024 * 1024,
                "medium" => 2L * 1024 * 1024 * 1024,
                "large" => 5L * 1024 * 1024 * 1024,
                "xl" => 10L * 1024 * 1024 * 1024,
                _ => 2L * 1024 * 1024 * 1024
            };

            // Adjust for quantization
            var quantizationMultiplier = request.Quantization switch
            {
                "Q4_K_M" => 0.4f,
                "Q5_K_M" => 0.5f,
                "Q8_0" => 0.8f,
                "F16" => 1.0f,
                "F32" => 2.0f,
                _ => 0.5f
            };

            return (long)(baseMemory * quantizationMultiplier);
        }

        private string DetermineProvider(ModelType type)
        {
            return type switch
            {
                ModelType.LLM => "llama.cpp",
                ModelType.Whisper => "whisper.cpp",
                ModelType.CLIP => "onnxruntime",
                ModelType.Embedding => "sentence-transformers",
                _ => "custom"
            };
        }

        private int EstimateTokens(object input, ModelType type)
        {
            return type switch
            {
                ModelType.LLM => (input?.ToString()?.Length ?? 0) / 4,
                ModelType.Whisper => 1500, // ~30 seconds of audio
                ModelType.Embedding => (input?.ToString()?.Length ?? 0) / 4,
                _ => 100
            };
        }

        private bool CheckROCmSupport()
        {
            return Directory.Exists("/opt/rocm") ||
                   Environment.GetEnvironmentVariable("ROCM_PATH") != null;
        }

        private bool CheckDirectMLSupport()
        {
            if (OperatingSystem.IsWindows())
            {
                var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                return File.Exists(Path.Combine(systemPath, "DirectML.dll"));
            }
            return false;
        }

        // Model-specific inference methods - Returns EXISTING models from IIM.Core.Models
        private async Task<object> InferWhisperAsync(object input, LoadedModel model, CancellationToken ct)
        {
            await Task.Delay(500, ct); // Simulate processing

            // Returns EXISTING TranscriptionResult from IIM.Core.Models
            return new TranscriptionResult
            {
                Id = Guid.NewGuid().ToString("N"),
                Text = $"Transcription of: {input}",
                Language = "en",
                Confidence = 0.95,
                Segments = new List<TranscriptionSegment>
                {
                    new TranscriptionSegment
                    {
                        Start = 0,
                        End = 5,
                        Text = "Transcribed segment",
                        Confidence = 0.95
                    }
                }
            };
        }

        private async Task<object> InferCLIPAsync(object input, LoadedModel model, CancellationToken ct)
        {
            await Task.Delay(300, ct); // Simulate processing

            // Returns EXISTING ImageAnalysisResult from IIM.Core.Models
            return new ImageAnalysisResult
            {
                Id = Guid.NewGuid().ToString("N"),
                EvidenceId = "evidence-001",
                Embedding = new float[] { 0.1f, 0.2f, 0.3f },
                Tags = new List<string> { "person", "vehicle" },
                SimilarImages = new List<SimilarImage>
                {
                    new SimilarImage
                    {
                        EvidenceId = "evidence-002",
                        FileName = "similar.jpg",
                        Similarity = 0.89
                    }
                }
            };
        }

        private async Task<object> InferEmbeddingAsync(object input, LoadedModel model, CancellationToken ct)
        {
            await Task.Delay(100, ct); // Simulate processing
            return new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
        }

        private async Task<object> InferLLMAsync(object input, LoadedModel model, CancellationToken ct)
        {
            await Task.Delay(800, ct); // Simulate processing
            return $"Response to: {input}";
        }

        // Internal type - ONLY for internal tracking, not exposed
        private class LoadedModel
        {
            public required ModelHandle Handle { get; init; }
            public required ModelConfiguration Configuration { get; init; }
            public Process? Process { get; init; }
            public DateTimeOffset LastAccessed { get; set; }
            public int AccessCount { get; set; }
        }
    }
}