
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs;
// Request DTOs
public record CreateCaseRequest(
    string CaseNumber,
    string Name,
    string Type,
    string Description,
    string LeadInvestigator,
    List<string>? TeamMembers = null,
    string? Classification = null,
    Dictionary<string, object>? Metadata = null
);

public record UpdateCaseRequest(
    string? Name = null,
    string? Description = null,
    string? Status = null,
    string? LeadInvestigator = null,
    List<string>? TeamMembers = null,
    string? Classification = null,
    Dictionary<string, object>? Metadata = null
);

// Response DTOs
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

public record CaseListResponse(
    List<CaseSummaryDto> Cases,
    int TotalCount,
    int Page,
    int PageSize
);

public record CaseSummaryDto(
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
