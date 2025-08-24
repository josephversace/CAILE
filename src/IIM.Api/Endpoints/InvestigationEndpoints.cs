using IIM.Application.Commands.Investigation;
using IIM.Core.Mediator;
using IIM.Shared.Models;
using IIM.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints;

/// <summary>
/// Investigation session management endpoints using Mediator pattern
/// </summary>
public static class InvestigationEndpoints
{
    /// <summary>
    /// Maps investigation-related endpoints for session management and querying
    /// </summary>
    public static void MapInvestigationEndpoints(this IEndpointRouteBuilder app)
    {
        var investigation = app.MapGroup("/api/investigation")
            .WithTags("Investigation")
            .WithOpenApi();

        // ========================================
        // SESSION MANAGEMENT
        // ========================================

        // Create investigation session
        investigation.MapPost("/session", async (
            [FromBody] CreateSessionRequest request,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            // Convert CreateSessionRequest to CreateSessionCommand
            var command = new CreateSessionCommand(
                request.CaseId,
                request.Title,
                request.InvestigationType);

            var session = await mediator.Send(command, ct);
            return Results.Created($"/api/investigation/session/{session.Id}", session);
        })
        .WithName("CreateInvestigationSession")
        .WithSummary("Create a new investigation session")
        .Produces<InvestigationSession>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Get session by ID
        investigation.MapGet("/session/{sessionId}", async (
            string sessionId,
            [FromQuery] bool includeMessages = true,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetSessionCommand(sessionId, includeMessages);
            var session = await mediator.Send(query, ct);
            return session != null ? Results.Ok(session) : Results.NotFound();
        })
        .WithName("GetInvestigationSession")
        .WithSummary("Get investigation session by ID")
        .Produces<InvestigationSession>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Delete session
        investigation.MapDelete("/session/{sessionId}", async (
            string sessionId,
            [FromQuery] string? reason,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new DeleteSessionCommand(sessionId, reason, archiveOnly: false);
            var deleted = await mediator.Send(command, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteInvestigationSession")
        .WithSummary("Delete an investigation session")
        .RequireAuthorization()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ========================================
        // QUERY PROCESSING
        // ========================================

        // Process investigation query - accepts command directly
        investigation.MapPost("/query", async (
            [FromBody] ProcessInvestigationCommand command,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            // Add user context if available
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                command.UserId = httpContext.User.Identity.Name;
            }

            var response = await mediator.Send(command, ct);
            return Results.Ok(response);
        })
        .WithName("ProcessInvestigationQuery")
        .WithSummary("Process an investigation query with RAG")
        .Produces<InvestigationResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Alternative: Process query with separate parameters
        investigation.MapPost("/session/{sessionId}/query", async (
            string sessionId,
            [FromBody] InvestigationQuery query,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new ProcessInvestigationCommand(sessionId, query.Text)
            {
                Attachments = query.Attachments,
                UserId = httpContext.User?.Identity?.Name
            };

            var response = await mediator.Send(command, ct);
            return Results.Ok(response);
        })
        .WithName("ProcessSessionQuery")
        .WithSummary("Process a query within a specific session")
        .Produces<InvestigationResponse>();

        // ========================================
        // TOOL EXECUTION
        // ========================================

        // Execute tool within session
        investigation.MapPost("/session/{sessionId}/tool", async (
            string sessionId,
            [FromBody] ToolExecutionRequest request,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new ExecuteToolCommand(
                sessionId,
                request.ToolName,
                request.Parameters);

            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ExecuteTool")
        .WithSummary("Execute a specific tool within a session")
        .Produces<ToolResult>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ========================================
        // CASE-RELATED QUERIES
        // ========================================

        // Get sessions by case
        investigation.MapGet("/case/{caseId}/sessions", async (
            string caseId,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetSessionsByCaseCommand(caseId);
            var sessions = await mediator.Send(query, ct);
            return Results.Ok(sessions);
        })
        .WithName("GetCaseSessions")
        .WithSummary("Get all investigation sessions for a case")
        .Produces<List<InvestigationSession>>();

        // ========================================
        // EXPORT & REPORTING
        // ========================================

        // Export investigation results
        investigation.MapPost("/session/{sessionId}/export", async (
            string sessionId,
            [FromQuery] ExportFormat format = ExportFormat.PDF,
            [FromBody] ExportOptions? options,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ExportInvestigationCommand(sessionId, format)
            {
                Options = options
            };

            var exportData = await mediator.Send(command, ct);

            var contentType = format switch
            {
                ExportFormat.PDF => "application/pdf",
                ExportFormat.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.JSON => "application/json",
                _ => "application/octet-stream"
            };

            return Results.File(exportData, contentType, $"investigation_{sessionId}.{format.ToString().ToLower()}");
        })
        .WithName("ExportInvestigation")
        .WithSummary("Export investigation results")
        .RequireAuthorization()
        .Produces(StatusCodes.Status200OK);

        // ========================================
        // REAL-TIME UPDATES (SignalR Integration)
        // ========================================

        // Subscribe to session updates
        investigation.MapPost("/session/{sessionId}/subscribe", async (
            string sessionId,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            // This would typically connect to SignalR hub
            // For now, just acknowledge subscription
            return Results.Ok(new { message = "Subscribed to session updates", sessionId });
        })
        .WithName("SubscribeToSession")
        .WithSummary("Subscribe to real-time session updates");
    }
}
