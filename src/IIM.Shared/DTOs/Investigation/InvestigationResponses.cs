using IIM.Shared.DTOs;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Response DTO for investigation session
    /// </summary>
    public record InvestigationSessionResponse(
        string Id,
        string CaseId,
        string Title,
        string Icon,
        string Type,
        string Status,
        List<string> EnabledTools,
        Dictionary<string, ModelConfiguration> Models,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string CreatedBy,
        int MessageCount,
        List<FindingSummary>? Findings
    );

    /// <summary>
    /// Response DTO for investigation query results
    /// </summary>
    public record InvestigationResponse(
        string Id,
        string Message,
        List<ToolResult>? ToolResults,
        List<Citation>? Citations,
        List<string>? RelatedEvidenceIds,
        double? Confidence,
        string DisplayType,
        Dictionary<string, object>? Metadata,
        Dictionary<string, object>? DisplayMetadata,
        List<Visualization>? Visualizations
    );

    /// <summary>
    /// Investigation message for conversation history
    /// </summary>
    public record InvestigationMessageResponse(
        string Id,
        string Role,
        string Content,
        List<AttachmentInfo>? Attachments,
        List<ToolResult>? ToolResults,
        List<Citation>? Citations,
        DateTimeOffset Timestamp,
        string? ModelUsed,
        Dictionary<string, object>? Metadata
    );

    /// <summary>
    /// Finding summary for investigations
    /// </summary>
    public record FindingSummary(
        string Id,
        string Title,
        string Description,
        string Severity,
        double Confidence,
        DateTimeOffset DiscoveredAt
    );

    /// <summary>
    /// Response DTO for session list
    /// </summary>
    public record SessionListResponse(
        List<InvestigationSessionResponse> Sessions,
        int TotalCount,
        int Page,
        int PageSize
    );
}