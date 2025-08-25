
using IIM.Application.Case;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

/// <summary>
/// Case management endpoints for investigation cases
/// </summary>
public static class CaseEndpoints
{
    /// <summary>
    /// Maps all case-related endpoints for CRUD operations and management
    /// </summary>
    public static void MapCaseEndpoints(this IEndpointRouteBuilder app)
    {
        var cases = app.MapGroup("/api/cases")
            .WithTags("Cases")
            .WithOpenApi();

        // ========================================
        // CASE CRUD OPERATIONS
        // ========================================

        // Create case
        cases.MapPost("/", async (
            [FromBody] CreateCaseRequest request,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new CreateCaseCommand
            {
                CaseNumber = request.CaseNumber,
                Name = request.Name,
                Type = request.Type,
                Description = request.Description,
                LeadInvestigator = request.LeadInvestigator ?? httpContext.User?.Identity?.Name ?? "Unknown",
                TeamMembers = request.TeamMembers,
                Classification = request.Classification,
                Metadata = request.Metadata
            };

            var caseEntity = await mediator.Send(command, ct);
            return Results.Created($"/api/cases/{caseEntity.Id}", caseEntity);
        })
        .WithName("CreateCase")
        .WithSummary("Create a new investigation case")
        .Produces<Case>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Get case by ID
        cases.MapGet("/{caseId}", async (
            string caseId,
            [FromServices] IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool includeEvidence = false,
            [FromQuery] bool includeSessions = false,
            [FromQuery] bool includeReports = false,
            [FromQuery] bool includeStatistics = true) =>
        {
            var query = new GetCaseCommand(
                caseId,
                includeEvidence,
                includeSessions,
                includeReports,
                includeStatistics);

            var caseEntity = await mediator.Send(query, ct);
            return caseEntity != null
                ? Results.Ok(caseEntity)
                : Results.NotFound(new { error = $"Case {caseId} not found" });
        })
        .WithName("GetCase")
        .WithSummary("Get case details by ID")
        .Produces<Case>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Update case
        cases.MapPut("/{caseId}", async (
            string caseId,
            [FromBody] UpdateCaseRequest request,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new UpdateCaseCommand
            {
                CaseId = caseId,
                Name = request.Name,
                Description = request.Description,
                Status = request.Status,
                LeadInvestigator = request.LeadInvestigator,
                TeamMembers = request.TeamMembers,
                Classification = request.Classification,
                Metadata = request.Metadata
            };

            var updated = await mediator.Send(command, ct);
            return updated
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Case {caseId} not found" });
        })
        .WithName("UpdateCase")
        .WithSummary("Update case details")
        .RequireAuthorization()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Delete case
        cases.MapDelete("/{caseId}", async (
            string caseId,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct,
            [FromQuery] string? reason = null,
            [FromQuery] bool archiveOnly = true) =>
        {
            var command = new DeleteCaseCommand(caseId, reason, archiveOnly);
            var deleted = await mediator.Send(command, ct);

            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Case {caseId} not found" });
        })
        .WithName("DeleteCase")
        .WithSummary("Delete or archive a case")
        .RequireAuthorization()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ========================================
        // CASE QUERIES
        // ========================================

        // Search cases
        cases.MapPost("/search", async (
            [FromBody] SearchCaseRequest request,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new SearchCasesCommand
            {
                SearchTerm = request.SearchTerm,
                CaseNumbers = request.CaseNumbers,
                Statuses = request.Statuses,
                CreatedAfter = request.CreatedAfter,
                CreatedBefore = request.CreatedBefore,
                Page = request.Page,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending
            };

            var results = await mediator.Send(command, ct);
            return Results.Ok(results);
        })
        .WithName("SearchCases")
        .WithSummary("Search cases with filters")
        .Produces<CaseListResponse>();

        // Get recent cases
        cases.MapGet("/recent", async (
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct,
            [FromQuery] int count = 10) =>
        {
            var query = new GetRecentCasesCommand(count, httpContext.User?.Identity?.Name);
            var recentCases = await mediator.Send(query, ct);
            return Results.Ok(recentCases);
        })
        .WithName("GetRecentCases")
        .WithSummary("Get most recently updated cases")
        .Produces<List<Case>>();

        // Get case statistics
        cases.MapGet("/{caseId}/statistics", async (
            string caseId,
            [FromServices] IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool includeEvidenceStats = true,
            [FromQuery] bool includeSessionStats = true) =>
        {
            var query = new GetCaseStatisticsCommand
            {
                CaseId = caseId,
                IncludeEvidenceStats = includeEvidenceStats,
                IncludeSessionStats = includeSessionStats
            };

            var stats = await mediator.Send(query, ct);
            return Results.Ok(stats);
        })
        .WithName("GetCaseStatistics")
        .WithSummary("Get detailed statistics for a case")
        .Produces<CaseStatistics>();

        // ========================================
        // CASE TIMELINE
        // ========================================

        // Get case timeline
        cases.MapGet("/{caseId}/timeline", async (
            string caseId,
            [FromServices] ICaseManager caseManager,
            CancellationToken ct,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null) =>
        {
            var events = await caseManager.GetCaseTimelineAsync(caseId, ct);

            // Filter by date range if provided
            if (startDate.HasValue)
                events = events.Where(e => e.Timestamp >= startDate.Value).ToList();
            if (endDate.HasValue)
                events = events.Where(e => e.Timestamp <= endDate.Value).ToList();

            return Results.Ok(new
            {
                CaseId = caseId,
                Events = events.OrderBy(e => e.Timestamp),
                TotalEvents = events.Count,
                StartDate = events.Min(e => e.Timestamp),
                EndDate = events.Max(e => e.Timestamp)
            });
        })
        .WithName("GetCaseTimeline")
        .WithSummary("Get timeline of events for a case")
        .Produces<object>();

        // ========================================
        // CASE RELATIONSHIPS
        // ========================================

        // Link session to case
        cases.MapPost("/{caseId}/sessions/{sessionId}", async (
            string caseId,
            string sessionId,
            [FromServices] ICaseManager caseManager,
            CancellationToken ct) =>
        {
            var linked = await caseManager.LinkSessionToCaseAsync(sessionId, caseId, ct);
            return linked
                ? Results.Ok(new { message = "Session linked to case successfully" })
                : Results.Problem("Failed to link session to case");
        })
        .WithName("LinkSessionToCase")
        .WithSummary("Link an investigation session to a case")
        .RequireAuthorization()
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Link evidence to case
        cases.MapPost("/{caseId}/evidence/{evidenceId}", async (
            string caseId,
            string evidenceId,
            [FromServices] ICaseManager caseManager,
            CancellationToken ct) =>
        {
            var linked = await caseManager.LinkEvidenceToCaseAsync(evidenceId, caseId, ct);
            return linked
                ? Results.Ok(new { message = "Evidence linked to case successfully" })
                : Results.Problem("Failed to link evidence to case");
        })
        .WithName("LinkEvidenceToCase")
        .WithSummary("Link evidence to a case")
        .RequireAuthorization()
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // ========================================
        // CASE EXPORT
        // ========================================

        // Export case
        cases.MapPost("/{caseId}/export", async (
            string caseId,
            [FromServices] IExportService exportService,
            [FromServices] ICaseManager caseManager,
            CancellationToken ct,
            [FromQuery] ExportFormat format = ExportFormat.Pdf,
            [FromBody] ExportOptions? options = null) =>
        {
            var caseEntity = await caseManager.GetCaseAsync(caseId, ct);
            if (caseEntity == null)
            {
                return Results.NotFound(new { error = $"Case {caseId} not found" });
            }

            var exportResult = await exportService.ExportCaseAsync(caseEntity, format, options);

            var contentType = format switch
            {
                ExportFormat.Pdf => "application/pdf",
                ExportFormat.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.Json => "application/json",
                _ => "application/octet-stream"
            };

            return Results.File(
                exportResult.Data ?? Array.Empty<byte>(),
                contentType,
                $"case_{caseId}_{DateTimeOffset.UtcNow:yyyyMMdd}.{format.ToString().ToLower()}");
        })
        .WithName("ExportCase")
        .WithSummary("Export case data in various formats")
        .RequireAuthorization()
        .Produces<byte[]>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ========================================
        // BATCH OPERATIONS
        // ========================================

        // Batch update cases
        cases.MapPost("/batch/update", async (
            [FromBody] BatchUpdateCasesRequest request,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var results = new List<object>();

            foreach (var caseId in request.CaseIds)
            {
                try
                {
                    var command = new UpdateCaseCommand
                    {
                        CaseId = caseId,
                        Status = request.Status,
                        Priority = request.Priority,
                        Classification = request.Classification,
                        Metadata = request.Metadata
                    };

                    var updated = await mediator.Send(command, ct);
                    results.Add(new { caseId, success = updated });
                }
                catch (Exception ex)
                {
                    results.Add(new { caseId, success = false, error = ex.Message });
                }
            }

            return Results.Ok(results);
        })
        .WithName("BatchUpdateCases")
        .WithSummary("Update multiple cases in batch")
        .RequireAuthorization()
        .Produces<List<object>>();

        // Get user's cases
        cases.MapGet("/user/{userId}", async (
            string userId,
            [FromServices] ICaseManager caseManager,
            CancellationToken ct) =>
        {
            var userCases = await caseManager.GetUserCasesAsync(userId, ct);
            return Results.Ok(userCases);
        })
        .WithName("GetUserCases")
        .WithSummary("Get all cases for a specific user")
        .Produces<List<Case>>();

        // Get case summary
        cases.MapGet("/{caseId}/summary", async (
            string caseId,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetCaseCommand(caseId, false, false, false, true);
            var caseEntity = await mediator.Send(query, ct);

            if (caseEntity == null)
            {
                return Results.NotFound(new { error = $"Case {caseId} not found" });
            }

            var summary = new CaseSummary
            {
                Id = caseEntity.Id,
                CaseNumber = caseEntity.CaseNumber,
                Name = caseEntity.Title,
                Type = caseEntity.Type.ToString(),
                Status = caseEntity.Status.ToString(),
                Classification = caseEntity.Classification,
                UpdatedAt = caseEntity.UpdatedAt,
                EvidenceCount = caseEntity.Evidence?.Count ?? 0,
                ActiveSessions = caseEntity.Sessions?.Count(s => s.Status == InvestigationStatus.Active) ?? 0
            };

            return Results.Ok(summary);
        })
        .WithName("GetCaseSummary")
        .WithSummary("Get summary information for a case")
        .Produces<CaseSummary>()
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}


