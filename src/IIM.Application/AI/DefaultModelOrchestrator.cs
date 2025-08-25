using IIM.Core.Configuration;
using IIM.Core.Models;
using IIM.Infrastructure.Storage;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.AI
{
    /// <summary>
    /// Central orchestrator for loading, managing, and unloading AI models (ONNX, GGUF, etc).
    /// Handles resource management and ONNX DirectML sessions.
    /// </summary>
    public class DefaultModelOrchestrator : IModelOrchestrator
    {
        private readonly ILogger<DefaultModelOrchestrator> _logger;
        private readonly IStorageConfiguration _storageConfig;
        private readonly ConcurrentDictionary<string, LoadedModel> _loadedModels = new();
        private readonly ConcurrentDictionary<string, InferenceSession> _onnxSessions = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private bool _disposed;

        private const long MaxMemoryBytes = 120L * 1024 * 1024 * 1024; // 120GB
        private const long MinMemoryBuffer = 2L * 1024 * 1024 * 1024;  // 2GB

        public event EventHandler<ModelLoadedEventArgs>? ModelLoaded;
        public event EventHandler<ModelUnloadedEventArgs>? ModelUnloaded;
        public event EventHandler<ModelErrorEventArgs>? ModelError;
        public event EventHandler<ResourceThresholdEventArgs>? ResourceThresholdExceeded;

        /// <summary>
        /// Constructor. Injects logger and storage config, ensures directories exist.
        /// </summary>
        public DefaultModelOrchestrator(ILogger<DefaultModelOrchestrator> logger, IStorageConfiguration storageConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageConfig = storageConfig ?? throw new ArgumentNullException(nameof(storageConfig));
            _storageConfig.EnsureDirectoriesExist();
        }

        /// <summary>
        /// Loads a model into memory. Creates an ONNX InferenceSession for .onnx models (with DirectML GPU if possible).
        /// Thread-safe with locking.
        /// </summary>
        public async Task<ModelHandle> LoadModelAsync(ModelLoadRequest request, IProgress<float>? progress = null, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.ModelId)) throw new ArgumentException("ModelId cannot be null or empty", nameof(request));

            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                // Return handle if already loaded
                if (_loadedModels.ContainsKey(request.ModelId))
                {
                    _logger.LogInformation("Model {ModelId} already loaded.", request.ModelId);
                    return _loadedModels[request.ModelId].Handle;
                }

                // Resolve and check model file
                var modelPath = ResolveModelPath(request);
                if (!File.Exists(modelPath))
                    throw new FileNotFoundException("Model file not found", modelPath);

                // Estimate RAM required, check available RAM
                var estimatedMemory = await EstimateModelMemoryAsync(request, modelPath);
                var currentMemory = GetCurrentMemoryUsage();
                if (currentMemory + estimatedMemory + MinMemoryBuffer > MaxMemoryBytes)
                    throw new Models.InsufficientMemoryException(estimatedMemory, MaxMemoryBytes - currentMemory);

                // --- ONNX Session creation (for .onnx models) ---
                if (modelPath.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_onnxSessions.ContainsKey(request.ModelId))
                    {
                        var options = new SessionOptions();
                        try
                        {
                            // Prefer DirectML GPU if present, fallback to CPU if needed
                            options.AppendExecutionProvider_DML(0);
                            _logger.LogInformation("DirectML provider added for model {ModelId}", request.ModelId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to add DirectML, falling back to CPU for model {ModelId}", request.ModelId);
                            options.AppendExecutionProvider_CPU();
                        }

                        // Optional: Optimize for performance
                        options.ExecutionMode = ExecutionMode.ORT_PARALLEL;
                        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

                        var session = new InferenceSession(modelPath, options);
                        _onnxSessions[request.ModelId] = session;
                    }
                }

                // Build model handle and register
                var handle = new ModelHandle
                {
                    ModelId = request.ModelId,
                    SessionId = Guid.NewGuid().ToString(),
                    LoadedAt = DateTimeOffset.UtcNow,
                    MemoryUsage = estimatedMemory,
                    State = ModelState.Ready,
                    Metadata = new Dictionary<string, object>()
                };
                var loadedModel = new LoadedModel
                {
                    Handle = handle,
                    Request = request,
                    ModelPath = modelPath,
                    RuntimeOptions = new ModelRuntimeOptions(),
                    Process = Process.GetCurrentProcess(),
                    LastAccessed = DateTimeOffset.UtcNow
                };
                _loadedModels[request.ModelId] = loadedModel;

                ModelLoaded?.Invoke(this, new ModelLoadedEventArgs
                {
                    ModelId = request.ModelId,
                    Type = request.ModelType,
                    MemoryUsage = estimatedMemory,
                    LoadTime = TimeSpan.Zero
                });

                return handle;
            }
            catch (Exception ex)
            {
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
        /// Unloads the specified model from memory. Cleans up any ONNX InferenceSession.
        /// </summary>
        public async Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                if (_loadedModels.TryRemove(modelId, out var loadedModel))
                {
                    // Dispose ONNX session if present
                    if (_onnxSessions.TryRemove(modelId, out var session))
                        session.Dispose();

                    ModelUnloaded?.Invoke(this, new ModelUnloadedEventArgs
                    {
                        ModelId = modelId,
                        MemoryFreed = loadedModel.Handle.MemoryUsage
                    });
                }
            }
            finally
            {
                _loadLock.Release();
            }
        }

        /// <summary>
        /// Returns true if the model is currently loaded in memory.
        /// </summary>
        public Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default)
            => Task.FromResult(_loadedModels.ContainsKey(modelId));

        /// <summary>
        /// Returns metadata about all currently loaded models.
        /// </summary>
        public Task<List<ModelConfiguration>> GetLoadedModelsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_loadedModels.Values.Select(m => new ModelConfiguration
            {
                ModelId = m.Request.ModelId,
                Name = Path.GetFileName(m.ModelPath),
                ModelPath = m.ModelPath
            }).ToList());

        /// <summary>
        /// Scans the models folder and returns metadata for all available (not necessarily loaded) models.
        /// </summary>
        public Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            var available = Directory.EnumerateFiles(_storageConfig.ModelsPath, "*.*", SearchOption.AllDirectories)
                .Where(p =>
                    p.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".ggml", StringComparison.OrdinalIgnoreCase))
                .Select(p => new ModelConfiguration
                {
                    ModelId = Path.GetFileNameWithoutExtension(p),
                    Name = Path.GetFileName(p),
                    ModelPath = p
                }).ToList();
            return Task.FromResult(available);
        }

        /// <summary>
        /// Gets metadata for a loaded or available model by ID (null if not found).
        /// </summary>
        public async Task<ModelConfiguration?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default)
        {
            // Check loaded models first
            if (_loadedModels.TryGetValue(modelId, out var loadedModel))
            {
                return new ModelConfiguration
                {
                    ModelId = loadedModel.Request.ModelId,
                    Name = Path.GetFileName(loadedModel.ModelPath),
                    ModelPath = loadedModel.ModelPath,
                };
            }
            // Fallback: Check models on disk
            var available = await GetAvailableModelsAsync(cancellationToken);
            return available.FirstOrDefault(m => m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns the active ONNX InferenceSession for a loaded ONNX model.
        /// Throws if the session is not found (model not loaded).
        /// </summary>
        public InferenceSession GetOnnxSession(string modelId)
        {
            if (!_onnxSessions.TryGetValue(modelId, out var session))
                throw new InvalidOperationException("ONNX model session not loaded!");
            return session;
        }

        public long GetTotalMemoryUsageAsync() {

            long result = GetCurrentMemoryUsage();

            return result;
        }


        /// <summary>
        /// Builds the absolute path to the model file.
        /// </summary>
        private string ResolveModelPath(ModelLoadRequest request)
            => Path.IsPathRooted(request.ModelPath)
                ? request.ModelPath
                : Path.Combine(_storageConfig.ModelsPath, request.ModelPath);

        /// <summary>
        /// Estimates RAM needed for a model (currently: 2x file size as a placeholder).
        /// </summary>
        private Task<long> EstimateModelMemoryAsync(ModelLoadRequest request, string modelPath)
            => Task.FromResult(new FileInfo(modelPath).Length * 2);

        /// <summary>
        /// Sums the memory usage of all loaded models (for resource checks).
        /// </summary>
        private long GetCurrentMemoryUsage()
            => _loadedModels.Values.Sum(m => m.Handle.MemoryUsage);

        /// <summary>
        /// Disposes all loaded models and ONNX sessions, cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            foreach (var modelId in _loadedModels.Keys.ToList())
                UnloadModelAsync(modelId).Wait();

            foreach (var session in _onnxSessions.Values)
                session.Dispose();

            _loadLock.Dispose();
            _disposed = true;
        }
    }
}
