using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Inference;

/// <summary>
/// Interface for managing AI model lifecycle and memory
/// </summary>
public interface IModelOrchestrator
{
    Task<ModelHandle> LoadModelAsync(ModelRequest request, CancellationToken ct = default);
    Task<InferenceResult> InferAsync(string modelId, object input, CancellationToken ct = default);
    Task UnloadModelAsync(string modelId);
    Task<ModelStats> GetStatsAsync();
}

/// <summary>
/// Orchestrates loading, unloading, and memory management for AI models
/// Ensures system doesn't exceed memory limits and handles model lifecycle
/// </summary>
public sealed class ModelOrchestrator : IModelOrchestrator, IDisposable
{
    private readonly ILogger<ModelOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, LoadedModel> _models = new();
    private readonly SemaphoreSlim _memoryLock = new(1, 1);
    private readonly Timer _memoryMonitor;
    private int _currentPort = 9000; // Starting port for model services

    // Memory configuration constants
    private const long MaxMemoryBytes = 120L * 1024 * 1024 * 1024; // 120GB limit
    private const long EmergencyThresholdBytes = 110L * 1024 * 1024 * 1024; // 110GB emergency
    private const int MaxConcurrentInference = 2; // Max concurrent GPU operations

    /// <summary>
    /// Initializes the model orchestrator with memory monitoring
    /// </summary>
    /// <param name="logger">Logger for diagnostic output</param>
    public ModelOrchestrator(ILogger<ModelOrchestrator> logger)
    {
        _logger = logger;
        _memoryMonitor = new Timer(MonitorMemoryPressure, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Loads a model into memory, managing memory pressure and eviction if needed
    /// </summary>
    /// <param name="request">Model loading request with configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Handle to the loaded model</returns>
    public async Task<ModelHandle> LoadModelAsync(ModelRequest request, CancellationToken ct = default)
    {
        await _memoryLock.WaitAsync(ct);
        try
        {
            // Check if model already loaded
            if (_models.TryGetValue(request.ModelId, out var existing))
            {
                existing.LastAccessed = DateTimeOffset.UtcNow;
                existing.AccessCount++;
                _logger.LogInformation("Model {ModelId} already loaded", request.ModelId);
                return new ModelHandle(request.ModelId, existing.SessionId);
            }

            // Calculate required memory
            var requiredMemory = EstimateModelMemory(request);
            var currentUsage = GetCurrentMemoryUsage();

            _logger.LogInformation("Loading model {ModelId}: requires {Required:N0} MB",
                request.ModelId, requiredMemory / 1_048_576);

            // Evict models if necessary to make room
            while (currentUsage + requiredMemory > MaxMemoryBytes)
            {
                if (!await EvictLeastRecentlyUsedAsync())
                {
                    throw new InsufficientMemoryException(
                        $"Cannot load {request.ModelId}: requires {requiredMemory / 1_048_576:N0} MB");
                }
                currentUsage = GetCurrentMemoryUsage();
            }

            // Load the model
            var model = await LoadModelInternalAsync(request);
            _models[request.ModelId] = model;

            _logger.LogInformation("Successfully loaded {ModelId}", request.ModelId);
            return new ModelHandle(request.ModelId, model.SessionId);
        }
        finally
        {
            _memoryLock.Release();
        }
    }

    /// <summary>
    /// Performs inference using a loaded model
    /// </summary>
    /// <param name="modelId">ID of the model to use</param>
    /// <param name="input">Input data for inference</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Inference results</returns>
    public async Task<InferenceResult> InferAsync(string modelId, object input, CancellationToken ct = default)
    {
        if (!_models.TryGetValue(modelId, out var model))
        {
            throw new ModelNotLoadedException($"Model {modelId} is not loaded");
        }

        // Update access tracking
        model.LastAccessed = DateTimeOffset.UtcNow;
        model.AccessCount++;

        // Simulate inference (in production, this would call the actual model)
        await Task.Delay(100, ct);

        return new InferenceResult
        {
            ModelId = modelId,
            Output = $"Inference result for: {input}",
            InferenceTime = TimeSpan.FromMilliseconds(100),
            TokensProcessed = 100,
            TokensPerSecond = 10
        };
    }

    /// <summary>
    /// Unloads a model from memory and cleans up resources
    /// </summary>
    /// <param name="modelId">ID of the model to unload</param>
    public async Task UnloadModelAsync(string modelId)
    {
        if (!_models.TryGetValue(modelId, out var model))
        {
            _logger.LogWarning("Model {ModelId} not found for unloading", modelId);
            return;
        }

        await _memoryLock.WaitAsync();
        try
        {
            _logger.LogInformation("Unloading model {ModelId}", modelId);
            await UnloadModelInternalAsync(model);
            _models.TryRemove(modelId, out _);

            // Force garbage collection to reclaim memory
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }
        finally
        {
            _memoryLock.Release();
        }
    }

    /// <summary>
    /// Gets current statistics about loaded models and memory usage
    /// </summary>
    /// <returns>Model statistics including memory usage and loaded models</returns>
    public async Task<ModelStats> GetStatsAsync()
    {
        await Task.CompletedTask; // Make async for interface compliance

        var stats = new ModelStats
        {
            LoadedModels = _models.Count,
            TotalMemoryUsage = _models.Values.Sum(m => m.MemoryUsage),
            AvailableMemory = MaxMemoryBytes - _models.Values.Sum(m => m.MemoryUsage),
            Models = new Dictionary<string, ModelInfo>()
        };

        foreach (var kvp in _models)
        {
            stats.Models[kvp.Key] = new ModelInfo
            {
                ModelId = kvp.Key,
                Type = kvp.Value.ModelType,
                MemoryUsage = kvp.Value.MemoryUsage,
                AccessCount = kvp.Value.AccessCount,
                LastAccessed = kvp.Value.LastAccessed,
                LoadTime = kvp.Value.LoadTime
            };
        }

        return stats;
    }

    /// <summary>
    /// Evicts the least recently used model to free memory
    /// </summary>
    /// <returns>True if a model was evicted, false if no models can be evicted</returns>
    private async Task<bool> EvictLeastRecentlyUsedAsync()
    {
        var lru = _models.Values
            .Where(m => !m.IsPinned)
            .OrderBy(m => m.LastAccessed)
            .ThenBy(m => m.AccessCount)
            .FirstOrDefault();

        if (lru == null)
        {
            _logger.LogWarning("No models available for eviction");
            return false;
        }

        _logger.LogInformation("Evicting model {ModelId}", lru.ModelId);
        await UnloadModelInternalAsync(lru);
        _models.TryRemove(lru.ModelId, out _);

        return true;
    }

    /// <summary>
    /// Internal method to load a model based on its type
    /// </summary>
    private async Task<LoadedModel> LoadModelInternalAsync(ModelRequest request)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var backend = SelectOptimalBackend(request);

        var model = request.ModelType switch
        {
            ModelType.LLM => await LoadLLMAsync(request, backend, sessionId),
            ModelType.Whisper => await LoadWhisperAsync(request, backend, sessionId),
            ModelType.CLIP => await LoadCLIPAsync(request, backend, sessionId),
            ModelType.Embedding => await LoadEmbeddingAsync(request, backend, sessionId),
            _ => throw new NotSupportedException($"Model type {request.ModelType} not supported")
        };

        return model;
    }

    /// <summary>
    /// Loads a Large Language Model
    /// </summary>
    private async Task<LoadedModel> LoadLLMAsync(ModelRequest request, InferenceBackend backend, string sessionId)
    {
        var port = GetNextPort();

        // In production, this would start the actual model server
        // For now, we simulate it
        await Task.Delay(1000);

        return new LoadedModel
        {
            ModelId = request.ModelId,
            SessionId = sessionId,
            Process = null, // Would be actual process in production
            Port = port,
            Backend = backend,
            MemoryUsage = EstimateModelMemory(request),
            ModelType = ModelType.LLM,
            Quantization = DetermineQuantization(request)
        };
    }

    /// <summary>
    /// Loads a Whisper speech recognition model
    /// </summary>
    private async Task<LoadedModel> LoadWhisperAsync(ModelRequest request, InferenceBackend backend, string sessionId)
    {
        await Task.Delay(500); // Simulate loading

        return new LoadedModel
        {
            ModelId = request.ModelId,
            SessionId = sessionId,
            Process = null,
            Port = GetNextPort(),
            Backend = backend,
            MemoryUsage = EstimateModelMemory(request),
            ModelType = ModelType.Whisper
        };
    }

    /// <summary>
    /// Loads a CLIP vision-language model
    /// </summary>
    private async Task<LoadedModel> LoadCLIPAsync(ModelRequest request, InferenceBackend backend, string sessionId)
    {
        await Task.CompletedTask; // Async for interface compliance

        return new LoadedModel
        {
            ModelId = request.ModelId,
            SessionId = sessionId,
            Process = null,
            Port = GetNextPort(),
            Backend = backend,
            MemoryUsage = EstimateModelMemory(request),
            ModelType = ModelType.CLIP
        };
    }

    /// <summary>
    /// Loads a text embedding model
    /// </summary>
    private async Task<LoadedModel> LoadEmbeddingAsync(ModelRequest request, InferenceBackend backend, string sessionId)
    {
        await Task.CompletedTask; // Async for interface compliance

        return new LoadedModel
        {
            ModelId = request.ModelId,
            SessionId = sessionId,
            Process = null,
            Port = GetNextPort(),
            Backend = backend,
            MemoryUsage = EstimateModelMemory(request),
            ModelType = ModelType.Embedding
        };
    }

    /// <summary>
    /// Unloads a model and cleans up its resources
    /// </summary>
    private async Task UnloadModelInternalAsync(LoadedModel model)
    {
        try
        {
            if (model.Process != null && !model.Process.HasExited)
            {
                model.Process.Kill();
                await model.Process.WaitForExitAsync();
                model.Process.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading model {ModelId}", model.ModelId);
        }
    }

    /// <summary>
    /// Estimates memory required for a model based on its parameters
    /// </summary>
    private long EstimateModelMemory(ModelRequest request)
    {
        return request.ModelType switch
        {
            ModelType.LLM => EstimateLLMMemory(request),
            ModelType.Whisper => request.ModelSize switch
            {
                "tiny" => 100L * 1024 * 1024,
                "base" => 200L * 1024 * 1024,
                "small" => 500L * 1024 * 1024,
                "medium" => 1500L * 1024 * 1024,
                "large" => 3000L * 1024 * 1024,
                _ => 1000L * 1024 * 1024
            },
            ModelType.CLIP => 2000L * 1024 * 1024,
            ModelType.Embedding => 1000L * 1024 * 1024,
            _ => 1000L * 1024 * 1024
        };
    }

    /// <summary>
    /// Estimates memory for Large Language Models based on parameter count
    /// </summary>
    private long EstimateLLMMemory(ModelRequest request)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            request.ModelPath, @"(\d+)b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success && int.TryParse(match.Groups[1].Value, out var billions))
        {
            long baseMemory = billions * 2L * 1_000_000_000L;

            baseMemory = request.Quantization switch
            {
                "Q4_K_M" => baseMemory / 4,
                "Q5_K_M" => baseMemory * 5 / 16,
                "Q6_K" => baseMemory * 6 / 16,
                "Q8_0" => baseMemory / 2,
                _ => baseMemory
            };

            return baseMemory;
        }

        return 8L * 1024 * 1024 * 1024; // Default 8GB
    }

    /// <summary>
    /// Gets current system memory usage
    /// </summary>
    private long GetCurrentMemoryUsage()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows memory check would go here
            return _models.Values.Sum(m => m.MemoryUsage);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                var meminfo = File.ReadAllLines("/proc/meminfo");
                var total = ParseMemInfo(meminfo, "MemTotal");
                var available = ParseMemInfo(meminfo, "MemAvailable");
                return total - available;
            }
            catch
            {
                return _models.Values.Sum(m => m.MemoryUsage);
            }
        }

        return Process.GetCurrentProcess().WorkingSet64;
    }

    /// <summary>
    /// Parses memory information from /proc/meminfo on Linux
    /// </summary>
    private long ParseMemInfo(string[] lines, string key)
    {
        var line = lines.FirstOrDefault(l => l.StartsWith(key));
        if (line != null)
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, @"\d+");
            if (match.Success && long.TryParse(match.Value, out var kb))
            {
                return kb * 1024; // Convert KB to bytes
            }
        }
        return 0;
    }

    /// <summary>
    /// Monitors memory pressure and triggers emergency eviction if needed
    /// </summary>
    private void MonitorMemoryPressure(object? state)
    {
        var usage = GetCurrentMemoryUsage();

        if (usage > EmergencyThresholdBytes)
        {
            _logger.LogWarning("Emergency memory pressure detected");

            Task.Run(async () =>
            {
                await _memoryLock.WaitAsync();
                try
                {
                    var toEvict = _models.Values
                        .Where(m => !m.IsPinned)
                        .OrderBy(m => m.AccessCount)
                        .Take(2)
                        .ToList();

                    foreach (var model in toEvict)
                    {
                        await UnloadModelInternalAsync(model);
                        _models.TryRemove(model.ModelId, out _);
                    }
                }
                finally
                {
                    _memoryLock.Release();
                }
            });
        }
    }

    /// <summary>
    /// Selects the optimal inference backend based on available hardware
    /// </summary>
    private InferenceBackend SelectOptimalBackend(ModelRequest request)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return InferenceBackend.DirectML;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (Directory.Exists("/opt/rocm"))
                return InferenceBackend.ROCm;
            if (File.Exists("/usr/local/cuda/lib64/libcudart.so"))
                return InferenceBackend.CUDA;
        }

        return InferenceBackend.CPU;
    }

    /// <summary>
    /// Determines optimal quantization based on available memory
    /// </summary>
    private string DetermineQuantization(ModelRequest request)
    {
        if (!string.IsNullOrEmpty(request.Quantization))
            return request.Quantization;

        var availableMemory = MaxMemoryBytes - GetCurrentMemoryUsage();

        if (availableMemory > 32L * 1024 * 1024 * 1024)
            return "Q6_K";
        if (availableMemory > 16L * 1024 * 1024 * 1024)
            return "Q5_K_M";
        if (availableMemory > 8L * 1024 * 1024 * 1024)
            return "Q4_K_M";

        return "Q4_0";
    }

    /// <summary>
    /// Gets the next available port for a model service
    /// </summary>
    private int GetNextPort()
    {
        return Interlocked.Increment(ref _currentPort);
    }

    /// <summary>
    /// Disposes resources and stops monitoring
    /// </summary>
    public void Dispose()
    {
        _memoryMonitor?.Dispose();

        foreach (var model in _models.Values)
        {
            UnloadModelInternalAsync(model).GetAwaiter().GetResult();
        }

        _models.Clear();
    }
}

