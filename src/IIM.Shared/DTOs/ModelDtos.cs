
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs;
// Request DTOs

public record ModelLoadRequest(
    string ModelId,
    string? ModelPath = null,
    string? Provider = null,
    Dictionary<string, object>? Options = null
);

public record ModelConfigurationRequest(
    string ModelId,
    Dictionary<string, object> Parameters
);

public record InferenceRequestDto(
    string ModelId,
    object Input,
    Dictionary<string, object>? Parameters = null,
    HashSet<string>? Tags = null,
    int Priority = 1,
    bool Stream = false


);




// Response DTOs
public record ModelInfoDto(
    string Id,
    string Name,
    string Type,
    string Provider,
    string Status,
    long MemoryUsage,
    string? LoadedPath,
    DateTimeOffset? LoadedAt,
    ModelCapabilitiesDto Capabilities,
    Dictionary<string, object>? Metadata = null
);

public record ModelCapabilitiesDto(
    int MaxContextLength,
    List<string> SupportedLanguages,
    List<string>? SpecialFeatures,
    bool SupportsStreaming,
    bool SupportsFineTuning,
    bool SupportsMultiModal,
    Dictionary<string, object>? CustomCapabilities = null
);

public record ModelListResponse(
    List<ModelInfoDto> Models,
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedCount,
    int AvailableCount
);



public record InferenceResponseDto(
    string ModelId,
    object Output,
    TimeSpan InferenceTime,
    int? TokensUsed = null,
    Dictionary<string, object>? Metadata = null
);



public record InferencePipelineStatsDto(
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

public record OrchestratorStatsDto(
    long TotalMemoryUsage,
    long AvailableMemory,
    int LoadedModels,
    Dictionary<string, ModelStatsDto> Models
);

public record ModelStatsDto(
    string ModelId,
    string Type,
    long MemoryUsage,
    int AccessCount,
    DateTimeOffset LastAccessed,
    TimeSpan AverageLatency,
    double AverageTokensPerSecond
);

public record FineTuneStatusDto(
    string JobId,
    string Status,
    double Progress,
    string? CurrentStep = null,
    TimeSpan? EstimatedTimeRemaining = null,
    Dictionary<string, double>? Metrics = null
);
