using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Inference;

public interface IModelOrchestrator
{
    Task<ModelHandle> LoadModelAsync(ModelRequest request, CancellationToken ct = default);
    Task<InferenceResult> InferAsync(string modelId, object input, CancellationToken ct = default);
    Task UnloadModelAsync(string modelId);
    Task<ModelStats> GetStatsAsync();
}

public sealed class ModelOrchestrator : IModelOrchestrator, IDisposable
{
    private readonly ILogger<ModelOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, LoadedModel> _models = new();
    private readonly SemaphoreSlim _memoryLock = new(1, 1);
    private readonly PriorityQueue<InferenceRequest, int> _inferenceQueue = new();
    private readonly Timer _memoryMonitor;

    // Configuration
    private const long MaxMemoryBytes = 120L * 1024 * 1024 * 1024; // 120GB limit (leave 8GB for OS)
    private const long EmergencyThresholdBytes = 110L * 1024 * 1024 * 1024; // 110GB emergency threshold
    private const int MaxConcurrentInference = 2; // Limit concurrent GPU operations

    public ModelOrchestrator(ILogger<ModelOrchestrator> logger)
    {
        _logger = logger;
        _memoryMonitor = new Timer(MonitorMemoryPressure, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

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
                _logger.LogInformation("Model {ModelId} already loaded, access count: {Count}",
                    request.ModelId, existing.AccessCount);
                return new ModelHandle(request.ModelId, existing.SessionId);
            }

            // Calculate required memory
            var requiredMemory = EstimateModelMemory(request);
            var currentUsage = GetCurrentMemoryUsage();

            _logger.LogInformation("Loading model {ModelId}: requires {Required:N0} MB, current usage {Current:N0} MB",
                request.ModelId, requiredMemory / 1_048_576, currentUsage / 1_048_576);

            // Evict models if necessary
            while (currentUsage + requiredMemory > MaxMemoryBytes)
            {
                if (!await EvictLeastRecentlyUsedAsync())
                {
                    throw new InsufficientMemoryException(
                        $"Cannot load {request.ModelId}: requires {requiredMemory / 1_048_576:N0} MB, " +
                        $"but only {(MaxMemoryBytes - currentUsage) / 1_048_576:N0} MB available");
                }
                currentUsage = GetCurrentMemoryUsage();
            }

            // Load the model
            var model = await LoadModelInternalAsync(request);
            _models[request.ModelId] = model;

            _logger.LogInformation("Successfully loaded {ModelId} using {Memory:N0} MB",
                request.ModelId, model.MemoryUsage / 1_048_576);

            return new ModelHandle(request.ModelId, model.SessionId);
        }
        finally
        {
            _memoryLock.Release();
        }
    }

    public async Task<InferenceResult> InferAsync(string modelId, object input, CancellationToken ct = default)
    {
        if (!_models.TryGetValue(modelId, out var model))
        {
            throw new ModelNotLoadedException($"Model {modelId} is not loaded");
        }

        // Update access tracking
        model.LastAccessed = DateTimeOffset.UtcNow;
        model.AccessCount++;

        // Queue inference request
        var request = new InferenceRequest
        {
            ModelId = modelId,
            Input = input,
            Priority = CalculatePriority(model),
            CompletionSource = new TaskCompletionSource<InferenceResult>()
        };

        _inferenceQueue.Enqueue(request, request.Priority);

        // Process queue (this would run on background threads in production)
        _ = Task.Run(() => ProcessInferenceQueue(ct), ct);

        return await request.CompletionSource.Task;
    }

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

        _logger.LogInformation("Evicting model {ModelId} (last accessed: {LastAccessed}, count: {Count})",
            lru.ModelId, lru.LastAccessed, lru.AccessCount);

        await UnloadModelInternalAsync(lru);
        _models.TryRemove(lru.ModelId, out _);

        // Force garbage collection after model unload
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        return true;
    }

    private async Task<LoadedModel> LoadModelInternalAsync(ModelRequest request)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var startTime = Stopwatch.StartNew();

        // Determine optimal backend based on hardware
        var backend = SelectOptimalBackend(request);

        // Pre-allocate memory to avoid fragmentation
        var estimatedMemory = EstimateModelMemory(request);

        try
        {
            // Load model based on type
            var model = request.ModelType switch
            {
                ModelType.LLM => await LoadLLMAsync(request, backend, sessionId),
                ModelType.Whisper => await LoadWhisperAsync(request, backend, sessionId),
                ModelType.CLIP => await LoadCLIPAsync(request, backend, sessionId),
                ModelType.Embedding => await LoadEmbeddingAsync(request, backend, sessionId),
                _ => throw new NotSupportedException($"Model type {request.ModelType} not supported")
            };

            model.LoadTime = startTime.Elapsed;
            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model {ModelId}", request.ModelId);
            throw;
        }
    }

    private async Task<LoadedModel> LoadLLMAsync(ModelRequest request, InferenceBackend backend, string sessionId)
    {
        // Quantization strategy based on available memory
        var quantization = DetermineQuantization(request);

        var processInfo = new ProcessStartInfo
        {
            FileName = backend switch
            {
                InferenceBackend.ROCm => "/opt/rocm/bin/llm-server",
                InferenceBackend.DirectML => "llm-server-dml.exe",
                InferenceBackend.CPU => "llama.cpp/main",
                _ => throw new NotSupportedException()
            },
            Arguments = $"--model {request.ModelPath} " +
                       $"--port {GetNextPort()} " +
                       $"--ctx-size {request.ContextSize} " +
                       $"--batch-size {request.BatchSize} " +
                       $"--n-gpu-layers {request.GpuLayers} " +
                       $"--quantization {quantization}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = Process.Start(processInfo);

        // Wait for model to load (parse stdout for ready signal)
        await WaitForModelReadyAsync(process, TimeSpan.FromMinutes(5));

        return new LoadedModel
        {
            ModelId = request.ModelId,
            SessionId = sessionId,
            Process = process,
            Port = GetCurrentPort(),
            Backend = backend,
            MemoryUsage = GetProcessMemory(process),
            ModelType = ModelType.LLM,
            Quantization = quantization
        };
    }

    private long EstimateModelMemory(ModelRequest request)
    {
        return request.ModelType switch
        {
            ModelType.LLM => EstimateLLMMemory(request),
            ModelType.Whisper => request.ModelSize switch
            {
                "tiny" => 100L * 1024 * 1024,  // 100 MB
                "base" => 200L * 1024 * 1024,  // 200 MB
                "small" => 500L * 1024 * 1024, // 500 MB
                "medium" => 1500L * 1024 * 1024, // 1.5 GB
                "large" => 3000L * 1024 * 1024,  // 3 GB
                _ => 1000L * 1024 * 1024
            },
            ModelType.CLIP => 2000L * 1024 * 1024, // 2 GB
            ModelType.Embedding => 1000L * 1024 * 1024, // 1 GB
            _ => 1000L * 1024 * 1024
        };
    }

    private long EstimateLLMMemory(ModelRequest request)
    {
        // Parse model size from name (e.g., "llama-70b", "mistral-7b")
        var match = System.Text.RegularExpressions.Regex.Match(
            request.ModelPath, @"(\d+)b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success && int.TryParse(match.Groups[1].Value, out var billions))
        {
            // Base calculation: 2 bytes per parameter (FP16)
            long baseMemory = billions * 2L * 1_000_000_000L;

            // Adjust for quantization
            baseMemory = request.Quantization switch
            {
                "Q4_K_M" => baseMemory / 4,  // 4-bit quantization
                "Q5_K_M" => baseMemory * 5 / 16, // 5-bit quantization  
                "Q6_K" => baseMemory * 6 / 16,   // 6-bit quantization
                "Q8_0" => baseMemory / 2,        // 8-bit quantization
                _ => baseMemory // FP16 default
            };

            // Add overhead for context (typically 10-20% of model size)
            var contextOverhead = (long)(request.ContextSize * 2048 * 0.15);

            return baseMemory + contextOverhead;
        }

        // Default fallback
        return 8L * 1024 * 1024 * 1024; // 8GB default
    }

    private void MonitorMemoryPressure(object? state)
    {
        var usage = GetCurrentMemoryUsage();
        var percentage = (double)usage / MaxMemoryBytes * 100;

        if (usage > EmergencyThresholdBytes)
        {
            _logger.LogWarning("EMERGENCY: Memory usage at {Usage:N0} MB ({Percentage:F1}%), initiating emergency eviction",
                usage / 1_048_576, percentage);

            // Emergency eviction - remove least critical models
            Task.Run(async () =>
            {
                await _memoryLock.WaitAsync();
                try
                {
                    var toEvict = _models.Values
                        .Where(m => !m.IsPinned && m.ModelType != ModelType.LLM)
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

    private long GetCurrentMemoryUsage()
    {
        // Get system memory info
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var info = new MEMORYSTATUSEX();
            info.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref info))
            {
                return (long)(info.ullTotalPhys - info.ullAvailPhys);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Parse /proc/meminfo
            var meminfo = File.ReadAllLines("/proc/meminfo");
            var total = ParseMemInfo(meminfo, "MemTotal");
            var available = ParseMemInfo(meminfo, "MemAvailable");
            return total - available;
        }

        // Fallback to process memory
        return Process.GetCurrentProcess().WorkingSet64;
    }

    public void Dispose()
    {
        _memoryMonitor?.Dispose();

        // Unload all models
        foreach (var model in _models.Values)
        {
            UnloadModelInternalAsync(model).GetAwaiter().GetResult();
        }

        _models.Clear();
    }

    // Native memory APIs
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
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
    public int GpuLayers { get; init; } = -1; // -1 = all layers on GPU
    public bool Pin { get; init; } = false; // Prevent eviction
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

public sealed class InferenceRequest
{
    public required string ModelId { get; init; }
    public required object Input { get; init; }
    public int Priority { get; init; }
    public TaskCompletionSource<InferenceResult> CompletionSource { get; init; } = new();
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