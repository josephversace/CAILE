using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Response DTO containing full case details
    /// </summary>
    public record CaseResponse(
        string Id,
        string CaseNumber,
        string Name,
        string Type,
        string Status,
        string Description,
        string LeadInvestigator,
        List<string> TeamMembers,
        string Classification,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        int EvidenceCount,
        int SessionCount,
        int ReportCount,
        Dictionary<string, object> Metadata
    );

    /// <summary>
    /// Response DTO for paginated case lists
    /// </summary>
    public record CaseListResponse(
        List<CaseSummary> Cases,
        int TotalCount,
        int Page,
        int PageSize
    );

    /// <summary>
    /// Summary DTO for case list items
    /// </summary>
    public record CaseSummary(
        string Id,
        string CaseNumber,
        string Name,
        string Type,
        string Status,
        string Classification,
        DateTimeOffset UpdatedAt,
        int EvidenceCount,
        int ActiveSessions
    );

    /// <summary>
    /// Response DTO for case statistics
    /// </summary>
    public record CaseStatistics(
        int TotalCases,
        int ActiveCases,
        int ClosedCases,
        int TotalEvidence,
        int TotalSessions,
        Dictionary<string, int> CasesByType,
        Dictionary<string, int> CasesByStatus
    );
}