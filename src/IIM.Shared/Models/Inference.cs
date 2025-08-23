using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    #region InferencePipeline

    public sealed class QueuedRequest
    {
        public required string Id { get; init; }
        public required InferencePipelineRequest Request { get; init; }
        public required Priority Priority { get; init; }
        public DateTimeOffset QueuedAt { get; init; }
        public TaskCompletionSource<object?> CompletionSource { get; init; } = new();
        public CancellationToken CancellationToken { get; init; }
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

    /// <summary>
    /// Request for inference pipeline execution.
    /// This is the SINGLE source of truth - no duplicates elsewhere!
    /// </summary>
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

        public InferencePipelineRequest(string modelId, object input, Dictionary<string, object>? parameters = null)
            : this(modelId, input)
        {
            Parameters = parameters;
        }
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

    // Enhanced notification classes
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

    #endregion

    public sealed class InferenceResult
    {
        public required string ModelId { get; init; }
        public required object Output { get; init; }
        public TimeSpan InferenceTime { get; init; }
        public long TokensProcessed { get; init; }
        public int TokensGenerated { get; init; }
        public double TokensPerSecond { get; init; }
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
}
