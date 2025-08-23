// Generated 08/23/2025 12:10:20
    public class AnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
  
        public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
        public string PerformedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Results { get; set; } = new();    
        public double Confidence { get; set; }
        public List<string> Tags { get; set; } = new();

    public AnalysisType Type { get; set; }
    public List<Finding> Findings { get; set; } = new();  // Using existing Finding from IIM.Shared.Models
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public TimeSpan AnalysisTime { get; set; }
    public float ConfidenceScore { get; set; }
}


public class Citation
{
    // Existing core properties
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? Location { get; set; }
    public double Relevance { get; set; }

    // New optional properties
    public int? Index { get; set; }  // Citation number in response
    public string? Source { get; set; }  // Source name/title
    public string? Url { get; set; }  // Link to source
    public DateTimeOffset? AccessedAt { get; set; }  // When cited
    public string? Author { get; set; }  // Source author
    public DateTimeOffset? PublishedAt { get; set; }  // Publication date
    public Dictionary<string, object>? Metadata { get; set; }
}


public class ImageAnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string EvidenceId { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public List<DetectedObject> Objects { get; set; } = new();
        public List<DetectedFace> Faces { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<SimilarImage> SimilarImages { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }


    public class DetectedObject
    {
        public string Label { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public BoundingBox BoundingBox { get; set; } = new();
    }


    public class DetectedFace
    {
        public string Id { get; set; } = string.Empty;
        public BoundingBox BoundingBox { get; set; } = new();
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public Dictionary<string, double> Emotions { get; set; } = new();
        public int? Age { get; set; }
        public string? Gender { get; set; }
    }


    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }


    public class SimilarImage
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public double Similarity { get; set; }
    }


    public class Attachment
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public AttachmentType Type { get; set; }
        public string? StoragePath { get; set; }

