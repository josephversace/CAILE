using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Request DTO for creating a new case
    /// </summary>
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

    /// <summary>
    /// Request DTO for updating an existing case
    /// </summary>
    public record UpdateCaseRequest(
        string? Name = null,
        string? Description = null,
        string? Status = null,
        string? LeadInvestigator = null,
        List<string>? TeamMembers = null,
        string? Classification = null,
        Dictionary<string, object>? Metadata = null
    );

    /// <summary>
    /// Request DTO for case search/filter operations
    /// </summary>
    public record SearchCaseRequest(
        string? SearchTerm = null,
        string? Status = null,
        string? Type = null,
        string? Classification = null,
        DateTimeOffset? CreatedAfter = null,
        DateTimeOffset? CreatedBefore = null,
        int Page = 1,
        int PageSize = 20
    );
}