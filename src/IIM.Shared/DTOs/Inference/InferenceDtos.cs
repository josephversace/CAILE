using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Request for text generation
    /// </summary>
    public record GenerateRequest(
        string ModelId,
        string Prompt,
        Dictionary<string, object>? Parameters = null,
        HashSet<string>? Tags = null
    );

    /// <summary>
    /// Response from text generation
    /// </summary>
    public record GenerateResponse(
        string Text,
        string ModelId,
        DateTimeOffset Timestamp,
        int? TokensUsed = null,
        TimeSpan? InferenceTime = null
    );

    /// <summary>
    /// Single inference request
    /// </summary>
    public record InferenceRequest(
        string ModelId,
        object Input,
        Dictionary<string, object>? Parameters = null,
        HashSet<string>? Tags = null,
        int Priority = 1,
        bool Stream = false
    );

    /// <summary>
    /// Inference response
    /// </summary>
    public record InferenceResponse(
        string ModelId,
        object Output,
        TimeSpan InferenceTime,
        int? TokensUsed = null,
        Dictionary<string, object>? Metadata = null
    );

    /// <summary>
    /// Batch inference request
    /// </summary>
    public record BatchInferenceRequest(
        string ModelId,
        List<object> Inputs,
        Dictionary<string, object>? Parameters = null,
        int BatchSize = 32,
        bool Parallel = true
    );

    /// <summary>
    /// Batch inference response
    /// </summary>
    public record BatchInferenceResponse(
        List<InferenceResponse> Results,
        int TotalCount,
        int SuccessCount,
        int FailureCount
    );

    /// <summary>
    /// Pipeline statistics
    /// </summary>
    public record InferencePipelineStats(
        long TotalRequests,
        long CompletedRequests,
        long FailedRequests,
        int PendingRequests,
        int ActiveRequests,
        double AverageLatencyMs,
        double P95LatencyMs,
        double P99LatencyMs,
        Dictionary<string, long> RequestsByModel
    );
}