        // New optional properties
        public string? Hash { get; set; }  // SHA-256 hash
        public string? ThumbnailPath { get; set; }  // For images/videos
        public DateTimeOffset? UploadedAt { get; set; }  // Upload timestamp
        public string? UploadedBy { get; set; }  // User who uploaded
        public ProcessingStatus? ProcessingStatus { get; set; }  // Analysis status
        public Dictionary<string, object>? ExtractedMetadata { get; set; }  // EXIF, etc.
        public string? PreviewUrl { get; set; }  // For quick preview
        public bool? IsProcessed { get; set; }  // Has been analyzed
        public System.IO.Stream? Stream { get; set; }  // File stream for upload
    }


    public class AuditEvent
    {
        public long Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? RequestId { get; set; }
        public string? UserId { get; set; }
        public string? ModelId { get; set; }
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public string? Result { get; set; }
        public string? Details { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Priority { get; set; }
        public long? DurationMs { get; set; }
        public int? TokensGenerated { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public Dictionary<string, object>? AdditionalData { get; set; }
    }


    public class AuditLogFilter
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? EventType { get; set; }
        public string? UserId { get; set; }
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public int? Limit { get; set; } = 100;
        public int? Offset { get; set; }
    }


    public class ChainOfCustodyEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public string PreviousHash { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }

        public string Officer { get; set; }
        public string Notes { get; set; }
    }


    public class Case
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseNumber { get; set; } = string.Empty;
        public CaseType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CaseStatus Status { get; set; }
        public CasePriority Priority { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string Classification { get; set; }
        public List<string> AccessControlList { get; set; }

        // New optional properties
        public string? Name { get; set; }  // Case name (alias for Title)
        public string? ModelTemplateId { get; set; }  // Default model configuration
        public List<InvestigationSession>? Sessions { get; set; }  // All sessions for this case
        public List<Timeline> Timelines { get; set; }
        public List<Evidence>? Evidence { get; set; }  // All evidence
        public List<Finding>? Findings { get; set; }  // Key findings
        public List<string>? AssignedTo { get; set; }  // Assigned investigators
        public List<Report> Reports { get; set; }
        public string? LeadInvestigator { get; set; }  // Lead investigator
        public List<string>? TeamMembers { get; set; }  // Investigation team
        public DateTimeOffset? ClosedAt { get; set; }  // When case was closed
        public string? ClosedBy { get; set; }  // Who closed it
        public Dictionary<string, object>? Statistics { get; set; }  // Case metrics

        // Computed properties for convenience
        public string GetName() => Name ?? Title;
    }


    internal class Configuration
    {
    }


    public class ChunkData
    {
        public string Hash { get; set; }
        public byte[] Data { get; set; }
        public int Size { get; set; }
        public int Offset { get; set; }
    }


    public class DeduplicationResult
    {
        public string FileHash { get; set; }
        public long TotalSize { get; set; }
        public List<string> ChunkHashes { get; set; } = new();
        public List<ChunkData> UniqueChunks { get; set; } = new();
        public List<ChunkData> DuplicateChunks { get; set; } = new();
        public long BytesSaved { get; set; }
        public double DeduplicationRatio { get; set; }
    }


    public class DeviceInfo
    {
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public long MemoryAvailable { get; set; }
        public long MemoryTotal { get; set; }
        public bool SupportsDirectML { get; set; }
        public bool SupportsROCm { get; set; }
    }


    public class Evidence
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseId { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public EvidenceType Type { get; set; }
        public EvidenceStatus Status { get; set; }
        public string Hash { get; set; } = string.Empty;
        public string HashAlgorithm { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public DateTimeOffset IngestTimestamp { get; set; } = DateTimeOffset.UtcNow;
        public EvidenceMetadata Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }

        // New optional properties for compatibility
        public string? FileType { get; set; }  // MIME type
        public string? FileName { get; set; }  // Alias for OriginalFileName
        public DateTimeOffset? UploadedAt { get; set; }  // Alias for IngestTimestamp
        public DateTimeOffset? UpdatedAt { get; set; }  // Last update timestamp
        public string? UploadedBy { get; set; }  // User who uploaded
        public bool? IntegrityValid { get; set; }  // Hash verification status
        public string? Signature { get; set; }  // Digital signature
        public List<ChainOfCustodyEntry>? ChainOfCustody { get; set; }
        public List<ProcessedEvidence>? ProcessedVersions { get; set; }
        public Dictionary<string, string>? Hashes { get; set; }  // Multiple hash types

        // Computed properties for convenience
        public string GetFileType() => FileType ?? DetermineFileType(OriginalFileName);
        public DateTimeOffset GetUploadedAt() => UploadedAt ?? IngestTimestamp;
        public DateTimeOffset GetUpdatedAt() => UpdatedAt ?? IngestTimestamp;

        private string DetermineFileType(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" or ".docx" => "application/msword",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream"
            };


public class EvidenceMetadata
    {
        public string CaseNumber { get; set; } = string.Empty;
        public string CollectedBy { get; set; } = string.Empty;
        public DateTimeOffset CollectionDate { get; set; } = DateTimeOffset.UtcNow;
        public string CollectionLocation { get; set; } = string.Empty;
        public string DeviceSource { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> CustomFields { get; set; } = new();

        public string? SessionId { get; set; }
    }


    public class EvidenceContext
    {
        public string CaseId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
    }


    public class ProcessedEvidence
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OriginalEvidenceId { get; set; } = string.Empty;
        public string ProcessingType { get; set; } = string.Empty;
        public DateTimeOffset ProcessedTimestamp { get; set; } = DateTimeOffset.UtcNow;
        public string ProcessedBy { get; set; } = string.Empty;
        public string ProcessedHash { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public Dictionary<string, object>? ProcessingResults { get; set; }
        public TimeSpan? ProcessingDuration { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }


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

        // Additional fields for law enforcement compliance
        public DateTime IngestTimestamp { get; set; } = DateTime.UtcNow;
        public string MachineIdentifier { get; set; } = Environment.MachineName;
        public Dictionary<string, string> OriginalHashes { get; set; } = new();
        public List<AuditEvent> AuditLog { get; set; } = new();
        public string PublicKey { get; set; } = string.Empty;
    }


    public class EvidenceExport
    {
        public string ExportId { get; set; } = Guid.NewGuid().ToString("N");
        public string EvidenceId { get; set; } = string.Empty;
        public string ExportPath { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new();
        public bool IntegrityValid { get; set; }
        public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
        public string ExportedBy { get; set; } = string.Empty;
    }


    public class ExportConfiguration
    {
        public ExportFormat Format { get; set; }
        public ExportOptions Options { get; set; } = new();
        public string? TemplateName { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }


    public class ExportTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public string? HeaderTemplate { get; set; }
        public string? BodyTemplate { get; set; }
        public string? FooterTemplate { get; set; }
        public Dictionary<string, object> DefaultOptions { get; set; } = new();
        public bool IsSystemTemplate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
    }


    public class ExportOperation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EntityType { get; set; } = string.Empty; // Response, Report, Case, etc.
        public string EntityId { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public ExportStatus Status { get; set; } = ExportStatus.Pending;
        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }


public class FileMetadata
{
    public string FilePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}


public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Accuracy { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}


    public class InferencePipelineStats
    {
        public long TotalRequests { get; set; }
        public long CompletedRequests { get; set; }
        public long FailedRequests { get; set; }
        public long RejectedRequests { get; set; }
        public int PendingRequests { get; set; }
        public int HighPriorityQueueDepth { get; set; }
        public int NormalPriorityQueueDepth { get; set; }
        public int LowPriorityQueueDepth { get; set; }
        public int GpuSlotsAvailable { get; set; }
        public int CpuSlotsAvailable { get; set; }
        public double AverageLatencyMs { get; set; }
        public double P50LatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public double P99LatencyMs { get; set; }
        public double ErrorRate { get; set; }
        public int RequestsPerMinute { get; set; }
        public Dictionary<string, long> RequestsByModel { get; set; } = new();
    }


    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public List<string> Issues { get; set; } = new();
        public InferencePipelineStats Stats { get; set; } = new();
    }


    public class MetricEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public double LatencyMs { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorType { get; set; }
    }


    public class InferencePipelineRequest
    {
        // Identity
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        // Required fields
        public string ModelId { get; set; } = string.Empty;
        public object Input { get; set; } = new { };

        // Optional parameters
        public Dictionary<string, object>? Parameters { get; set; }
        public HashSet<string>? Tags { get; set; }

        // Execution control
        public int Priority { get; set; } = 1;  // 0=Low, 1=Normal, 2=High
        public int? Index { get; set; }  // For batch processing
        public CancellationToken CancellationToken { get; set; } = default;

        // Constructors for different use cases
        public InferencePipelineRequest() { }

        public InferencePipelineRequest(string modelId, object input)
        {
            ModelId = modelId ?? throw new ArgumentNullException(nameof(modelId));
            Input = input ?? throw new ArgumentNullException(nameof(input));
        }


    public class BatchResult<T>
    {
        public List<T> Results { get; set; } = new();
        public List<int> FailedIndices { get; set; } = new();
        public TimeSpan TotalTime { get; set; }
        public Dictionary<int, Exception> Errors { get; set; } = new();

        // ADD THESE PROPERTIES
        public int TotalRequests { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }


    public class OrchestratorStats
    {
        public long TotalMemoryUsage { get; set; }
        public long AvailableMemory { get; set; }
        public int LoadedModels { get; set; }
        public Dictionary<string, ModelStats> Models { get; set; } = new();
    }


    public class InferencePipelineException : Exception
    {
        public InferencePipelineException(string message) : base(message) { }
        public InferencePipelineException(string message, Exception innerException) : base(message, innerException) { }
    }


    public class InferenceQueuedNotification : INotification
    {
        public string RequestId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public Priority Priority { get; set; }
        public int QueueDepth { get; set; }
    }


    public class InferenceStartedNotification : INotification
    {
        public string RequestId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public long QueueTimeMs { get; set; }
    }


    public class InferenceCompletedNotification : INotification
    {
        public string RequestId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public long QueueTimeMs { get; set; }
        public long InferenceTimeMs { get; set; }
        public int TokensGenerated { get; set; }
    }


    public class InferenceFailedNotification : INotification
    {
        public string RequestId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
    }


    public class InferenceRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
        public List<string>? StopSequences { get; set; }
        public int? MaxTokens { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
    }


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


    public class ModelRuntimeOptions
    {
        public long MaxMemory { get; set; }
        public int DeviceId { get; set; }
        public ModelPriority Priority { get; set; }
        public string ExecutionProvider { get; set; } = "CPU";
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }


    public class ModelConstraints
    {
        public long? MaxMemoryBytes { get; set; }
        public bool PreferLocal { get; set; }
        public ModelType? RequiredType { get; set; }
        public List<string>? RequiredCapabilities { get; set; }
    }


    public class ModelRecommendation
    {
        public string ModelId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public float ConfidenceScore { get; set; }
        public List<string> AlternativeModels { get; set; } = new();
        public Dictionary<string, object>? RecommendedParameters { get; set; }
    }


    public class ServiceStatus
    {
        public string Name { get; set; } = string.Empty;
        public ServiceState State { get; set; }
        public bool IsHealthy { get; set; }
        public string? Endpoint { get; set; }
        public string? Version { get; set; }
        public long MemoryUsage { get; set; }
        public double CpuUsage { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public string? Message { get; set; }
    }


    public class ServiceConfig
    {
        public string Name { get; set; } = string.Empty;
        public ServiceType Type { get; set; }
        public string? DockerImage { get; set; }
        public int Port { get; set; }
        public string? HealthEndpoint { get; set; }
        public string StartupCommand { get; set; } = string.Empty;
        public string? WorkingDirectory { get; set; }
        public long RequiredMemoryMb { get; set; }
        public ServicePriority Priority { get; set; }
        public Dictionary<string, string> Environment { get; set; } = new();
    }


    public class PerformanceMetrics
    {

        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkLatency { get; set; }
        public int ActiveConnections { get; set; }
        public int RequestsPerSecond { get; set; }
        public double AverageResponseTime { get; set; }

        // Model Performance Metrics
        public double TokensPerSecond { get; set; }
        public double AverageLatencyMs { get; set; }
        public double P50LatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public double P99LatencyMs { get; set; }
        public double Throughput { get; set; }

        // Quality Metrics
        public double Accuracy { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1Score { get; set; }
        public double Confidence { get; set; }

        // Resource Utilization
        public long VramUsageBytes { get; set; }
        public long RamUsageBytes { get; set; }
        public double GpuUtilization { get; set; }
        public double GpuTemperature { get; set; }
        public double PowerConsumptionWatts { get; set; }

        // Inference Metrics
        public long TotalInferences { get; set; }
        public long SuccessfulInferences { get; set; }
        public long FailedInferences { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }

        // Queue Metrics
        public int QueueLength { get; set; }
        public int ActiveRequests { get; set; }
        public int PendingRequests { get; set; }
        public double QueueWaitTimeMs { get; set; }

        // Model-Specific Metrics
        public string ModelId { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public int ContextLength { get; set; }
        public int BatchSize { get; set; }
        public int MaxTokens { get; set; }

        // Timestamp
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public TimeSpan MeasurementPeriod { get; set; } = TimeSpan.FromMinutes(1);

        // Additional Metrics Dictionary for extensibility
        public Dictionary<string, object> CustomMetrics { get; set; } = new();

        // Helper Methods
        public double GetEfficiency()
        {
            return SuccessfulInferences > 0
                ? (double)SuccessfulInferences / TotalInferences * 100
                : 0;
        }


    public class AggregatedPerformanceMetrics
    {
        public string MetricId { get; set; } = Guid.NewGuid().ToString("N");
        public string ModelId { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        // Aggregated values
        public double MinTokensPerSecond { get; set; }
        public double MaxTokensPerSecond { get; set; }
        public double AvgTokensPerSecond { get; set; }

        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public double AvgLatencyMs { get; set; }

        public double MinAccuracy { get; set; }
        public double MaxAccuracy { get; set; }
        public double AvgAccuracy { get; set; }

        public long TotalRequests { get; set; }
        public long TotalTokensProcessed { get; set; }
        public double TotalGpuHours { get; set; }

        public List<PerformanceMetrics> Samples { get; set; } = new();
    }


    public class PerformanceBenchmark
    {
        public string BenchmarkId { get; set; } = Guid.NewGuid().ToString("N");
        public string ModelId { get; set; } = string.Empty;
        public string BenchmarkName { get; set; } = string.Empty;
        public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;

        // Test parameters
        public int NumberOfRuns { get; set; }
        public int BatchSize { get; set; }
        public int SequenceLength { get; set; }
        public string TestDataset { get; set; } = string.Empty;

        // Results
        public double TokensPerSecond { get; set; }
        public double AverageLatencyMs { get; set; }
        public double Accuracy { get; set; }
        public double MemoryUsageGb { get; set; }
        public double PowerEfficiency { get; set; } // Tokens per watt

        public Dictionary<string, object> DetailedResults { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }


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

        public string? SessionId { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public string? ModelPath { get; set; }
        public long RequiredMemory { get; set; }
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


    public class ModelHandle
    {
        public string ModelId { get; set; } = string.Empty;
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string Provider { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public IntPtr Handle { get; set; }
        public long MemoryUsage { get; set; }
        public DateTimeOffset LoadedAt { get; set; } = DateTimeOffset.UtcNow;

        public ModelState State { get; set; } = ModelState.Loading;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }


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


    public class ModelMetadata
    {
        public string ModelId { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public bool RequiresGpu { get; set; }
        public bool SupportsBatching { get; set; }
        public int MaxBatchSize { get; set; } = 1;
        public long EstimatedMemoryMb { get; set; }
        public int DefaultPriority { get; set; } = 1; // 0=Low, 1=Normal, 2=High
        public string Provider { get; set; } = "cpu";
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }    
        public Dictionary<string, object> Properties { get; set; } = new();
    }


    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public NotificationCategory Category { get; set; } = NotificationCategory.System;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ReadAt { get; set; }
        public string? SourceId { get; set; }
        public string? SourceType { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public NotificationAction? PrimaryAction { get; set; }
        public List<NotificationAction>? SecondaryActions { get; set; }
        public string? ImageUrl { get; set; }
        public string? Topic { get; set; }
    }


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


    public class NotificationAction
    {
        public string Label { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // "navigate", "execute", "dismiss"
        public string? Target { get; set; } // URL or command
        public Dictionary<string, object>? Parameters { get; set; }
    }


    public class NotificationReceivedEventArgs : EventArgs
    {
        public Notification Notification { get; set; } = null!;
    }


    public class NotificationReadEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
        public DateTimeOffset ReadAt { get; set; }
    }


    public class NotificationDeletedEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
    }


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


public class ProcessResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool Success => ExitCode == 0;
}


    public class ProcessingResult
    {
        public string ProcessingId { get; set; }
        public string EvidenceId { get; set; }
        public ProcessingStatus Status { get; set; }
        public string ProcessingType { get; set; }
        public Dictionary<string, object>? Results { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }
    }


    public class QdrantInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Collections { get; set; }
        public long TotalPoints { get; set; }
        public long StorageUsedBytes { get; set; }
    }


    public class VectorConfig
    {
        public int Dimensions { get; set; } = 384; // Default for all-MiniLM-L6-v2
        public string Distance { get; set; } = "Cosine"; // Cosine, Euclidean, Dot
        public bool OnDisk { get; set; } = false;
        public int? QuantizationConfig { get; set; }
    }


    public class CollectionInfo
    {
        public string Name { get; set; } = string.Empty;
        public VectorConfig Config { get; set; } = new();
        public long PointsCount { get; set; }
        public long SegmentsCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }


    public class VectorPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public DateTimeOffset? Timestamp { get; set; }
    }


    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public float? Distance { get; set; }
    }


    public class SearchFilter
    {
        public Dictionary<string, object>? Must { get; set; }
        public Dictionary<string, object>? Should { get; set; }
        public Dictionary<string, object>? MustNot { get; set; }
        public TimeRange? TimeRange { get; set; }
        public List<string>? Tags { get; set; }
    }


    public class Cluster
    {
        public int Id { get; set; }
        public float[] Centroid { get; set; } = Array.Empty<float>();
        public List<string> PointIds { get; set; } = new();
        public int Size { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }


    public class StorageInfo
    {
        public long TotalBytes { get; set; }
        public long UsedBytes { get; set; }
        public long AvailableBytes { get; set; }
        public int CollectionsCount { get; set; }
        public Dictionary<string, long> CollectionSizes { get; set; } = new();
    }


    public class RagResponse
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Query { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public double Confidence { get; set; }

        // New optional properties
        public List<RetrievedChunk>? RetrievedChunks { get; set; }  // Retrieved document chunks
        public List<object>? Chunks { get; set; }  // Generic chunks for compatibility
        public List<string>? Sources { get; set; }  // Source documents
        public Dictionary<string, double>? SourceScores { get; set; }  // Relevance scores
        public TimeSpan? RetrievalTime { get; set; }  // Time to retrieve
        public TimeSpan? GenerationTime { get; set; }  // Time to generate answer
    }


    public class RetrievedChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Source { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public double Relevance { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }


    public class RAGSearchResult
    {
        public List<RAGDocument> Documents { get; set; } = new();
        public List<Entity> Entities { get; set; } = new();
        public List<Relationship> Relationships { get; set; } = new();
        public KnowledgeGraph? KnowledgeGraph { get; set; }
        public QueryUnderstanding QueryUnderstanding { get; set; } = new();
        public List<string> SuggestedFollowUps { get; set; } = new();
        public Dictionary<string, object> CaseContext { get; set; } = new();
    }


    public class RAGDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Relevance { get; set; }
        public string? SourceId { get; set; }
        public string? SourceType { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<int> ChunkIndices { get; set; } = new();
    }


    public class Entity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public EntityType Type { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> Aliases { get; set; } = new();
        public List<Relationship> Relationships { get; set; } = new();
        public List<string> AssociatedCaseIds { get; set; } = new();
        public double RiskScore { get; set; }
        public DateTimeOffset FirstSeen { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();
    }


    public class Relationship
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SourceEntityId { get; set; } = string.Empty;
        public string TargetEntityId { get; set; } = string.Empty;
        public RelationshipType Type { get; set; }
        public double Strength { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }


    public class KnowledgeGraph
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }


    public class GraphNode
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
    }


    public class GraphEdge
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Weight { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }


    public class QueryUnderstanding
    {
        public List<string> KeyTerms { get; set; } = new();
        public string Intent { get; set; } = string.Empty;
        public List<string> RequiredCapabilities { get; set; } = new();
        public double Complexity { get; set; }
    }


    public class MockTranscriptionResult
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public float Confidence { get; set; }
        public TimeSpan Duration { get; set; }
        public Word[] Words { get; set; } = Array.Empty<Word>();
        public TimeSpan ProcessingTime { get; set; }
        public string DeviceUsed { get; set; } = string.Empty;
    }


    public class MockBoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }


    public class Word
    {
        public string Text { get; set; } = string.Empty;
        public float Start { get; set; }
        public float End { get; set; }
    }


    public class ImageSearchResults
    {
        public List<ImageMatch> Matches { get; set; } = new();
        public TimeSpan QueryProcessingTime { get; set; }
        public int TotalImagesSearched { get; set; }
    }


    public class ImageMatch
    {
        public string ImagePath { get; set; } = string.Empty;
        public float Score { get; set; }
        public MockBoundingBox? BoundingBox { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }


    public class Source
    {
        public string Document { get; set; } = string.Empty;
        public int Page { get; set; }
        public float Relevance { get; set; }
    }


    public class ReasoningResult
    {
        // Existing core properties
        public string QueryId { get; set; } = Guid.NewGuid().ToString();
        public string OriginalQuery { get; set; } = string.Empty;
        public IntentExtractionResult Intent { get; set; } = new();
        public Dictionary<string, object> ExtractedEntities { get; set; } = new();
        public double Confidence { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public List<ReasoningStep> ActionPlan { get; set; } = new();
        // New optional properties
        public string? RecommendedModel { get; set; }  // Best model for this query
        public List<string>? RequiredTools { get; set; }  // Tools needed
        public Dictionary<string, double>? ModelScores { get; set; }  // Model confidence scores
        public bool? RequiresRAG { get; set; }  // Needs document search
        public List<string>? RelevantEvidenceIds { get; set; }  // Related evidence
    }


    public class ReasoningChain
    {
        public string ChainId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public List<ReasoningStep> Steps { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public ChainExecutionMode Mode { get; set; } = ChainExecutionMode.Sequential;
    }


    public class ReasoningStep
    {
        public string StepId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public bool IsOptional { get; set; }
        public int MaxRetries { get; set; } = 3;
    }


    public class ChainExecutionResult
    {
        public string ChainId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<StepResult> StepResults { get; set; } = new();
        public Dictionary<string, object> FinalOutput { get; set; } = new();
        public TimeSpan TotalExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
    }


    public class StepResult
    {
        public string StepId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public object? Output { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
    }


    public class ReasoningProgress
    {
        public string OperationId { get; set; } = string.Empty;
        public string CurrentStep { get; set; } = string.Empty;
        public int StepsCompleted { get; set; }
        public int TotalSteps { get; set; }
        public float PercentComplete => TotalSteps > 0 ? (float)StepsCompleted / TotalSteps * 100 : 0;
        public string Status { get; set; } = string.Empty;
    }


    public class IntentExtractionResult
    {
        public string PrimaryIntent { get; set; } = string.Empty;
        public Dictionary<string, float> IntentScores { get; set; } = new();
        public List<string> Entities { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public float Confidence { get; set; }
    }


    public class ReasoningRequest
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public object Input { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public InvestigationSession? Session { get; set; }
    }


    public class RoutingDecision
    {
        public string RequestId { get; set; } = string.Empty;
        public string SelectedHandler { get; set; } = string.Empty;
        public string HandlerType { get; set; } = string.Empty;
        public Dictionary<string, object> HandlerParameters { get; set; } = new();
        public float ConfidenceScore { get; set; }
    }


    public class ReasoningStartedEventArgs : EventArgs
    {
        public string QueryId { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }


    public class ReasoningCompletedEventArgs : EventArgs
    {
        public string QueryId { get; set; } = string.Empty;
        public ReasoningResult Result { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }


    public class ReasoningStepCompletedEventArgs : EventArgs
    {
        public string ChainId { get; set; } = string.Empty;
        public string StepId { get; set; } = string.Empty;
        public StepResult Result { get; set; } = new();
    }


    public class ReasoningErrorEventArgs : EventArgs
    {
        public string OperationId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }


    public class Report
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public ReportStatus Status { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<ReportSection> Sections { get; set; } = new();
        public List<string> EvidenceIds { get; set; } = new();
        public List<Finding> Findings { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset? SubmittedAt { get; set; }
        public string? SubmittedTo { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }


    public class ReportSection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<string> EvidenceReferences { get; set; } = new();
        public List<Visualization> Visualizations { get; set; } = new();
    }


    public class Finding
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FindingSeverity Severity { get; set; }
        public double Confidence { get; set; }
        public List<string> SupportingEvidenceIds { get; set; } = new();
        public List<string> RelatedEntityIds { get; set; } = new();
        public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;
    }


    public class Recommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RecommendationPriority Priority { get; set; }
        public string Rationale { get; set; } = string.Empty;
        public List<string> RelatedFindingIds { get; set; } = new();
    }


    public class Timeline
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CaseId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<TimelineEvent> Events { get; set; } = new();
        public List<TimelinePattern> Patterns { get; set; } = new();
        public List<TimelineAnomaly> Anomalies { get; set; } = new();
        public List<CriticalPeriod> CriticalPeriods { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
    }


    public class TimelineEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public EventType Type { get; set; }
        public EventImportance Importance { get; set; }
        public string? EvidenceId { get; set; }
        public List<string> RelatedEntityIds { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public GeoLocation? Location { get; set; }
    }


    public class TimelinePattern
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public PatternType Type { get; set; }
        public List<string> EventIds { get; set; } = new();
        public double Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
    }


    public class TimelineAnomaly
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset Timestamp { get; set; }
        public AnomalyType Type { get; set; }
        public double Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> AffectedEventIds { get; set; } = new();
    }


    public class CriticalPeriod
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string Description { get; set; } = string.Empty;
        public CriticalityLevel Level { get; set; }
        public List<string> EventIds { get; set; } = new();
    }


    public class InvestigationSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = "New Investigation";
        public string Icon { get; set; } = "🕵️‍♂️";
        public InvestigationType Type { get; set; } = InvestigationType.GeneralInquiry;
        public List<InvestigationMessage> Messages { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, ModelConfiguration> Models { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = Environment.UserName;
        public InvestigationStatus Status { get; set; } = InvestigationStatus.Active;
        public List<Finding> Findings { get; set; } = new();
    }


    public class InvestigationMessage
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<Attachment>? Attachments { get; set; }
        public List<ToolResult>? ToolResults { get; set; }
        public List<Citation>? Citations { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? ModelUsed { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        
        // Properties from DTO version
        public RAGSearchResult? RAGResults { get; set; }
        public List<TranscriptionResult>? Transcriptions { get; set; }
        public List<ImageAnalysisResult>? ImageAnalyses { get; set; }
        public List<string>? EvidenceIds { get; set; }
        public List<string>? EntityIds { get; set; }
        public string? FineTuneJobId { get; set; }
        
        // New optional properties
        public string? SessionId { get; set; }  // Session this message belongs to
        public string? ParentMessageId { get; set; }  // For threaded conversations
        public List<string>? ChildMessageIds { get; set; }  // Reply chain
        public bool? IsEdited { get; set; }  // Message was edited
        public DateTimeOffset? EditedAt { get; set; }  // When edited
        public string? EditedBy { get; set; }  // Who edited
        public MessageStatus? Status { get; set; }  // Processing status
        public double? Confidence { get; set; }  // Confidence score
    }


    public class InvestigationQuery
    {
        public string Text { get; set; } = string.Empty;
        public List<Attachment> Attachments { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        
        // Properties from DTO version
        public string? SessionId { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public List<string>? RequestedTools { get; set; }
    } = string.Empty;


    public class InvestigationResponse
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        public List<ToolResult>? ToolResults { get; set; }
        public List<Citation>? Citations { get; set; }
        public List<Evidence>? RelatedEvidence { get; set; }
        public double? Confidence { get; set; }
        public ResponseDisplayType DisplayType { get; set; } = ResponseDisplayType.Auto;
        public Dictionary<string, object>? Metadata { get; set; }
        public Dictionary<string, object>? DisplayMetadata { get; set; }
        public List<Visualization>? Visualizations { get; set; }

        
        // Properties from DTO version
        public RAGSearchResult? RAGResults { get; set; }
        public List<TranscriptionResult>? Transcriptions { get; set; }
        public List<ImageAnalysisResult>? ImageAnalyses { get; set; }
        public List<string>? EvidenceIds { get; set; }
        public List<string>? EntityIds { get; set; }
        public string? FineTuneJobId { get; set; }
        
        // New optional properties
        public string? SessionId { get; set; }  // Session this response belongs to
        public DateTimeOffset? Timestamp { get; set; }  // When response was generated
        public string? QueryId { get; set; }  // Original query ID
        public string? ModelUsed { get; set; }  // Which model generated this
        public TimeSpan? ProcessingTime { get; set; }  // How long it took
        public string? CreatedBy { get; set; }  // User or system
        public DateTimeOffset? CreatedAt { get; set; }  // Creation timestamp
        public string? Hash { get; set; }  // For integrity verification
        public ResponseVisualization? Visualization { get; set; }  // Primary visualization
    }


    public class ResponseVisualization
    {
        public VisualizationType Type { get; set; } = VisualizationType.Auto;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public object Data { get; set; } = new { };
        public Dictionary<string, object>? Options { get; set; }


        public string? ChartType { get; set; } // For Chart visualizations (bar, line, pie, etc.)
        public List<string>? Columns { get; set; } // For Table visualizations
        public string? GraphType { get; set; } // For Graph visualizations (network, tree, etc.)
        public string? MapType { get; set; } // For Map visualizations (heat, markers, etc.)
        public string? CustomTemplate { get; set; } // For Custom visualizations
    }


    public class CreateSessionRequest
    { 
        // Existing constructor and properties
        public CreateSessionRequest(string caseId, string title, string investigationType)
        {
            CaseId = caseId;
            Title = title;
            InvestigationType = investigationType;
        }


    public class Setting
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }  // JSON serialized
        public string Category { get; set; }  // "MinIO", "Deployment", etc.
        public string Description { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsUserConfigurable { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }


    public class SystemStatus
    {
        public bool IsHealthy { get; set; }
        public long MemoryUsed { get; set; }
        public long MemoryTotal { get; set; }
        public int LoadedModels { get; set; }
        public double CpuUsage { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }


    public class TestConnectionResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
    }


public class TimeRange
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    
    public TimeSpan Duration => End - Start;
    
    public bool Contains(DateTimeOffset timestamp) => 
        timestamp >= Start && timestamp <= End;
}


    public class ToolResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ToolName { get; set; } = string.Empty;
        public ToolStatus Status { get; set; }
        public object? Data { get; set; }
        public List<Visualization> Visualizations { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }


        public ResponseDisplayType? PreferredDisplayType { get; set; }
    }


    public class ToolExecution
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SessionId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public ToolResult? Result { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string ExecutedBy { get; set; } = string.Empty;
    }


    public class Visualization
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public VisualizationType Type { get; set; } = VisualizationType.Auto;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public object Data { get; set; } = new { };
        public Dictionary<string, object> Options { get; set; } = new();
        public string? RenderFormat { get; set; } // html, svg, canvas, etc.
    }


    public class TranscriptionResult
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public double Confidence { get; set; }

        // New optional properties
        public TimeSpan? Duration { get; set; }  // Audio duration
        public List<TranscriptionSegment>? Segments { get; set; }  // Time-aligned segments
        public Dictionary<string, object>? Metadata { get; set; }
        public string? AudioFileId { get; set; }  // Source audio file
        public DateTimeOffset? ProcessedAt { get; set; }  // When transcribed
        public string? ModelUsed { get; set; }  // Which model was used
    }


    public class TranscriptionSegment
    {
        public int Start { get; set; }  // Start time in milliseconds
        public int End { get; set; }  // End time in milliseconds
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string? Speaker { get; set; }  // Speaker diarization
    }


    public class WslStatus
    {
        public bool WslReady { get; set; }
        public bool DistroRunning { get; set; }
        public bool ServicesHealthy { get; set; }
        public bool NetworkConnected { get; set; }
        public List<string> Issues { get; set; } = new();
    }


