using IIM.Core.Models;
using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.AI
{
    /// <summary>
    /// Orchestrates AI model loading, unloading, and resource management.
    /// Works with existing ModelConfiguration and ModelHandle types.
    /// </summary>
    public interface IModelOrchestrator
    {
        // Model Lifecycle
        Task<ModelHandle> LoadModelAsync(ModelRequest request, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        Task<bool> UnloadModelAsync(string modelId, CancellationToken cancellationToken = default);
        Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default);
        Task<List<ModelConfiguration>> GetLoadedModelsAsync(CancellationToken cancellationToken = default);
        Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

        // Model Information (uses existing ModelConfiguration)
        Task<ModelConfiguration?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default);
        Task<bool> UpdateModelParametersAsync(string modelId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

        // Resource Management
        Task<GpuStats> GetGpuStatsAsync(CancellationToken cancellationToken = default);
        Task<ModelResourceUsage> GetModelResourceUsageAsync(string modelId, CancellationToken cancellationToken = default);
        Task<bool> OptimizeMemoryAsync(CancellationToken cancellationToken = default);
        Task<long> GetTotalMemoryUsageAsync(CancellationToken cancellationToken = default);

        // Model Download & Installation
        Task<bool> DownloadModelAsync(string modelId, string source, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteModelAsync(string modelId, CancellationToken cancellationToken = default);
        Task<long> GetModelSizeAsync(string modelId, CancellationToken cancellationToken = default);
        Task<ModelStats> GetStatsAsync();

        /// <summary>
        /// Performs inference using a loaded model
        /// </summary>
        /// <param name="modelId">ID of the model to use</param>
        /// <param name="input">Input data for inference</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Inference result with output and metrics</returns>
        Task<InferenceResult> InferAsync(string modelId, object input, CancellationToken ct = default);

        // Events
        event EventHandler<ModelLoadedEventArgs>? ModelLoaded;
        event EventHandler<ModelUnloadedEventArgs>? ModelUnloaded;
        event EventHandler<ModelErrorEventArgs>? ModelError;
        event EventHandler<ResourceThresholdEventArgs>? ResourceThresholdExceeded;
    }

    // Additional DTOs needed for the orchestrator
    public class GpuStats
    {
        public string DeviceName { get; set; } = string.Empty;
        public long TotalMemory { get; set; }
        public long UsedMemory { get; set; }
        public long AvailableMemory { get; set; }
        public float UtilizationPercent { get; set; }
        public float TemperatureCelsius { get; set; }
        public float PowerWatts { get; set; }
        public bool IsROCmAvailable { get; set; }
        public bool IsDirectMLAvailable { get; set; }
    }

    public class ModelResourceUsage
    {
        public string ModelId { get; set; } = string.Empty;
        public long MemoryBytes { get; set; }
        public long VramBytes { get; set; }
        public float CpuPercent { get; set; }
        public float GpuPercent { get; set; }
        public int ActiveSessions { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    public class DownloadProgress
    {
        public string ModelId { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
        public long DownloadedBytes { get; set; }
        public float ProgressPercent { get; set; }
        public float SpeedMBps { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
    }

    // Event Args
    public class ModelLoadedEventArgs : EventArgs
    {
        public string ModelId { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public long MemoryUsage { get; set; }
        public TimeSpan LoadTime { get; set; }
    }

    public class ModelUnloadedEventArgs : EventArgs
    {
        public string ModelId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class ModelErrorEventArgs : EventArgs
    {
        public string ModelId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    public class ResourceThresholdEventArgs : EventArgs
    {
        public string ResourceType { get; set; } = string.Empty;
        public float CurrentUsage { get; set; }
        public float Threshold { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }
}