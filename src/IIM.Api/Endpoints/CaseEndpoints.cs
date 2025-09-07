
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
public static class WorkspaceEndpoints
{
    /// <summary>
    /// Maps all case-related endpoints for CRUD operations and management
    /// </summary>
    public static void MapCaseEndpoints(this IEndpointRouteBuilder app)
    {
        var cases = app.MapGroup("/api/workspaces")
            .WithTags("Workspaces")
            .WithOpenApi();

        // ========================================
        // CASE CRUD OPERATIONS
        // ========================================

        // Create case
        cases.MapPost("/", async (
            [FromBody] CreateWorspaceRequest request,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new CreateWorkspaceCommand
            {
                CaseNumber = request.CaseNumber,
                Name = request.Name,
                Type = request.Type,
                Description = request.Description,
                Owner = request.LeadInvestigator ?? httpContext.User?.Identity?.Name ?? "Unknown",
                TeamMembers = request.TeamMembers,
                Classification = request.Classification,
                Metadata = request.Metadata
            };

            var workspaceEntity = await mediator.Send(command, ct);
            return Results.Created($"/api/cases/{workspaceEntity.Id}", workspaceEntity);
        })
        .WithName("CreateWorkspace")
        .WithSummary("Create a new workspace")
        .Produces<Workspace>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Get case by ID
        cases.MapGet("/{workspaceId}", async (
            string workspaceId,
            [FromServices] IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool includeFiles = false,
            [FromQuery] bool includeSessions = false,
            [FromQuery] bool includeReports = false,
            [FromQuery] bool includeStatistics = true) =>
        {
            var query = new GetWorkspaceCommand(
                workspaceId,
                includeFiles,
                includeSessions,
                includeReports,
                includeStatistics);

            var workspaceEntity = await mediator.Send(query, ct);
            return workspaceEntity != null
                ? Results.Ok(workspaceEntity)
                : Results.NotFound(new { error = $"Workspace {workspaceId} not found" });
        })
        .WithName("GetCase")
        .WithSummary("Get workspace details by ID")
        .Produces<Workspace>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Update case
        cases.MapPut("/{workspaceId}", async (
            string workspaceId,
            [FromBody] UpdateWorkspaceRequest request,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new UpdateWorkspaceCommand
            {
                WorkspaceId = workspaceId,
                Name = request.Name,
                Description = request.Description,
                Status = request.Status,
                Owner = request.Owner,
                TeamMembers = request.TeamMembers,
                Classification = request.Classification,
                Metadata = request.Metadata
            };

            var updated = await mediator.Send(command, ct);
            return updated
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Workspace {workspaceId} not found" });
        })
        .WithName("UpdateWorkspace")
        .WithSummary("Update workspace details")
        .RequireAuthorization()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Delete case
        cases.MapDelete("/{workspaceId}", async (
            string workspaceId,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct,
            [FromQuery] string? reason = null,
            [FromQuery] bool archiveOnly = true) =>
        {
            var command = new DeleteWorkspaceCommand(workspaceId, reason, archiveOnly);
            var deleted = await mediator.Send(command, ct);

            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Workspace {workspaceId} not found" });
        })
        .WithName("DeleteWorkspace")
        .WithSummary("Delete or archive a workspace")
        .RequireAuthorization()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ========================================
        // Workspace QUERIES
        // ========================================

        // Search cases
        cases.MapPost("/search", async (
            [FromBody] SearchWorkspacesRequest request,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new SearchWorkspacesCommand
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
        .WithName("SearchWorkspaces")
        .WithSummary("Search workspaces with filters")
        .Produces<WorkspaceListResponse>();

        // Get recent cases
        cases.MapGet("/recent", async (
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct,
            [FromQuery] int count = 10) =>
        {
            var query = new GetRecentWorkspaceCommand(count, httpContext.User?.Identity?.Name);
            var recentCases = await mediator.Send(query, ct);
            return Results.Ok(recentCases);
        })
        .WithName("GetRecentWorkspaces")
        .WithSummary("Get most recently updated workspaces")
        .Produces<List<Workspace>>();

        // Get case statistics
        cases.MapGet("/{workspaceId}/statistics", async (
            string workspaceId,
            [FromServices] IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool includeEvidenceStats = true,
            [FromQuery] bool includeSessionStats = true) =>
        {
            var query = new GetWorkspaceStatisticsCommand
            {
                WorkspaceId = workspaceId,
                IncludeEvidenceStats = includeEvidenceStats,
                IncludeSessionStats = includeSessionStats
            };

            var stats = await mediator.Send(query, ct);
            return Results.Ok(stats);
        })
        .WithName("GetWorkspaceStatistics")
        .WithSummary("Get detailed statistics for a workspace")
        .Produces<WorkspaceStatistics>();

        // ========================================
        // CASE TIMELINE
        // ========================================

        // Get case timeline
        cases.MapGet("/{workspaceId}/timeline", async (
            string workspaceId,
            [FromServices] IWorkspaceManager caseManager,
            CancellationToken ct,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null) =>
        {
            var events = await caseManager.GetWorkspaceTimelineAsync(workspaceId, ct);

            // Filter by date range if provided
            if (startDate.HasValue)
                events = events.Where(e => e.Timestamp >= startDate.Value).ToList();
            if (endDate.HasValue)
                events = events.Where(e => e.Timestamp <= endDate.Value).ToList();

            return Results.Ok(new
            {
                CaseId = workspaceId,
                Events = events.OrderBy(e => e.Timestamp),
                TotalEvents = events.Count,
                StartDate = events.Min(e => e.Timestamp),
                EndDate = events.Max(e => e.Timestamp)
            });
        })
        .WithName("GetWorkspaceTimeline")
        .WithSummary("Get timeline of events for a case")
        .Produces<object>();

        // ========================================
        // CASE RELATIONSHIPS
        // ========================================

        // Link session to case
        cases.MapPost("/{workspaceId}/sessions/{sessionId}", async (
            string workspaceId,
            string sessionId,
            [FromServices] IWorkspaceManager caseManager,
            CancellationToken ct) =>
        {
            var linked = await caseManager.LinkSessionToWorkspaceAsync(sessionId, workspaceId, ct);
            return linked
                ? Results.Ok(new { message = "Session linked to case successfully" })
                : Results.Problem("Failed to link session to case");
        })
        .WithName("LinkSessionToWorkspace")
        .WithSummary("Link an ai session to a case")
        .RequireAuthorization()
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Link files to workspace
        cases.MapPost("/{workspaceId}/files/{fileId}", async (
            string workspaceId,
            string fileId,
            [FromServices] IWorkspaceManager caseManager,
            CancellationToken ct) =>
        {
            var linked = await caseManager.LinkFileToWorkspaceAsync(fileId, workspaceId, ct);
            return linked
                ? Results.Ok(new { message = "File linked to workspace successfully" })
                : Results.Problem("Failed to link file to workspace");
        })
        .WithName("LinkFileToWorkspace")
        .WithSummary("Link file to a workspace")
        .RequireAuthorization()
        .Produces<object>()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // ========================================
        // CASE EXPORT
        // ========================================

        // Export case
        cases.MapPost("/{workspaceId}/export", async (
            string workspaceId,
            [FromServices] IExportService exportService,
            [FromServices] IWorkspaceManager workspaceManager,
            CancellationToken ct,
            [FromQuery] ExportFormat format = ExportFormat.Pdf,
            [FromBody] ExportOptions? options = null) =>
        {
            var workspaceEntity = await workspaceManager.GetWorkspaceAsync(workspaceId, ct);
            if (workspaceEntity == null)
            {
                return Results.NotFound(new { error = $"Case {workspaceId} not found" });
            }

            var exportResult = await exportService.ExportWorkspaceAsync(workspaceEntity, format, options);

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
                $"case_{workspaceId}_{DateTimeOffset.UtcNow:yyyyMMdd}.{format.ToString().ToLower()}");
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
            [FromBody] BatchUpdateWorkspaceRequest request,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var results = new List<object>();

            foreach (var caseId in request.WorkspaceIds)
            {
                try
                {
                    var command = new UpdateWorkspaceCommand
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
        .WithName("BatchUpdateWorkspaces")
        .WithSummary("Update multiple workspaces in batch")
        .RequireAuthorization()
        .Produces<List<object>>();

        // Get user's cases
        cases.MapGet("/user/{userId}", async (
            string userId,
            [FromServices] IWorkspaceManager caseManager,
            CancellationToken ct) =>
        {
            var userCases = await caseManager.GetUserWorkspacesAsync(userId, ct);
            return Results.Ok(userCases);
        })
        .WithName("GetUserWorkspaces")
        .WithSummary("Get all workspaces for a specific user")
        .Produces<List<Workspace>>();

        // Get case summary
        cases.MapGet("/{workspaceId}/summary", async (
            string workspaceId,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetWorkspaceCommand(workspaceId, false, false, false, true);
            var workspaceEntity = await mediator.Send(query, ct);

            if (workspaceEntity == null)
            {
                return Results.NotFound(new { error = $"Workspace {workspaceId} not found" });
            }

            var summary = new WorkspaceSummary
            {
                Id = workspaceEntity.Id,
                CaseNumber = workspaceEntity.CaseNumber,
                Name = workspaceEntity.Title,
                Type = workspaceEntity.Type.ToString(),
                Status = workspaceEntity.Status.ToString(),
                Classification = workspaceEntity.Classification,
                UpdatedAt = workspaceEntity.UpdatedAt,
                FileCount = workspaceEntity.Files?.Count ?? 0,
                ActiveSessions = workspaceEntity.Sessions?.Count(s => s.Status == InvestigationStatus.Active) ?? 0
            };

            return Results.Ok(summary);
        })
        .WithName("GetCaseSummary")
        .WithSummary("Get summary information for a case")
        .Produces<WorkspaceSummary>()
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}


