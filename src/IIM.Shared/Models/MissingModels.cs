using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


    namespace IIM.Shared.Models;

    // ========================================
    // AI/ML INFERENCE MODELS
    // ========================================

    #region Inference Models

    /// <summary>
    /// Inference pipeline request
    /// </summary>
    public class InferencePipelineRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ModelId { get; set; } = string.Empty;
        public object Input { get; set; } = new { };
        public Dictionary<string, object>? Parameters { get; set; }
        public HashSet<string>? Tags { get; set; }
        public int Priority { get; set; } = 1;
        public int? Index { get; set; }
        public CancellationToken CancellationToken { get; set; } = default;
    }

    /// <summary>
    /// Inference request
    /// </summary>
    public class InferenceRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
        public List<string>? StopSequences { get; set; }
        public int? MaxTokens { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
    }

    /// <summary>
    /// Inference result
    /// </summary>
    public class InferenceResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public object Output { get; set; } = new { };
        public TimeSpan InferenceTime { get; set; }
        public int TokensGenerated { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Queued request for pipeline
    /// </summary>
    public class QueuedRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N"),
        public InferencePipelineRequest Request { get; set; } = new();
        public TaskCompletionSource<InferenceResult> CompletionSource { get; set; } = new();
        public DateTimeOffset QueuedAt { get; set; } = DateTimeOffset.UtcNow;
        public Priority Priority { get; set; } = Priority.Normal;
}

    /// <summary>
    /// Batch result
    /// </summary>
    public class BatchResult<T>
    {
        public List<T> Results { get; set; } = new();
        public List<int> FailedIndices { get; set; } = new();
        public TimeSpan TotalTime { get; set; }
        public Dictionary<int, Exception> Errors { get; set; } = new();
        public int TotalRequests { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }

    /// <summary>
    /// Health check result
    /// </summary>
    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public List<string> Issues { get; set; } = new();
        public InferencePipelineStats Stats { get; set; } = new();
    }

    /// <summary>
    /// Metric entry for monitoring
    /// </summary>
    public class MetricEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public double LatencyMs { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorType { get; set; }
    }

    /// <summary>
    /// Inference pipeline statistics - Updated with missing properties
    /// </summary>
    public class InferencePipelineStats
    {
        public long TotalRequests { get; set; }
        public long CompletedRequests { get; set; }
        public long FailedRequests { get; set; }
        public long RejectedRequests { get; set; }  // Added
        public int PendingRequests { get; set; }
        public int ActiveRequests { get; set; }

        // Queue depth properties
        public int HighPriorityQueueDepth { get; set; }  // Added
        public int NormalPriorityQueueDepth { get; set; }  // Added
        public int LowPriorityQueueDepth { get; set; }  // Added

        // Resource availability
        public int GpuSlotsAvailable { get; set; }  // Added
        public int CpuSlotsAvailable { get; set; }  // Added

        // Latency metrics
        public double AverageLatencyMs { get; set; }
        public double P50LatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public double P99LatencyMs { get; set; }

        public double ErrorRate { get; set; }
        public int RequestsPerMinute { get; set; }
        public Dictionary<string, long> RequestsByModel { get; set; } = new();
    }

    #endregion

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
        public ModelRequest Request { get; set; } = new();
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
    /// Model metadata
    /// </summary>
    public class ModelMetadata
    {
        public string ModelId { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ModelPath { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public HashType  HashType { get; set; } = HashType.SHA256;
        public string FileName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    public ModelType Type { get; set; }
        public bool RequiresGpu { get; set; }
        public bool SupportsBatching { get; set; }
        public int MaxBatchSize { get; set; } = 1;
        public long EstimatedMemoryMb { get; set; }
        public int DefaultPriority { get; set; } = 1;
        public string Provider { get; set; } = "cpu";
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

/// <summary>
/// Model request
/// </summary>
public class ModelRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string? ModelPath { get; set; }
    public ModelType ModelType { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public string ModelSize { get; set; }
    public string Quantization { get; set; }
    public int ContextSize { get; set; }
    public int BatchSize { get; set; } = 512;
    public int GpuLayers { get; set; } = -1; //Use all available GPU layers
    public string Provider { get; set; } = "cpu";
    public Dictionary<string, object> Options { get; set; } = new();


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
    }

    #endregion

    #region Embeddings

    /// <summary>
    /// Embedding response
    /// </summary>
    public class EmbeddingResponse
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public int Dimensions { get; set; }
        public string Model { get; set; } = string.Empty;
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Batch embedding response
    /// </summary>
    public class BatchEmbeddingResponse
    {
        public List<float[]> Embeddings { get; set; } = new();
        public int Count { get; set; }
        public string Model { get; set; } = string.Empty;
        public TimeSpan TotalProcessingTime { get; set; }
    }

    /// <summary>
    /// Embedding model info
    /// </summary>
    public class EmbeddingModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Dimensions { get; set; }
        public bool IsLoaded { get; set; }
        public long MemoryUsage { get; set; }
        public List<string> SupportedInputTypes { get; set; } = new();
    }

    /// <summary>
    /// Embedding models response
    /// </summary>
    public class EmbeddingModelsResponse
    {
        public List<EmbeddingModelInfo> Models { get; set; } = new();
    }

    /// <summary>
    /// Embedding service info
    /// </summary>
    public class EmbeddingServiceInfo
    {
        public string Status { get; set; } = string.Empty;
        public List<EmbeddingModelInfo> LoadedModels { get; set; } = new();
        public long TotalMemoryUsage { get; set; }
        public int RequestsProcessed { get; set; }
        public double AverageLatencyMs { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    #endregion

    // ========================================
    // VECTOR STORE MODELS
    // ========================================

    #region Vector Store

    /// <summary>
    /// Vector point for storage
    /// </summary>
    public class VectorPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public DateTimeOffset? Timestamp { get; set; }
    }

    /// <summary>
    /// Vector configuration
    /// </summary>
    public class VectorConfig
    {
        public int Dimensions { get; set; } = 384;
        public string Distance { get; set; } = "Cosine";
        public bool OnDisk { get; set; } = false;
        public int? QuantizationConfig { get; set; }
    }

    /// <summary>
    /// Search result from vector store
    /// </summary>
    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public float? Distance { get; set; }
    }

    /// <summary>
    /// Search filter
    /// </summary>
    public class SearchFilter
    {
        public Dictionary<string, object>? Must { get; set; }
        public Dictionary<string, object>? Should { get; set; }
        public Dictionary<string, object>? MustNot { get; set; }
        public TimeRange? TimeRange { get; set; }
        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// Qdrant info
    /// </summary>
    public class QdrantInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Collections { get; set; }
        public long TotalPoints { get; set; }
        public long StorageUsedBytes { get; set; }
    }

    /// <summary>
    /// Collection info
    /// </summary>
    public class CollectionInfo
    {
        public string Name { get; set; } = string.Empty;
        public VectorConfig Config { get; set; } = new();
        public long PointsCount { get; set; }
        public long SegmentsCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Storage info
    /// </summary>
    public class StorageInfo
    {
        public long TotalBytes { get; set; }
        public long UsedBytes { get; set; }
        public long AvailableBytes { get; set; }
        public int CollectionsCount { get; set; }
        public Dictionary<string, long> CollectionSizes { get; set; } = new();
    }

    /// <summary>
    /// Cluster
    /// </summary>
    public class Cluster
    {
        public int Id { get; set; }
        public float[] Centroid { get; set; } = Array.Empty<float>();
        public List<string> PointIds { get; set; } = new();
        public int Size { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    #endregion

    // ========================================
    // NOTIFICATION EVENT MODELS
    // ========================================

    #region Notification Events

    /// <summary>
    /// Notification filter
    /// </summary>
    public class NotificationFilter
    {
        public bool? IsRead { get; set; }
        public NotificationType? Type { get; set; }
        public NotificationCategory? Category { get; set; }
        public NotificationPriority? Priority { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? Topic { get; set; }
        public string? SourceType { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
    }

    /// <summary>
    /// Create notification request
    /// </summary>
    public class CreateNotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public NotificationCategory Category { get; set; } = NotificationCategory.System;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public string? SourceId { get; set; }
        public string? SourceType { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public NotificationAction? PrimaryAction { get; set; }
        public List<NotificationAction>? SecondaryActions { get; set; }
        public string? ImageUrl { get; set; }
        public string? Topic { get; set; }
    }

    /// <summary>
    /// Notification received event args
    /// </summary>
    public class NotificationReceivedEventArgs : EventArgs
    {
        public Notification Notification { get; set; } = null!;
    }

    /// <summary>
    /// Notification read event args
    /// </summary>
    public class NotificationReadEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
        public DateTimeOffset ReadAt { get; set; }
    }

    /// <summary>
    /// Notification deleted event args
    /// </summary>
    public class NotificationDeletedEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
    }

    #endregion

    // ========================================
    // ADDITIONAL MISSING MODELS
    // ========================================

    #region Additional Models

    /// <summary>
    /// Chain of custody report
    /// </summary>
    public class ChainOfCustodyReport
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;
        public List<ChainOfCustodyEntry> ChainEntries { get; set; } = new();
        public List<ProcessedEvidence> ProcessedVersions { get; set; } = new();
        public bool IntegrityValid { get; set; }
        public string Signature { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime IngestTimestamp { get; set; } = DateTime.UtcNow;
        public string MachineIdentifier { get; set; } = Environment.MachineName;
        public Dictionary<string, string> OriginalHashes { get; set; } = new();
        public List<AuditEvent> AuditLog { get; set; } = new();
        public string PublicKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Evidence export
    /// </summary>
    public class EvidenceExport
    {
        public string ExportId { get; set; } = Guid.NewGuid().ToString();
        public string EvidenceId { get; set; } = string.Empty;
        public string ExportPath { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new();
        public bool IntegrityValid { get; set; }
        public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
        public string ExportedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Plugin info
    /// </summary>
    public class PluginInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string? PackagePath { get; set; }
        public bool IsLoaded { get; set; }
        public List<string> Functions { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion

 


    // ========================================
    // INTERFACES
    // ========================================

    #region Interfaces

    /// <summary>
    /// Notification interface for MediatR
    /// </summary>
    public interface INotification
    {
    }

    #endregion

    // ========================================
    // EXCEPTIONS
    // ========================================

    #region Exceptions

    /// <summary>
    /// Inference pipeline exception
    /// </summary>
    public class InferencePipelineException : Exception
    {
        public InferencePipelineException(string message) : base(message) { }
        public InferencePipelineException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Model load exception
    /// </summary>
    public class ModelLoadException : Exception
    {
        public ModelLoadException(string message) : base(message) { }
        public ModelLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Inference exception
    /// </summary>
    public class InferenceException : Exception
    {
        public InferenceException(string message) : base(message) { }
        public InferenceException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion

