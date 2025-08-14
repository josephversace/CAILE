namespace IIM.Api.DTOs;

// Request DTOs
public record ToolExecutionRequest(
    string ToolName,
    Dictionary<string, object> Parameters,
    string? CaseId = null,
    string? SessionId = null
);

public record OSINTRequest(
    string Target,
    List<string>? Sources = null,
    TimeRangeDto? DateRange = null,
    int Depth = 2
);

public record TimelineRequest(
    string CaseId,
    TimeRangeDto? DateRange = null,
    string Resolution = "hourly",
    bool IncludeInferences = true,
    bool HighlightAnomalies = true
);

public record NetworkAnalysisRequest(
    string CaseId,
    List<string>? EntityIds = null,
    int MaxDepth = 3,
    double MinConnectionStrength = 0.5
);

// Response DTOs
public record ToolExecutionResponseDto(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<VisualizationDto>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
);

public record OSINTResultDto(
    Dictionary<string, List<OSINTFinding>> Findings,
    NetworkGraphDto? NetworkGraph = null,
    string IntelligenceSummary = "",
    List<string> Recommendations = null!
);

public record OSINTFinding(
    string Source,
    string Type,
    string Content,
    DateTimeOffset? Timestamp,
    double Relevance,
    Dictionary<string, object>? Metadata
);

public record TimelineResponseDto(
    List<TimelineEventDto> Events,
    List<TimelinePatternDto> Patterns,
    List<TimelineAnomalyDto> Anomalies,
    List<CriticalPeriodDto> CriticalPeriods,
    object? VisualizationData
);

public record TimelineEventDto(
    string Id,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    string Type,
    string Importance,
    string? EvidenceId,
    List<string>? RelatedEntityIds,
    GeoLocationDto? Location,
    Dictionary<string, object>? Metadata
);

public record TimelinePatternDto(
    string Id,
    string Name,
    string Type,
    List<string> EventIds,
    double Confidence,
    string Description
);

public record TimelineAnomalyDto(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    double Severity,
    string Description,
    List<string>? AffectedEventIds
);

public record CriticalPeriodDto(
    DateTimeOffset Start,
    DateTimeOffset End,
    string Description,
    string Level,
    List<string>? EventIds
);

public record NetworkGraphDto(
    List<NodeDto> Nodes,
    List<EdgeDto> Edges,
    Dictionary<string, object>? Metadata
);

public record NodeDto(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties,
    double? X = null,
    double? Y = null
);

public record EdgeDto(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);
