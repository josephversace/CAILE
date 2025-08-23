// File: src/IIM.Shared/Models/LoadedModel.cs

using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Represents a model that has been loaded into memory and is ready for inference.
    /// Tracks runtime information and resource usage.
    /// </summary>
    public class LoadedModel
    {
        /// <summary>
        /// The handle returned when the model was loaded
        /// </summary>
        public required ModelHandle Handle { get; init; }

        /// <summary>
        /// The configuration of the loaded model
        /// </summary>
        public required ModelConfiguration Configuration { get; init; }

        /// <summary>
        /// Associated process if the model runs in a separate process (e.g., Ollama)
        /// </summary>
        public Process? Process { get; set; }

        /// <summary>
        /// When the model was last accessed for inference
        /// </summary>
        public DateTimeOffset LastAccessed { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Number of times this model has been accessed
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// Current state of the loaded model
        /// </summary>
        public ModelRuntimeState RuntimeState { get; set; } = ModelRuntimeState.Initializing;

        /// <summary>
        /// Performance metrics for this model
        /// </summary>
        public ModelPerformanceMetrics Metrics { get; set; } = new();

        /// <summary>
        /// Custom runtime data specific to the provider
        /// </summary>
        public Dictionary<string, object> RuntimeData { get; set; } = new();

        public ModelRequest Request { get; set; }
        public string ModelPath { get; set; } = string.Empty;
        public ModelRuntimeOptions RuntimeOptions { get; set; } = new();
      
    }

    /// <summary>
    /// Runtime state of a loaded model
    /// </summary>
    public enum ModelRuntimeState
    {
        /// <summary>Model is being initialized</summary>
        Initializing,

        /// <summary>Model is warming up (loading weights, etc.)</summary>
        WarmingUp,

        /// <summary>Model is ready for inference</summary>
        Ready,

        /// <summary>Model is currently processing a request</summary>
        Busy,

        /// <summary>Model is idle but ready</summary>
        Idle,

        /// <summary>Model is being unloaded</summary>
        Unloading,

        /// <summary>Model encountered an error</summary>
        Error,

        /// <summary>Model is suspended to save resources</summary>
        Suspended
    }

    /// <summary>
    /// Performance metrics for a loaded model
    /// </summary>
    public class ModelPerformanceMetrics
    {
        /// <summary>
        /// Total number of inference requests processed
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Number of successful inference requests
        /// </summary>
        public long SuccessfulRequests { get; set; }

        /// <summary>
        /// Number of failed inference requests
        /// </summary>
        public long FailedRequests { get; set; }

        /// <summary>
        /// Average inference time in milliseconds
        /// </summary>
        public double AverageInferenceMs { get; set; }

        /// <summary>
        /// Minimum inference time in milliseconds
        /// </summary>
        public double MinInferenceMs { get; set; } = double.MaxValue;

        /// <summary>
        /// Maximum inference time in milliseconds
        /// </summary>
        public double MaxInferenceMs { get; set; }

        /// <summary>
        /// Average tokens per second (for text models)
        /// </summary>
        public double AverageTokensPerSecond { get; set; }

        /// <summary>
        /// Total tokens processed (for text models)
        /// </summary>
        public long TotalTokensProcessed { get; set; }

        /// <summary>
        /// Current queue depth (pending requests)
        /// </summary>
        public int QueueDepth { get; set; }

        /// <summary>
        /// Timestamp of last metric update
        /// </summary>
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;



        /// <summary>
        /// Updates metrics with a new inference result
        /// </summary>
        public void UpdateWithInference(double inferenceMs, bool success, long? tokens = null)
        {
            TotalRequests++;

            if (success)
            {
                SuccessfulRequests++;

                // Update timing metrics
                MinInferenceMs = Math.Min(MinInferenceMs, inferenceMs);
                MaxInferenceMs = Math.Max(MaxInferenceMs, inferenceMs);

                // Update rolling average
                AverageInferenceMs = ((AverageInferenceMs * (SuccessfulRequests - 1)) + inferenceMs) / SuccessfulRequests;

                // Update token metrics if applicable
                if (tokens.HasValue)
                {
                    TotalTokensProcessed += tokens.Value;
                    var tokensPerSecond = (tokens.Value / inferenceMs) * 1000;
                    AverageTokensPerSecond = ((AverageTokensPerSecond * (SuccessfulRequests - 1)) + tokensPerSecond) / SuccessfulRequests;
                }
            }
            else
            {
                FailedRequests++;
            }

            LastUpdated = DateTimeOffset.UtcNow;
        }
    }


    /// <summary>
    /// Additional supporting classes for model management.
    /// </summary>

 
     
    

    /// <summary>
    /// Runtime options for a loaded model.
    /// </summary>
    public class ModelRuntimeOptions
    {
        public long MaxMemory { get; set; }
        public int DeviceId { get; set; }
        public ModelPriority Priority { get; set; }
        public string ExecutionProvider { get; set; } = "CPU";
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }

    /// <summary>
    /// Model constraints for filtering/selection.
    /// </summary>
    public class ModelConstraints
    {
        public long? MaxMemoryBytes { get; set; }
        public bool PreferLocal { get; set; }
        public ModelType? RequiredType { get; set; }
        public List<string>? RequiredCapabilities { get; set; }
    }

    /// <summary>
    /// Model recommendation result.
    /// </summary>
    public class ModelRecommendation
    {
        public string ModelId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public float ConfidenceScore { get; set; }
        public List<string> AlternativeModels { get; set; } = new();
        public Dictionary<string, object>? RecommendedParameters { get; set; }
    }
}