using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Application.Artifacts;
using IIM.Application.Case;
using IIM.Application.Workspaces;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NPOI.OpenXmlFormats.Spreadsheet;
using IIM.Shared.Extensions;
using Org.BouncyCastle.Ocsp;
using IIM.Application;

namespace IIM.Api.Endpoints
{
    /// <summary>
    /// Workspace management endpoints.
    /// </summary>
    public static class WorkspaceEndpoints
    {
        /// <summary>
        /// Maps all workspace-related endpoints.
        /// </summary>
        public static void MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
        {
            var workspaces = app.MapGroup("/api/workspaces")
                .WithTags("Workspaces")
                .WithOpenApi();

            // Create workspace
            workspaces.MapPost("/", async (
                [FromBody] CreateWorkspaceRequest request,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var command = new CreateWorkspaceCommand
                {
                    Name = request.Name,
                    Description = request.Description,
                    Type = request.Type,
                    CreatedBy = request.OwnerId ?? httpContext.User?.Identity?.Name ?? "UnknownUser"
                };

                var workspaceEntity = await mediator.Send(command, ct);
                return Results.Created($"/api/workspaces/{workspaceEntity.Id}", workspaceEntity);
            })
            .WithName("CreateWorkspace")
            .WithSummary("Create a new workspace")
            .Produces<Workspace>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

            // Get workspace by ID
            workspaces.MapGet("/{workspaceId:guid}", async (
                Guid workspaceId,
                [FromServices] IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetWorkspaceQuery(workspaceId);
                var workspaceEntity = await mediator.Send(query, ct);
                return workspaceEntity is not null
                    ? Results.Ok(workspaceEntity)
                    : Results.NotFound(new { error = $"Workspace {workspaceId} not found" });
            })
            .WithName("GetWorkspace")
            .WithSummary("Get workspace details by ID")
            .Produces<Workspace>()
            .ProducesProblem(StatusCodes.Status404NotFound);

			workspaces.MapPut("{workspaceId:guid}", async (
		Guid workspaceId,
	UpdateWorkspaceRequest req,
	IMediator mediator,
	HttpContext ctx,
	CancellationToken ct) =>
			{
				var cmd = new UpdateWorkspaceCommand
				{
					WorkspaceId = workspaceId,
					Name = req.Name,
					Description = req.Description,
					Type = req.Type,
					IsPublic = req.IsPublic,
					OwnerId = req.OwnerId,
					UsersToAdd = req.UsersToAdd,
					UsersToUpdate = req.UsersToUpdate,
					UsersToRemove = req.UsersToRemove,
					UpdatedBy = ctx.User.GetUserIdString() ?? "system"
				};

				var ok = await mediator.Send(cmd, ct);
				return ok ? Results.Ok() : Results.NotFound();
			});


			// Delete workspace
			workspaces.MapDelete("/{workspaceId:guid}", async (
                Guid workspaceId,
                [FromServices] IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new DeleteWorkspaceCommand(workspaceId, "Deleted via API", true);
                await mediator.Send(command, ct);
                return Results.NoContent();
            })
            .WithName("DeleteWorkspace")
            .WithSummary("Delete or archive a workspace")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

            // Get recent workspaces
            workspaces.MapGet("/recent", async (
                [FromServices] IMediator mediator,
                CancellationToken ct,
                [FromQuery] int count = 10) =>
            {
                var query = new GetRecentWorkspacesQuery(count);
                var recentWorkspaces = await mediator.Send(query, ct);
                return Results.Ok(recentWorkspaces);
            })
            .WithName("GetRecentWorkspaces")
            .WithSummary("Get most recently updated workspaces")
            .Produces<IEnumerable<Workspace>>();

            // Get workspace timeline
            workspaces.MapGet("/{workspaceId:guid}/timeline", async (
                Guid workspaceId,
                [FromServices] IWorkspaceManager workspaceManager,
                CancellationToken ct) =>
            {
                var events = await workspaceManager.GetWorkspaceTimelineAsync(workspaceId, ct);
                return Results.Ok(events.OrderBy(e => e.Timestamp));
            })
            .WithName("GetWorkspaceTimeline")
            .WithSummary("Get timeline of events for a workspace")
            .Produces<IEnumerable<TimelineEvent>>();

            // Link session to workspace
            workspaces.MapPost("/{workspaceId:guid}/sessions/{sessionId:guid}", async (
                Guid workspaceId,
                Guid sessionId,
                [FromServices] IWorkspaceManager workspaceManager,
                CancellationToken ct) =>
            {
                var linked = await workspaceManager.LinkSessionToWorkspaceAsync(sessionId, workspaceId, ct);
                return linked
                    ? Results.Ok(new { message = "Session linked to workspace successfully" })
                    : Results.Problem("Failed to link session to workspace");
            })
            .WithName("LinkSessionToWorkspace")
            .WithSummary("Link an AI session to a workspace")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // Link files to workspace
            workspaces.MapPost("/{workspaceId:guid}/files/{fileId:guid}", async (
                Guid workspaceId,
                Guid fileId,
                [FromServices] IWorkspaceManager workspaceManager,
                CancellationToken ct) =>
            {
                var linked = await workspaceManager.LinkFileToWorkspaceAsync(fileId, workspaceId, ct);
                return linked
                    ? Results.Ok(new { message = "File linked to workspace successfully" })
                    : Results.Problem("Failed to link file to workspace");
            })
            .WithName("LinkFileToWorkspace")
            .WithSummary("Link a file to a workspace")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

			// ─────────────────────────────────────────────────────────────
			// ARTIFACTS (CRUD)
			// Route base: /api/workspaces/{workspaceId}/artifacts
			// ─────────────────────────────────────────────────────────────
			var artifacts = app.MapGroup("/api/workspaces/{workspaceId:guid}/artifacts")
				.WithTags("Artifacts")
				.WithOpenApi();

			// CREATE ARTIFACT
			artifacts.MapPost("/", async (
				Guid workspaceId,
				CreateArtifactDto dto,
				IMediator mediator,
				CancellationToken ct) =>
			{
				var artifact = new WorkspaceArtifact
				{
					Id = Guid.NewGuid(),
					WorkspaceId = workspaceId,
					Title = dto.Title,
					Summary = dto.Summary,
					Content = dto.Content,
					Tags = dto.Tags ?? new List<string>(),
					Type = dto.Type,
					CreatedUtc = DateTime.UtcNow,
					UpdatedUtc = DateTime.UtcNow,
					IsDeleted = false
				};

				var created = await mediator.Send(
					new CreateArtifactCommand { Artifact = artifact },
					ct
				);

				return Results.Created($"/api/workspaces/{workspaceId}/artifacts/{created.Id}", created);
			});

			// GET ALL ARTIFACTS FOR WORKSPACE
			artifacts.MapGet("/", async (
				Guid workspaceId,
				IMediator mediator,
				CancellationToken ct) =>
			{
				var items = await mediator.Send(
					new GetWorkspaceArtifactsQuery { WorkspaceId = workspaceId },
					ct
				);

				return Results.Ok(items);
			});

			// GET SINGLE ARTIFACT
			artifacts.MapGet("/{artifactId:guid}", async (
				Guid workspaceId,
				Guid artifactId,
				IMediator mediator,
				CancellationToken ct) =>
			{
				var result = await mediator.Send(
					new GetArtifactQuery { ArtifactId = artifactId },
					ct
				);

				return result is null
					? Results.NotFound()
					: Results.Ok(result);
			});

			// UPDATE ARTIFACT
			artifacts.MapPut("/{artifactId:guid}", async (
				Guid workspaceId,
				Guid artifactId,
				UpdateArtifactDto dto,
				IMediator mediator,
				CancellationToken ct) =>
			{
				var updatedArtifact = new WorkspaceArtifact
				{
					Id = artifactId,
					WorkspaceId = workspaceId,
					Title = dto.Title,
					Summary = dto.Summary,
					Content = dto.Content,
					Tags = dto.Tags ?? new List<string>(),
					Type = dto.Type,
					UpdatedUtc = DateTime.UtcNow
				};

				var ok = await mediator.Send(
					new UpdateArtifactCommand { Artifact = updatedArtifact },
					ct
				);

				return ok ? Results.Ok() : Results.NotFound();
			});

			// DELETE ARTIFACT
			artifacts.MapDelete("/{artifactId:guid}", async (
				Guid workspaceId,
				Guid artifactId,
				IMediator mediator,
				CancellationToken ct) =>
			{
				var ok = await mediator.Send(
					new DeleteArtifactCommand { ArtifactId = artifactId },
					ct
				);

				return ok ? Results.NoContent() : Results.NotFound();
			});

			// ─────────────────────────────────────────────────────────────
			// Derived
			// Route base: /api/workspaces/{workspaceId}/artifacts
			// ─────────────────────────────────────────────────────────────
			var derived = app.MapGroup("/api/workspaces/{workspaceId:guid}/derived")
				.WithTags("Derived")
				.WithOpenApi();

			// GET ALL ARTIFACTS FOR Virtualfile
			derived.MapGet("/{virtualfileid:guid}", async (
				Guid workspaceId,
				Guid virtualfileid,
				IMediator mediator,
				CancellationToken ct) =>
			{
				var items = await mediator.Send(
					new GetProcessedFilesByVirtualFileId(virtualfileid),
					ct
				);

				return Results.Ok(items);
			});


		}
	}
}

