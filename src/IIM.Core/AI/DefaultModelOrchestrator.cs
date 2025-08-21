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
using IIM.Shared.Models;
using IIM.Infrastructure.Storage;
using Microsoft.Extensions.Logging;


namespace IIM.Core.AI
{
    /// <summary>
    /// Default implementation of IModelOrchestrator for managing AI model lifecycle.
    /// Handles model loading/unloading, memory management, and inference coordination.
    /// This is the resource management layer responsible for HOW models are loaded and run.
    /// </summary>
    public class DefaultModelOrchestrator : IModelOrchestrator
    {
        private readonly ILogger<DefaultModelOrchestrator> _logger;
        private readonly StorageConfiguration _storageConfig;
        private readonly ConcurrentDictionary<string, LoadedModel> _loadedModels = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);

        // Memory management constants
        private const long MaxMemoryBytes = 120L * 1024 * 1024 * 1024; // 120GB for Framework laptop
        private const long MemoryWarningThreshold = 100L * 1024 * 1024 * 1024; // Warn at 100GB

        // Events
        public event EventHandler<ModelLoadedEventArgs>? ModelLoaded;
        public event EventHandler<ModelUnloadedEventArgs>? ModelUnloaded;
        public event EventHandler<ModelErrorEventArgs>? ModelError;
        public event EventHandler<ResourceThresholdEventArgs>? ResourceThresholdExceeded;

        /// <summary>
        /// Initializes a new instance of the DefaultModelOrchestrator with StorageConfiguration.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output</param>
        /// <param name="storageConfig">Centralized storage configuration for paths</param>
        public DefaultModelOrchestrator(
            ILogger<DefaultModelOrchestrator> logger,
            StorageConfiguration storageConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageConfig = storageConfig ?? throw new ArgumentNullException(nameof(storageConfig));

            // Ensure all necessary directories exist
            _storageConfig.EnsureDirectoriesExist();

            _logger.LogInformation(
                "DefaultModelOrchestrator initialized with models path: {ModelsPath}",
                _storageConfig.ModelsPath);
        }

        /// <summary>
        /// Loads a model into memory with resource management.
        /// Handles memory checks, model validation, and proper error handling.
        /// </summary>
        /// <param name="request">Model loading request with configuration</param>
        /// <param name="progress">Optional progress reporter for loading status</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Handle to the loaded model for future operations</returns>
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

                // Resolve model path using StorageConfiguration
                var modelPath = ResolveModelPath(request);
                if (!ValidateModelPath(modelPath))
                {
                    throw new FileNotFoundException(
                        $"Model file not found at: {modelPath}. Model ID: {request.ModelId}");
                }

                // Estimate memory requirement
                var estimatedMemory = EstimateModelMemory(request);
                var currentMemory = GetCurrentMemoryUsage();

                // Check memory availability
                if (currentMemory + estimatedMemory > MaxMemoryBytes)
                {
                    _logger.LogWarning(
                        "Insufficient memory for model {ModelId}. Required: {Required}GB, Available: {Available}GB",
                        request.ModelId,
                        estimatedMemory / (1024 * 1024 * 1024),
                        (MaxMemoryBytes - currentMemory) / (1024 * 1024 * 1024));

                    // Try to free memory by unloading LRU models
                    await OptimizeMemoryAsync(cancellationToken);

                    // Check again after optimization
                    currentMemory = GetCurrentMemoryUsage();
                    if (currentMemory + estimatedMemory > MaxMemoryBytes)
                    {
                        throw new System.InsufficientMemoryException(
                            $"Cannot load model {request.ModelId}. Insufficient memory after optimization. " +
                            $"Required: {estimatedMemory / (1024 * 1024 * 1024)}GB, " +
                            $"Available: {(MaxMemoryBytes - currentMemory) / (1024 * 1024 * 1024)}GB");
                    }
                }

                // Load the model with progress reporting
                var stopwatch = Stopwatch.StartNew();
                var handle = await LoadModelInternalAsync(request, modelPath, estimatedMemory, progress, cancellationToken);
                stopwatch.Stop();

