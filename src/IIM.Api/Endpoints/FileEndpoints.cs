using IIM.Application.ManagedFiles;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Api.Endpoints
{
    public static class FileEndpoints
    {
        public static void MapFileEndpoints(this IEndpointRouteBuilder app)
        {
            var files = app.MapGroup("/api/files")
                .WithTags("Files")
                .WithOpenApi();

            // Direct file ingestion (e.g., from a web UI or simple client)
            files.MapPost("/ingest", async (
                HttpRequest request,
                [FromServices] IManagedFileManager fileManager,
                [FromQuery] Guid workspaceId,
                [FromQuery] string path,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { error = "Content-Type must be multipart/form-data" });
                }

                var form = await request.ReadFormAsync(ct);
                var file = form.Files.FirstOrDefault();

                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "No file provided in the form." });
                }

                using var stream = file.OpenReadStream();
                var virtualFile = new VirtualFile
                {
                    WorkspaceId = workspaceId,
                    FileName = file.FileName,
                    Path = path ?? "/",
                    FileSize = file.Length,
                    CreatedBy = httpContext.User?.Identity?.Name ?? "Anonymous",
                    CollectedBy = httpContext.User?.Identity?.Name ?? "Anonymous",
                    CollectionDate = DateTimeOffset.UtcNow,
                    CollectedLocation = "API Upload"
                };

                var createdFile = await fileManager.IngestFileAsync(stream, virtualFile, ct);

                return Results.Created($"/api/files/{createdFile.Id}", createdFile);
            })
            .WithName("IngestFile")
            .WithSummary("Directly ingest a file via multipart/form-data")
            .DisableAntiforgery();

            // Get file metadata by ID
            files.MapGet("/{fileId:guid}", async (
                Guid fileId,
                [FromServices] IWorkspaceProvider workspaceProvider,
                CancellationToken ct) =>
            {
                var file = await workspaceProvider.GetVirtualFileByIdAsync(fileId, ct);
                return file is not null ? Results.Ok(file) : Results.NotFound();
            })
            .WithName("GetFileById")
            .Produces<VirtualFile>()
            .Produces(StatusCodes.Status404NotFound);

            // Get all files for a specific workspace
            files.MapGet("/workspace/{workspaceId:guid}", async (
                Guid workspaceId,
                [FromServices] IWorkspaceProvider workspaceProvider,
                CancellationToken ct) =>
            {
                var fileList = await workspaceProvider.GetVirtualFilesByWorkspaceAsync(workspaceId, ct);
                return Results.Ok(fileList);
            })
            .WithName("GetFilesByWorkspace")
            .Produces<IEnumerable<VirtualFile>>();

            // Get the chain of custody for a file
            files.MapGet("/{fileId:guid}/chain-of-custody", async (
                Guid fileId,
                [FromServices] IWorkspaceProvider workspaceProvider,
                CancellationToken ct) =>
            {
                var file = await workspaceProvider.GetVirtualFileByIdAsync(fileId, ct);
                if (file == null)
                {
                    return Results.NotFound(new { error = $"File {fileId} not found" });
                }
                return Results.Ok(file.ChainOfCustody.OrderBy(c => c.Timestamp));
            })
            .WithName("GetChainOfCustody")
            .Produces<IEnumerable<ChainOfCustodyEntry>>()
            .Produces(StatusCodes.Status404NotFound);

            // Verify a file's integrity
            files.MapPost("/{fileId:guid}/verify", async (
                Guid fileId,
                [FromServices] IManagedFileManager fileManager,
                CancellationToken ct) =>
            {
                var isIntact = await fileManager.VerifyIntegrityAsync(fileId, ct);
                return Results.Ok(new { VirtualFileId = fileId, IntegrityValid = isIntact });
            })
            .WithName("VerifyFileIntegrity")
            .Produces<object>();

            // In FileEndpoints.cs
            files.MapPost("/request-upload", async (
                [FromBody] RequestUploadUrlCommand command,  // Take the command directly
                [FromServices] IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);
                return Results.Ok(result);
            })
            .WithName("RequestUpload")
            .RequireAuthorization();


        }
    }

}
