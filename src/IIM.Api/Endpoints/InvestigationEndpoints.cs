using IIM.Application.Commands.Investigation;
using IIM.Core.Mediator;
using IIM.Shared.DTOs;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;

namespace IIM.Api.Endpoints;

public static class InvestigationEndpoints
{
    public static void MapInvestigationEndpoints(this IEndpointRouteBuilder app)
    {
        var investigation = app.MapGroup("/api/investigation")
            .RequireAuthorization();

        // Create investigation session
        investigation.MapPost("/session", async (
            [FromServices] IMediator mediator,
            [FromBody] CreateSessionRequest request) =>
        {
            // Map DTO to existing Command structure
            var command = new CreateSessionCommand(
                request.CaseId,
                request.Title,
                request.Type  // This maps to investigationType parameter
            );

            // If you need to pass EnabledTools and Models, add them to metadata
            if (request.EnabledTools != null || request.Models != null)
            {
                command.Metadata["EnabledTools"] = request.EnabledTools ?? new List<string>();
                command.Metadata["Models"] = request.Models ?? new Dictionary<string, ModelConfiguration>();
            }

            var result = await mediator.Send(command);

            // Map result to DTO response
            var response = new InvestigationSessionResponse(
                Id: result.Id,
                CaseId: result.CaseId,
                Title: result.Title,
                Icon: result.Icon,
                Type: result.Type.ToString(),
                Status: result.Status.ToString(),
                EnabledTools: result.EnabledTools,
                Models: result.Models.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ModelConfiguration(
                        kvp.Value.ModelId,
                        kvp.Value.Provider,
                        kvp.Value.Settings,
                        kvp.Value.AutoLoad
                    )
                ),
                CreatedAt: result.CreatedAt,
                UpdatedAt: result.UpdatedAt,
                CreatedBy: result.CreatedBy,
                MessageCount: result.Messages.Count,
                Findings: result.Findings?.Select(f => new FindingSummary(
                    f.Id,
                    f.Title,
                    f.Description,
                    f.Severity.ToString(),
                    f.Confidence,
                    f.DiscoveredAt
                )).ToList()
            );

            return Results.Ok(response);
        })
        .WithName("CreateSession")
        .Produces<InvestigationSessionResponse>(200)
        .Produces<ErrorResponse>(400);

        // Process investigation query
        investigation.MapPost("/query", async (
            [FromServices] IMediator mediator,
            [FromBody] InvestigationQueryRequest request) =>
        {
            // Map DTO to existing Command structure
            var command = new ProcessInvestigationCommand(
                request.SessionId,
                request.Text  // Maps to Query property
            );

            // Map attachments if provided
            if (request.Attachments != null)
            {
                command.Attachments = request.Attachments.Select(a => new Attachment
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    MimeType = a.MimeType,
                    Size = a.Size,
                    StoragePath = a.StoragePath ?? string.Empty
                }).ToList();
            }

            // EnabledTools and Context from the request might need to be handled differently
            // Perhaps stored in session or passed through a different mechanism

            var result = await mediator.Send(command);

            // Return DTO response
            return Results.Ok(new InvestigationResponse(
                Id: result.Id,
                Message: result.Message,
                ToolResults: result.ToolResults,
                Citations: result.Citations,
                RelatedEvidenceIds: result.RelatedEvidenceIds,
                Confidence: result.Confidence,
                DisplayType: result.DisplayType.ToString(),
                Metadata: result.Metadata,
                DisplayMetadata: result.DisplayMetadata,
                Visualizations: result.Visualizations
            ));
        })
        .WithName("ProcessQuery")
        .Produces<InvestigationResponse>(200)
        .Produces<ErrorResponse>(400);

        // Get investigation sessions
        investigation.MapGet("/sessions", async (
            [FromServices] IMediator mediator,
            [FromQuery] string? caseId) =>
        {
            var query = new GetSessionsQuery { CaseId = caseId };
            var result = await mediator.Send(query);

            return Results.Ok(new SessionListResponse(
                Sessions: result,
                TotalCount: result.Count,
                Page: 1,
                PageSize: result.Count
            ));
        })
        .WithName("GetSessions")
        .Produces<SessionListResponse>(200);
    }
}