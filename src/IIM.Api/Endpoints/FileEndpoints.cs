using IIM.Application.Files;
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

		
		}
	}
}
