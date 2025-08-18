
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs;
// Request DTOs
public record GenerateReportRequest(
    string CaseId,
    string ReportType,
    string? Title = null,
    List<string>? SessionIds = null,
    List<string>? EvidenceIds = null,
    TimeRangeDto? DateRange = null,
    Dictionary<string, object>? Options = null
);

public record ReportSectionRequest(
    string Title,
    string Content,
    int Order,
    List<string>? EvidenceReferences = null,
    List<VisualizationDto>? Visualizations = null
);

// Response DTOs
public record ReportResponseDto(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    string Content,
    List<ReportSectionDto> Sections,
    List<string> EvidenceIds,
    List<FindingDto> Findings,
    List<RecommendationDto> Recommendations,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt,
    string? SubmittedTo,
    Dictionary<string, object>? Metadata
);

public record ReportSectionDto(
    string Id,
    string Title,
    string Content,
    int Order,
    List<string> EvidenceReferences,
    List<VisualizationDto>? Visualizations
);

public record FindingDto(
    string Id,
    string Title,
    string Description,
    string Severity,
    double Confidence,
    List<string> SupportingEvidenceIds,
    List<string>? RelatedEntityIds,
    DateTimeOffset DiscoveredAt
);

public record RecommendationDto(
    string Id,
    string Title,
    string Description,
    string Priority,
    string Rationale,
    List<string> RelatedFindingIds
);

public record ReportListResponse(
    List<ReportSummaryDto> Reports,
    int TotalCount,
    int Page,
    int PageSize
);

public record ReportSummaryDto(
    string Id,
    string CaseId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SubmittedAt
);
