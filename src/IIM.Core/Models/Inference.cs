
using System;
using System.Collections.Generic;
using System.Threading;

namespace IIM.Core.Models;

public class InferencePipelineRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ModelId { get; set; } = string.Empty;
    public object Input { get; set; } = new { };
    public Dictionary<string, object>? Parameters { get; set; }
    public HashSet<string>? Tags { get; set; }
    public int Priority { get; set; } = 1;
    public int? Index { get; set; }
    public CancellationToken CancellationToken { get; set; }
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
    public Dictionary<int, string> Errors { get; set; } = new();
}

public class OrchestratorStats
{
    public long TotalMemoryUsage { get; set; }
    public long AvailableMemory { get; set; }
    public int LoadedModels { get; set; }
    public Dictionary<string, ModelStats> Models { get; set; } = new();
}

public class ModelStats
{
    public string ModelId { get; set; } = string.Empty;
    public ModelType Type { get; set; }
    public long MemoryUsage { get; set; }
    public int AccessCount { get; set; }
    public DateTimeOffset LastAccessed { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public double AverageTokensPerSecond { get; set; }
}