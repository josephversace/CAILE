using IIM.Shared.Dtos;
using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



    #region Model Management

    /// <summary>
    /// Loaded model information
    /// </summary>
    public class LoadedModel
    {
        public required ModelHandle Handle { get; init; }
        public required ModelConfiguration Configuration { get; init; }
        public Process? Process { get; set; }
        public DateTimeOffset LastAccessed { get; set; } = DateTimeOffset.UtcNow;
        public int AccessCount { get; set; }
        public ModelRuntimeState RuntimeState { get; set; } = ModelRuntimeState.Initializing;
        public ModelPerformanceMetrics Metrics { get; set; } = new();
        public Dictionary<string, object> RuntimeData { get; set; } = new();
        public ModelLoadRequest Request { get; set; } = new();
        public string ModelPath { get; set; } = string.Empty;
        public ModelRuntimeOptions RuntimeOptions { get; set; } = new();
    }

    /// <summary>
    /// Model handle
    /// </summary>
    public class ModelHandle
    {
        public string ModelId { get; set; } = string.Empty;
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string Provider { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public IntPtr Handle { get; set; }
        public long MemoryUsage { get; set; }
        public DateTimeOffset LoadedAt { get; set; } = DateTimeOffset.UtcNow;
        public ModelState State { get; set; } = ModelState.Loading;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }



/// <summary>
/// Model capabilities
/// </summary>
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

/// <summary>
/// Model performance metrics
/// </summary>
public class ModelStats
{
    public string ModelId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long MemoryUsage { get; set; }
    public int AccessCount { get; set; }
    public DateTimeOffset LastAccessed { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public double AverageTokensPerSecond { get; set; }
}

/// <summary>
/// Model metadata
/// </summary>
public class ModelConfiguration
{
	public Guid Id { get; set; } = Guid.NewGuid();


	public string ModelId { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ModelPath { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public ModelFormat Format { get; set; } = ModelFormat.Unknown;
    public string Hash { get; set; } = string.Empty;
    public HashType HashType { get; set; } = HashType.SHA256;
    public string Description { get; set; } = string.Empty;
    public ModelType Type { get; set; }
    public bool RequiresGpu { get; set; }
    public bool SupportsBatching { get; set; }
    public int MaxBatchSize { get; set; } = 1;
    public long EstimatedMemoryMb { get; set; }
    public int DefaultPriority { get; set; } = 1;
    public string Provider { get; set; } = "directml";
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ModelStatus Status { get; set; }
    public long MemoryUsage { get; set; }
    public long RequiredMemory { get; set; }
    public string? LoadedPath { get; set; }
    public DateTimeOffset? LoadedAt { get; set; }
    public ModelCapabilities Capabilities { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
    public string Name { get; set; }
    public DeviceType DeviceType { get;set; } = DeviceType.CPU;
}


    /// <summary>
    /// Model constraints
    /// </summary>
    public class ModelConstraints
    {
        public long? MaxMemoryBytes { get; set; }
        public bool PreferLocal { get; set; }
        public ModelType? RequiredType { get; set; }
        public List<string>? RequiredCapabilities { get; set; }
    }

    /// <summary>
    /// Model recommendation
    /// </summary>
    public class ModelRecommendation
    {
        public string ModelId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public float ConfidenceScore { get; set; }
        public List<string> AlternativeModels { get; set; } = new();
        public Dictionary<string, object>? RecommendedParameters { get; set; }
    }

    /// <summary>
    /// Model runtime options
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
    /// Model performance metrics
    /// </summary>
    public class ModelPerformanceMetrics
    {
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long FailedRequests { get; set; }
        public double AverageInferenceMs { get; set; }
        public double MinInferenceMs { get; set; } = double.MaxValue;
        public double MaxInferenceMs { get; set; }
        public double AverageTokensPerSecond { get; set; }
        public long TotalTokensProcessed { get; set; }
        public int QueueDepth { get; set; }
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Model info
    /// </summary>
    public class ModelInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long MemoryUsage { get; set; }
        public string? LoadedPath { get; set; }
        public DateTimeOffset? LoadedAt { get; set; }
        public ModelCapabilities Capabilities { get; set; } = new();
        public Dictionary<string, object>? Metadata { get; set; }
    public object DownloadedAt { get; set; }
    public object Message { get; set; }
}

/// <summary>
/// Model load request
/// </summary>
public class ModelLoadRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string? ModelPath { get; set; }
    public string? Provider { get; set; }
    public Dictionary<string, object>? Options { get; set; }
    public ModelType ModelType { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public string ModelSize { get; set; }
    public string Quantization { get; set; }
    public int ContextSize { get; set; }
    public int BatchSize { get; set; } = 512;
    public int GpuLayers { get; set; } = -1; //Use all available GPU layers

}


    /// <summary>
    /// Represents a named set of parameters for a specific model.
    /// </summary>
    public class ModelParameterSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ModelId { get; set; } = string.Empty; // FK to ModelConfiguration
        public string Name { get; set; } = string.Empty;    // "Default", "Low Memory", etc.
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }



#endregion

#region ModelTemplates



public class ModelTemplate
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string? Description { get; set; }
	public MultiModelDto Models { get; set; } = new();
	public List<string> EnabledTools { get; set; } = new();

	public IEnumerable<ModelDefinitionDto> GetAllSlots()
	{
		if (Models.Chat is not null) yield return Models.Chat;
		if (Models.Reasoning is not null) yield return Models.Reasoning;
		if (Models.Coding is not null) yield return Models.Coding;
		if (Models.Embedding is not null) yield return Models.Embedding;
		if (Models.Vision is not null) yield return Models.Vision;
		if (Models.Multimodal is not null) yield return Models.Multimodal;
	}
}


public  class ModelDefinition
{
	public string Id { get; set; } = "";
	public string DisplayName { get; set; } = "";
	public string? Description { get; set; }

	public string? FoundryModelId { get; set; }
	public string? LocalPath { get; set; }

	public double Temperature { get; set; } = 0.7;
	public int MaxTokens { get; set; } = 2048;
	public double TopP { get; set; } = 0.9;

	public string? SystemPrompt { get; set; }
	public string? CustomPromptFormat { get; set; }

	public string Category { get; set; } = "chat";
}

public class BasicModelDefinition : ModelDefinition { }

public class MultiModel
{
    public ModelDefinition? Chat { get; set; }
    public ModelDefinition? Reasoning { get; set; }
    public ModelDefinition? Embedding { get; set; }
    public ModelDefinition? Vision { get; set; }
    public ModelDefinition? Multimodal { get; set; }

    // Future expansion:
    public ModelDefinition? Coding { get; set; }
    public ModelDefinition? Planning { get; set; }
}



/// <summary>
/// Pipeline configuration for model orchestration
/// </summary>
public class PipelineConfig
{
    /// <summary>
    /// RAG-specific configuration
    /// </summary>
    public RagPipelineConfig? Rag { get; set; }

    /// <summary>
    /// Multi-modal pipeline configuration
    /// </summary>
    public MultiModalConfig? MultiModal { get; set; }

    /// <summary>
    /// Default processing order for tools/models
    /// </summary>
    public List<string> ProcessingOrder { get; set; } = new();
}

/// <summary>
/// RAG pipeline configuration
/// </summary>
public class RagPipelineConfig
{
    public int ChunkSize { get; set; } = 512;
    public int ChunkOverlap { get; set; } = 50;
    public int TopK { get; set; } = 5;
    public float MinRelevanceScore { get; set; } = 0.7f;
    public bool UseReranking { get; set; } = true;
    public string? RerankingModel { get; set; }
}

/// <summary>
/// Multi-modal pipeline configuration
/// </summary>
public class MultiModalConfig
{
    public bool EnableCrossModalSearch { get; set; } = true;
    public bool AutoTranscribeAudio { get; set; } = true;
    public bool AutoAnalyzeImages { get; set; } = true;
    public bool AutoExtractText { get; set; } = true;
}

/// <summary>
/// Tool configuration for investigation tools
/// </summary>
public class ToolConfig
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Performance preferences for the template
/// </summary>
public class PerformancePreferences
{
    /// <summary>
    /// Priority: Speed, Quality, or Balanced
    /// </summary>
    public string Priority { get; set; } = "Balanced";

    /// <summary>
    /// Maximum number of models loaded concurrently
    /// </summary>
    public int MaxConcurrentModels { get; set; } = 3;

    /// <summary>
    /// Maximum memory usage in bytes
    /// </summary>
    public long MaxMemoryUsage { get; set; } = 64L * 1024 * 1024 * 1024; // 64GB default

    /// <summary>
    /// Enable response streaming for real-time updates
    /// </summary>
    public bool EnableStreaming { get; set; } = true;
}

/// <summary>
/// Template metadata for tracking and analytics
/// </summary>
public class TemplateMetadata
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;
    public int UsageCount { get; set; } = 0;
    public List<string> RecentCases { get; set; } = new();
}

#endregion


#region ModelEndpoints


/// <summary>
/// Request to load a model
/// </summary>
public record LoadModelLoadRequest(
    string? ModelPath,
    ModelType? ModelType,
    string? ModelSize,
    string? Quantization,
    int? ContextLength,
    int? DeviceId,
    ModelPriority? Priority,
    Dictionary<string, object>? Parameters,
    bool? PreloadToGpu,
    long? MaxMemory);

/// <summary>
/// Model status response
/// </summary>
public record ModelStatusResponse
{
    public string ModelId { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public ModelStatus Status { get; set; }
    public DateTimeOffset? LoadedAt { get; set; }
    public long MemoryUsage { get; set; }
    public DeviceType DeviceType { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request to predict memory requirements
/// </summary>
public record PredictMemoryRequest(
    string ModelId,
    string? ModelSize,
    string? Quantization);

/// <summary>
/// Memory prediction result
/// </summary>
public record MemoryPrediction(
    long EstimatedMemory,
    long MinimumMemory,
    long RecommendedMemory,
    DeviceType RecommendedDevice,
    Dictionary<string, object> Details);

#endregion


#region ModelOrchestrator


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
    public long MemoryUsage { get; set; }
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
    public long MemoryFreed { get; set; }
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

#endregion
