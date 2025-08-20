
using System;
using System.Collections.Generic;
using System.Threading;

namespace IIM.Core.Models;

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


public class InferencePipelineStats
{
    public long TotalRequests { get; set; }
    public long CompletedRequests { get; set; }
    public long FailedRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ActiveRequests { get; set; }
    public double AverageLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public Dictionary<string, long> RequestsByModel { get; set; } = new();
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

