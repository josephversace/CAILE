
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs;

// Request DTOs
public record CreateSessionRequest(
    string CaseId,
    string Title,
    string InvestigationType,
    Dictionary<string, ModelConfigDto>? Models = null,
    List<string>? EnabledTools = null,
    SessionContextDto? Context = null
);

public record InvestigationQuery(
    string SessionId,
    string Text,
    List<AttachmentDto>? Attachments = null,
    Dictionary<string, object>? Parameters = null,
    List<string>? RequestedTools = null
);

public record SessionContextDto(
    string CaseId,
    List<string>? RelevantEvidenceIds = null,
    Dictionary<string, object>? Variables = null,
    TimeRangeDto? FocusTimeRange = null
);

public record ModelConfigDto(
    string ModelId,
    string Provider,
    string Type,
    Dictionary<string, object>? Parameters = null
);

public record AttachmentDto(
    string FileName,
    string ContentType,
    long Size,
    string Type,
    string? Base64Content = null,
    string? Url = null
);

// Response DTOs
public record InvestigationResponse(
    string Id,
    string SessionId,
    string QueryId,
    string Message,
    RAGSearchResultDto? RAGResults,
    List<TranscriptionResultDto>? Transcriptions,
    List<ImageAnalysisResultDto>? ImageAnalyses,
    List<ToolResultDto> ToolResults,
    List<CitationDto> Citations,
    List<string> EvidenceIds,
    List<string> EntityIds,
    double Confidence,
    string? FineTuneJobId,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null
);

public record SessionResponseDto(
    string Id,
    string CaseId,
    string UserId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    int MessageCount,
    List<string> EnabledTools,
    Dictionary<string, ModelConfigDto> Models,
    List<FindingDto>? Findings,
    Dictionary<string, object>? Metrics
);

public record InvestigationMessage(
    string Id,
    string Role,
    string Content,
    List<AttachmentDto>? Attachments,
    List<ToolResultDto>? ToolResults,
    List<CitationDto>? Citations,
    DateTimeOffset Timestamp,
    string? ModelUsed,
    Dictionary<string, object>? Metadata
);

public record TranscriptionResultDto(
     string Id,
     string EvidenceId,
     string Text,
     string Language,
     double Confidence,
     List<TranscriptionSegmentDto> Segments,
     Dictionary<string, object>? Metadata,
     // Add missing properties
     TimeSpan? Duration = null,
     int? AudioFileId = null
 );

public record TranscriptionSegmentDto(
    int Start,
    int End,
    string Text,
    double Confidence,
    string? Speaker
);

public record ImageAnalysisResultDto(
    string Id,
    string EvidenceId,
    List<DetectedObjectDto> Objects,
    List<DetectedFaceDto> Faces,
    List<string> Tags,
    List<SimilarImageDto> SimilarImages,
    Dictionary<string, object>? Metadata
);

public record DetectedObjectDto(
    string Label,
    double Confidence,
    BoundingBoxDto BoundingBox
);

public record DetectedFaceDto(
    string Id,
    BoundingBoxDto BoundingBox,
    Dictionary<string, double>? Emotions,
    int? Age,
    string? Gender
);

public record BoundingBoxDto(
    int X,
    int Y,
    int Width,
    int Height
);

public record SimilarImageDto(
    string EvidenceId,
    string FileName,
    double Similarity
);

public record ToolResultDto(
    string Id,
    string ToolName,
    string Status,
    object? Data,
    List<VisualizationDto>? Visualizations = null,
    List<string>? Recommendations = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan ExecutionTime = default,
    string? ErrorMessage = null
);

public record CitationDto(
    string Id,
    string SourceId,
    string SourceType,
    string Text,
    int? PageNumber,
    string? Location,
    double Relevance
);

public record VisualizationDto(
    string Id,
    string Type,
    string? Title,
    string? Description,
    object Data,
    Dictionary<string, object>? Options = null,
    string? RenderFormat = null
);
