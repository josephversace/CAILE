using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.AI
{
    /// <summary>
    /// Abstraction for loading, unloading, and managing machine learning models (ONNX, GGUF, etc).
    /// Provides ONNX InferenceSession for inference when applicable.
    /// </summary>
    public interface IModelOrchestrator : IDisposable
    {
        /// <summary>
        /// Loads a model into memory and prepares it for inference.
        /// </summary>
        Task<ModelHandle> LoadModelAsync(ModelLoadRequest request, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unloads a model from memory and releases its resources.
        /// </summary>
        Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns true if the model is currently loaded in memory.
        /// </summary>
        Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of all currently loaded models.
        /// </summary>
        Task<List<ModelConfiguration>> GetLoadedModelsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of all models available on disk.
        /// </summary>
        Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a model, either loaded or on disk.
        /// </summary>
        Task<ModelConfiguration?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an ONNX InferenceSession for the specified model (if ONNX).
        /// Throws if the model/session is not loaded.
        /// </summary>
        InferenceSession GetOnnxSession(string modelId);

        long GetTotalMemoryUsageAsync();
    }
}



