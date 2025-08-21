using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class ModelConfiguration
    {
        public string ModelId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public ModelStatus Status { get; set; }
        public long MemoryUsage { get; set; }
        public string? LoadedPath { get; set; }
        public DateTimeOffset? LoadedAt { get; set; }
        public ModelCapabilities Capabilities { get; set; } = new();
        public string Name { get; set; } = string.Empty;
    }

    public class ModelCapabilities
    {
        public int MaxContextLength { get; set; }
        public List<string> SupportedLanguages { get; set; } = new();
        public List<string> SpecialFeatures { get; set; } = new();
        public bool SupportsStreaming { get; set; }
        public bool SupportsFineTuning { get; set; }
        public bool SupportsMultiModal { get; set; }
        public Dictionary<string, object> CustomCapabilities { get; set; } = new();
    }



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
        public string? Provider { get; set; }
        public Dictionary<string, object>? Options { get; set; }
    }


    public class ModelHandle
    {
        public string ModelId { get; set; } = string.Empty;
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string Provider { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public IntPtr Handle { get; set; }
        public long MemoryUsage { get; set; }
        public DateTimeOffset LoadedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class ModelStats
    {
        public int LoadedModels { get; init; }
        public long TotalMemoryUsage { get; init; }
        public long AvailableMemory { get; init; }
        public Dictionary<string, ModelInfo> Models { get; init; } = new();
    }

    /// <summary>
    /// Individual model information - ADD TO Models/ModelStats.cs or similar
    /// </summary>
    public sealed class ModelInfo
    {
        public required string ModelId { get; init; }
        public required ModelType Type { get; init; }
        public long MemoryUsage { get; init; }
        public int AccessCount { get; init; }
        public DateTimeOffset LastAccessed { get; init; }
        public TimeSpan LoadTime { get; init; }
        public double AverageTokensPerSecond { get; init; }
    }

    #region ExtendedModelConfiguration
    /// <summary>
    /// Extended model configuration that tracks whether it's from a template
    /// </summary>
    public class ExtendedModelConfiguration
    {
        /// <summary>
        /// The base model configuration
        /// </summary>
        public ModelConfiguration Configuration { get; set; } = new();

        /// <summary>
        /// Indicates if this model was loaded from a template
        /// </summary>
        public bool IsFromTemplate { get; set; }

        /// <summary>
        /// The template ID if this model is from a template
        /// </summary>
        public string? TemplateId { get; set; }

        /// <summary>
        /// When the model was added to the session
        /// </summary>
        public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Last time this model was used
        /// </summary>
        public DateTimeOffset LastUsedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Number of times this model has been used in the session
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Priority for unloading (lower = unload first)
        /// Template models have higher priority by default
        /// </summary>
        public int UnloadPriority => IsFromTemplate ? 100 : 50;
    }

    /// <summary>
    /// Session extension for tracking model sources
    /// </summary>
    public class SessionModelTracking
    {
        /// <summary>
        /// Session ID this tracking belongs to
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// All models in the session with extended tracking
        /// Key: Model ID, Value: Extended configuration
        /// </summary>
        public Dictionary<string, ExtendedModelConfiguration> Models { get; set; } = new();

        /// <summary>
        /// The template ID used for this session (if any)
        /// </summary>
        public string? TemplateId { get; set; }

        /// <summary>
        /// Template name for display purposes
        /// </summary>
        public string? TemplateName { get; set; }

        /// <summary>
        /// Gets models that are from the template
        /// </summary>
        public IEnumerable<ExtendedModelConfiguration> GetTemplateModels()
        {
            foreach (var model in Models.Values)
            {
                if (model.IsFromTemplate)
                    yield return model;
            }
        }

        /// <summary>
        /// Gets models that were added ad-hoc
        /// </summary>
        public IEnumerable<ExtendedModelConfiguration> GetAdHocModels()
        {
            foreach (var model in Models.Values)
            {
                if (!model.IsFromTemplate)
                    yield return model;
            }
        }

        /// <summary>
        /// Calculates total memory usage
        /// </summary>
        public long GetTotalMemoryUsage()
        {
            long total = 0;
            foreach (var model in Models.Values)
            {
                total += model.Configuration.MemoryUsage;
            }
            return total;
        }
    }

    /// <summary>
    /// Memory pressure response when trying to load a model
    /// </summary>
    public class MemoryPressureResponse
    {
        /// <summary>
        /// Whether there's enough memory to load the requested model
        /// </summary>
        public bool CanLoad { get; set; }

        /// <summary>
        /// Available memory in bytes
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// Memory required for the requested model
        /// </summary>
        public long RequiredMemory { get; set; }

        /// <summary>
        /// Memory that would need to be freed
        /// </summary>
        public long MemoryDeficit => Math.Max(0, RequiredMemory - AvailableMemory);

        /// <summary>
        /// Suggested models to unload (ordered by priority)
        /// </summary>
        public List<ModelUnloadSuggestion> SuggestedUnloads { get; set; } = new();

        /// <summary>
        /// Message to display to the user
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Suggestion for which model to unload
    /// </summary>
    public class ModelUnloadSuggestion
    {
        /// <summary>
        /// Model ID to unload
        /// </summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the model
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Memory that would be freed
        /// </summary>
        public long MemoryToFree { get; set; }

        /// <summary>
        /// Whether this is from a template
        /// </summary>
        public bool IsTemplateModel { get; set; }

        /// <summary>
        /// Last time this model was used
        /// </summary>
        public DateTimeOffset LastUsed { get; set; }

        /// <summary>
        /// Reason this model is suggested for unloading
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to load a model with tracking
    /// </summary>
    public class TrackedModelLoadRequest
    {
        /// <summary>
        /// The model ID to load
        /// </summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>
        /// Session ID this is for
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is from a template
        /// </summary>
        public bool IsFromTemplate { get; set; }

        /// <summary>
        /// Template ID if from template
        /// </summary>
        public string? TemplateId { get; set; }

        /// <summary>
        /// Force load even if memory pressure
        /// </summary>
        public bool ForceLoad { get; set; }

        /// <summary>
        /// Models user has agreed to unload
        /// </summary>
        public List<string> ModelsToUnload { get; set; } = new();
    }
    #endregion

}