                // Create loaded model entry
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
                        LoadedPath = modelPath,
                        LoadedAt = DateTimeOffset.UtcNow,
                        Parameters = request.Options ?? new Dictionary<string, object>()
                    },
                    Process = null, // Would be actual process in production
                    LastAccessed = DateTimeOffset.UtcNow
                };

                _loadedModels[request.ModelId] = loadedModel;

                // Raise model loaded event
                ModelLoaded?.Invoke(this, new ModelLoadedEventArgs
                {
                    ModelId = request.ModelId,
                    Type = request.ModelType,
                    MemoryUsage = estimatedMemory,
                    LoadTime = stopwatch.Elapsed
                });

                // Check memory warning threshold
                CheckMemoryThreshold(currentMemory + estimatedMemory);

                _logger.LogInformation(
                    "Model {ModelId} loaded successfully in {Time}ms from {Path}",
                    request.ModelId, stopwatch.ElapsedMilliseconds, modelPath);

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
        /// Resolves the actual file path for a model using StorageConfiguration.
        /// Checks multiple locations in priority order.
        /// </summary>
        /// <param name="request">Model request containing model ID and optional path</param>
        /// <returns>Resolved path to the model file</returns>
        private string ResolveModelPath(ModelRequest request)
        {
            // If explicit path provided and exists, use it
            if (!string.IsNullOrEmpty(request.ModelPath))
            {
                if (File.Exists(request.ModelPath) || Directory.Exists(request.ModelPath))
                {
                    return request.ModelPath;
                }
            }

            // Try to find model using StorageConfiguration's intelligent path resolution
            // This checks FineTuned -> User -> Cache -> System directories
            var modelPath = _storageConfig.GetModelPath(request.ModelId, ModelSource.Auto);

            // If the path doesn't exist yet, determine where it should be placed
            if (!File.Exists(modelPath) && !Directory.Exists(modelPath))
            {
                // Determine the appropriate location based on model type
                var source = DetermineModelSource(request);
                modelPath = _storageConfig.GetModelPath(request.ModelId, source);
            }

            _logger.LogDebug("Resolved model path for {ModelId}: {Path}", request.ModelId, modelPath);
            return modelPath;
        }

        /// <summary>
        /// Determines the appropriate storage location for a model.
        /// </summary>
        /// <param name="request">Model request to analyze</param>
        /// <returns>Appropriate ModelSource enum value</returns>
        private ModelSource DetermineModelSource(ModelRequest request)
        {
            // Fine-tuned models
            if (request.ModelId.Contains("finetuned", StringComparison.OrdinalIgnoreCase) ||
                request.ModelId.Contains("ft-", StringComparison.OrdinalIgnoreCase))
            {
                return ModelSource.FineTuned;
            }

            // User-provided models
            if (request.Options?.ContainsKey("user_provided") == true)
            {
                return ModelSource.User;
            }

            // System models (whisper, clip, etc.)
            if (IsSystemModel(request.ModelId))
            {
                return ModelSource.System;
            }

            // Default to cache for downloaded models
            return ModelSource.Cache;
        }

        /// <summary>
        /// Checks if a model is a system-provided model.
        /// </summary>
        /// <param name="modelId">Model identifier to check</param>
        /// <returns>True if system model, false otherwise</returns>
        private bool IsSystemModel(string modelId)
        {
            var systemModels = new[]
            {
                "whisper", "clip", "bge", "all-minilm", "sentence-transformers",
                "yolo", "sam", "groundingdino"
            };

            var lowerModelId = modelId.ToLowerInvariant();
            return systemModels.Any(sm => lowerModelId.Contains(sm));
        }

        /// <summary>
        /// Validates that a model path exists and is accessible.
        /// </summary>
        /// <param name="path">Path to validate</param>
        /// <returns>True if path is valid, false otherwise</returns>
        private bool ValidateModelPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Check if it's a file
            if (File.Exists(path))
                return true;

            // Check if it's a directory (some models are folders)
            if (Directory.Exists(path))
                return true;

            // Check for common model file extensions
            var extensions = new[] { ".gguf", ".bin", ".onnx", ".pt", ".safetensors", ".pkl" };
            foreach (var ext in extensions)
            {
                if (File.Exists(path + ext))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Internal method to actually load the model.
        /// Simulates loading with progress reporting.
        /// </summary>
        private async Task<ModelHandle> LoadModelInternalAsync(
            ModelRequest request,
            string modelPath,
            long estimatedMemory,
            IProgress<float>? progress,
            CancellationToken cancellationToken)
        {
            // Simulate model loading with progress
            for (int i = 0; i <= 100; i += 10)
            {
                progress?.Report(i / 100f);
                await Task.Delay(100, cancellationToken);
            }

            // Create and return model handle
            return new ModelHandle
            {
                ModelId = request.ModelId,
                SessionId = Guid.NewGuid().ToString("N"),
                Provider = request.Provider ?? DetermineProvider(request.ModelType),
                Type = request.ModelType,
                MemoryUsage = estimatedMemory,
                LoadedAt = DateTimeOffset.UtcNow
            };
        }

        /// <summary>
        /// Checks if memory usage exceeds warning threshold and raises event if needed.
        /// </summary>
        /// <param name="totalMemory">Total memory usage in bytes</param>
        private void CheckMemoryThreshold(long totalMemory)
        {
            if (totalMemory > MemoryWarningThreshold)
            {
                ResourceThresholdExceeded?.Invoke(this, new ResourceThresholdEventArgs
                {
                    ResourceType = "Memory",
                    CurrentUsage = totalMemory / (float)MaxMemoryBytes * 100,
                    Threshold = MemoryWarningThreshold / (float)MaxMemoryBytes * 100,
                    Recommendation = "Consider unloading unused models to free memory"
                });
            }
        }

        #region Original Methods (Unchanged)

        /// <summary>
        /// Performs inference using a loaded model.
        /// Routes to appropriate inference method based on model type.
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
        /// Unloads a model from memory and cleans up resources.
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
            var available = new List<ModelConfiguration>();

            // Scan actual model directories using StorageConfiguration
            var modelDirs = new[]
            {
                (_storageConfig.SystemModelsPath, ModelSource.System),
                (_storageConfig.UserModelsPath, ModelSource.User),
                (_storageConfig.ModelCachePath, ModelSource.Cache),
                (_storageConfig.FineTunedModelsPath, ModelSource.FineTuned)
            };

            foreach (var (dirPath, source) in modelDirs)
            {
                if (Directory.Exists(dirPath))
                {
                    try
                    {
                        var models = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => IsModelFile(f))
                            .Select(f => CreateModelConfiguration(f, source))
                            .Where(c => c != null);

                        available.AddRange(models!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error scanning directory {Path}", dirPath);
                    }
                }
            }

            // Add currently loaded models
            available.AddRange(_loadedModels.Values.Select(m => m.Configuration));

            return Task.FromResult(available.DistinctBy(m => m.ModelId).ToList());
        }

        /// <summary>
        /// Checks if a file is a model file based on extension.
        /// </summary>
        private bool IsModelFile(string filePath)
        {
            var modelExtensions = new[] { ".gguf", ".bin", ".onnx", ".pt", ".safetensors", ".pkl", ".h5" };
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return modelExtensions.Contains(extension);
        }

        /// <summary>
        /// Creates a ModelConfiguration from a file path.
        /// </summary>
        private ModelConfiguration? CreateModelConfiguration(string filePath, ModelSource source)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileInfo = new FileInfo(filePath);

                return new ModelConfiguration
                {
                    ModelId = fileName,
                    Type = DetermineModelTypeFromName(fileName),
                    Status = ModelStatus.Available,
                    Provider = DetermineProviderFromFile(filePath),
                    MemoryUsage = fileInfo.Length,
                    LoadedPath = filePath,
                    Parameters = new Dictionary<string, object>
                    {
                        ["source"] = source.ToString(),
                        ["file_size"] = fileInfo.Length
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error creating configuration for {Path}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Determines model type from the model name.
        /// </summary>
        private ModelType DetermineModelTypeFromName(string modelName)
        {
            var lower = modelName.ToLowerInvariant();
            return lower switch
            {
                var n when n.Contains("whisper") => ModelType.Whisper,
                var n when n.Contains("clip") => ModelType.CLIP,
                var n when n.Contains("embed") || n.Contains("bge") || n.Contains("minilm") => ModelType.Embedding,
                var n when n.Contains("llama") || n.Contains("mistral") || n.Contains("gpt") => ModelType.LLM,
                _ => ModelType.Custom
            };
        }

        /// <summary>
        /// Determines the provider from file extension.
        /// </summary>
        private string DetermineProviderFromFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".gguf" => "llama.cpp",
                ".onnx" => "onnxruntime",
                ".pt" or ".pth" => "pytorch",
                ".safetensors" => "transformers",
                _ => "custom"
            };
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

            // Determine where to save the model
            var targetPath = _storageConfig.GetModelPath(modelId, ModelSource.Cache);
            var targetDir = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Simulate download progress
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

            _logger.LogInformation("Model {ModelId} downloaded to {Path}", modelId, targetPath);
            return true;
        }

        public Task<bool> DeleteModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            // Try to find the model in all locations
            var modelPath = _storageConfig.GetModelPath(modelId, ModelSource.Auto);

            if (File.Exists(modelPath))
            {
                File.Delete(modelPath);
                _logger.LogInformation("Deleted model file {ModelId} at {Path}", modelId, modelPath);
                return Task.FromResult(true);
            }

            if (Directory.Exists(modelPath))
            {
                Directory.Delete(modelPath, true);
                _logger.LogInformation("Deleted model directory {ModelId} at {Path}", modelId, modelPath);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Model {ModelId} not found for deletion", modelId);
            return Task.FromResult(false);
        }

        public Task<long> GetModelSizeAsync(string modelId, CancellationToken cancellationToken = default)
        {
            // First check if it's a loaded model
            if (_loadedModels.TryGetValue(modelId, out var loadedModel))
            {
                return Task.FromResult(loadedModel.Handle.MemoryUsage);
            }

            // Try to find the model file
            var modelPath = _storageConfig.GetModelPath(modelId, ModelSource.Auto);

            if (File.Exists(modelPath))
            {
                var fileInfo = new FileInfo(modelPath);
                return Task.FromResult(fileInfo.Length);
            }

            // Estimate based on model ID patterns
            var lowerModelId = modelId.ToLowerInvariant();
            var size = lowerModelId switch
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
            // First try to get actual file size if path exists
            if (!string.IsNullOrEmpty(request.ModelPath) && File.Exists(request.ModelPath))
            {
                var fileInfo = new FileInfo(request.ModelPath);
                // Add 20% overhead for runtime memory
                return (long)(fileInfo.Length * 1.2);
            }

            // Otherwise estimate based on model size and type
            var baseMemory = request.ModelSize?.ToLowerInvariant() switch
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

        // Model-specific inference methods
        private async Task<object> InferWhisperAsync(object input, LoadedModel model, CancellationToken ct)
        {
            await Task.Delay(500, ct); // Simulate processing

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

        #endregion
    }
}