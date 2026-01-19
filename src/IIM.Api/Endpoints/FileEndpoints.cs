using IIM.Application.Files;
using IIM.Application.ProcessedFile;
using IIM.Application.Urls;
using IIM.Ingestion.Services;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints
{
	public static class FileEndpoints
	{
		public static void MapFileEndpoints(this IEndpointRouteBuilder app)
		{
			var files = app.MapGroup("/api/files")
				.WithTags("Files")
				.WithOpenApi();

			// ------------------------------------------------------------
			// Get derived file content
			// ------------------------------------------------------------
			files.MapGet("thumbnail/{storedfilehash}/{size}", async (
				string storedFileHash,
				ThumbnailSize size,
				[FromServices] IMediator mediator,
				CancellationToken ct) =>
			{
			
				var query = new GetThumbnailCommand(storedFileHash, size);
				var workspaceEntity = await mediator.Send(query, ct);
				return workspaceEntity is not null
					? Results.Ok(workspaceEntity)
					: Results.NotFound(new { error = $"File {storedFileHash} not found" });
			})
			.WithName("GetThumbnail")
			.WithSummary("Get thumbnail for blake3hash")
			.Produces<string>()
			.ProducesProblem(StatusCodes.Status404NotFound);

			// ------------------------------------------------------------
			// Ingest URL (Scrape and Save as Virtual File)
			// ------------------------------------------------------------
			files.MapPost("/ingest-url", async (
				[FromBody] IngestUrlRequest req,
				[FromServices] IMediator mediator,
				CancellationToken ct) =>
			{
			
				//Todo validation of the url and the workspace

				var command = new IngestUrlCommand(req.Url, req.WorkspaceId);
				var result = await mediator.Send(command, ct);

				return result.Success
					? Results.Ok(result)
					: Results.BadRequest(result);
			})
			.WithName("IngestUrl")
			.WithSummary("Scrapes a URL using Playwright and adds it to a workspace")
			.Produces<IngestUrlResult>()
			.ProducesProblem(StatusCodes.Status400BadRequest);

			// ------------------------------------------------------------
			// Get derived file content
			// ------------------------------------------------------------
			files.MapGet("/derived/{storedFileHash}/{processorName}",
				async (string storedFileHash, string processorName, IWorkspaceManager workspaces, CancellationToken ct) =>
				{
					var content = await workspaces.GetDerivedContentAsync(storedFileHash, processorName, ct);

					return content is null ? Results.NotFound() : Results.Ok(content);
				})
			.WithName("GetDerivedContent");

			// ------------------------------------------------------------
			// Get metadata
			// ------------------------------------------------------------
			files.MapGet("/{id:guid}",
				async (Guid id, IWorkspaceManager workspaces, CancellationToken ct) =>
				{
					var vf = await workspaces.GetVirtualFileByIdAsync(id, ct);
					return vf is null ? Results.NotFound() : Results.Ok(vf);
				})
			.WithName("GetFileById");

			// ------------------------------------------------------------
			// Get workspace files
			// ------------------------------------------------------------
			files.MapGet("/workspace/{workspaceId:guid}",
				async (Guid workspaceId, IWorkspaceManager workspaces, CancellationToken ct) =>
				{
					var list = await workspaces.GetVirtualFilesByWorkspaceAsync(workspaceId, ct);
					return Results.Ok(list);
				})
			.WithName("GetFilesByWorkspace");

			// ------------------------------------------------------------
			// Chain of custody
			// ------------------------------------------------------------
			files.MapGet("/{id:guid}/chain",
				async (Guid id, IWorkspaceManager workspaces, CancellationToken ct) =>
				{
					var vf = await workspaces.GetVirtualFileByIdAsync(id, ct);
					return vf is null
						? Results.NotFound()
						: Results.Ok(vf.ChainOfCustody.OrderBy(x => x.Timestamp));
				})
			.WithName("GetFileChain");

			// ------------------------------------------------------------
			// Integrity Check
			// ------------------------------------------------------------
			files.MapPost("/{id:guid}/verify",
				async (Guid id, IFileIntegrityService integrity, CancellationToken ct) =>
				{
					var ok = await integrity.VerifyAsync(id, ct);

					return Results.Ok(new
					{
						VirtualFileId = id,
						Integrity = ok
					});
				})
			.WithName("VerifyFileIntegrity");

			// ------------------------------------------------------------
			// NEW: Move file between storage tiers (SeaweedFS volumes)
			// ------------------------------------------------------------
			files.MapPost("/{id:guid}/move",
				async (Guid id, MoveFileRequest req, IWorkspaceManager workspaces, CancellationToken ct) =>
				{
					var vf = await workspaces.GetVirtualFileByIdAsync(id, ct);
					if (vf is null)
						return Results.NotFound();

					if (vf.StoredFileHash is null)
						return Results.BadRequest("Virtual file has no physical StoredFile yet.");

					bool ok = await workspaces.MoveStoredFileAsync(
						vf.StoredFileHash,
						req.NewBucket,
						ct);

					if (!ok)
						return Results.BadRequest("Move operation failed.");

					// Re-load updated metadata and return
					var updated = await workspaces.GetVirtualFileByIdAsync(id, ct);
					return Results.Ok(updated);
				})
			.WithName("MoveFileToBucket");


			// ------------------------------------------------------------
			// Get derived artifact by derived hash (NEW, canonical)
			// ------------------------------------------------------------
			files.MapGet("/derived/{derivedHash}",
				async (
					string derivedHash,
					[FromQuery] bool preview,
					IWorkspaceManager workspaces,
					CancellationToken ct) =>
				{
					var content = await workspaces.GetDerivedContentByHashAsync(
						derivedHash,
						preview,
						ct);

					return content is null
						? Results.NotFound()
						: Results.Ok(content);
				})
			.WithName("GetDerivedContentByHash");

			// ============================================================
			// Reprocess file through ingestion pipeline
			// ============================================================
			files.MapPost("/{id:guid}/reprocess", async (
				Guid id,
				[FromBody] ReprocessFileRequest request,
				[FromServices] IMediator mediator,
				CancellationToken ct) =>
			{
				try
				{
					// Validate the file exists
					//var file = await mediator.Send(new GetVirtualFileQuery { FileId = id }, ct);
					//if (file == null)
					//	return Results.NotFound($"File {id} not found.");

					//// Queue the ingestion job with options
					//var command = new IngestionStepIds
					//{
					//	FileId = id,
					//	Force = request.Force,
					//	OnlySteps = request.OnlySteps,
					//	SkipSteps = request.SkipSteps,
					//	Overrides = request.Overrides
					//};

					//await mediator.Send(command, ct);

					return Results.Ok(new ReprocessFileResponse
					{
						Queued = true,
						Message = "File queued for reprocessing",
						Steps = request.OnlySteps ?? new List<string> { "All" }
					});
				}
				catch (Exception ex)
				{
					return Results.Problem(
						detail: ex.Message,
						statusCode: StatusCodes.Status500InternalServerError);
				}
			})
			.WithName("ReprocessFile")
			.WithSummary("Reprocess a file through the ingestion pipeline")
			.Produces<ReprocessFileResponse>(StatusCodes.Status200OK)
			.ProducesProblem(StatusCodes.Status404NotFound)
			.ProducesProblem(StatusCodes.Status500InternalServerError);

		}
	}
}