// Supporting types
public sealed class ModelRequest
{
    public required string ModelId { get; init; }
    public required string ModelPath { get; init; }
    public required ModelType ModelType { get; init; }
    public string ModelSize { get; init; } = "medium";
    public string Quantization { get; init; } = "Q4_K_M";
    public int ContextSize { get; init; } = 4096;
    public int BatchSize { get; init; } = 512;
    public int GpuLayers { get; init; } = -1;
    public bool Pin { get; init; } = false;
}

public sealed class LoadedModel
{
    public required string ModelId { get; init; }
    public required string SessionId { get; init; }
    public Process? Process { get; init; }
    public int Port { get; init; }
    public required InferenceBackend Backend { get; init; }
    public long MemoryUsage { get; set; }
    public required ModelType ModelType { get; init; }
    public string? Quantization { get; init; }
    public DateTimeOffset LastAccessed { get; set; } = DateTimeOffset.UtcNow;
    public int AccessCount { get; set; } = 0;
    public bool IsPinned { get; set; } = false;
    public TimeSpan LoadTime { get; set; }
}

public sealed class InferenceResult
{
    public required string ModelId { get; init; }
    public required object Output { get; init; }
    public TimeSpan InferenceTime { get; init; }
    public long TokensProcessed { get; init; }
    public double TokensPerSecond { get; init; }
}

public sealed class ModelHandle
{
    public ModelHandle(string modelId, string sessionId)
    {
        ModelId = modelId;
        SessionId = sessionId;
    }

    public string ModelId { get; }
    public string SessionId { get; }
}

public sealed class ModelStats
{
    public int LoadedModels { get; init; }
    public long TotalMemoryUsage { get; init; }
    public long AvailableMemory { get; init; }
    public Dictionary<string, ModelInfo> Models { get; init; } = new();
}

public sealed class ModelInfo
{
    public required string ModelId { get; init; }
    public required ModelType Type { get; init; }
    public long MemoryUsage { get; init; }
    public int AccessCount { get; init; }
    public DateTimeOffset LastAccessed { get; init; }
    public TimeSpan LoadTime { get; init; }
}

public enum ModelType
{
    LLM,
    Whisper,
    CLIP,
    Embedding
}

public enum InferenceBackend
{
    CPU,
    DirectML,
    ROCm,
    CUDA
}

public class InsufficientMemoryException : Exception
{
    public InsufficientMemoryException(string message) : base(message) { }
}

public class ModelNotLoadedException : Exception
{
    public ModelNotLoadedException(string message) : base(message) { }
}