using IIM.Application.Files;
using IIM.Shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace IIM.Api.Endpoints;

/// <summary>
/// Attachment upload endpoints.
/// Transport-only layer. All logic delegated to mediator.
/// </summary>
public static class AttachmentEndpoints
{
    public static void MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        var attachments = app.MapGroup("/api/attachments")
            .WithTags("Attachments")
            .WithOpenApi()
            .DisableAntiforgery();

        // ============================================================
        // Upload single attachment
        // ============================================================
        attachments.MapPost("/process", async (
            [FromForm] IFormFile file,
            [FromQuery] Guid workspaceId,
            [FromQuery] bool reprocess,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file provided.");

            if (workspaceId == Guid.Empty)
                return Results.BadRequest("workspaceId is required.");

            await using var stream = file.OpenReadStream();

            var result = await mediator.Send(
                new RegisterUploadedFileCommand
                {
                    WorkspaceId = workspaceId,
                    FileName = file.FileName,
                    MimeType = file.ContentType ?? "application/octet-stream",
                    FileSize = file.Length,
                    InputStream = stream,
                    Reprocess = reprocess
                },
                ct);

            return Results.Ok(new UploadAttachmentResponse
            {
                VirtualFileId = result.VirtualFileId,
                Blake3Hash = result.Blake3Hash,
                Deduplicated = result.Deduplicated
            });
        })
        .WithName("UploadAttachment")
        .WithSummary("Upload a file and enqueue ingestion")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<UploadAttachmentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);


        // ============================================================
        // Upload multiple attachments
        // ============================================================
        attachments.MapPost("/process-batch", async (
            [FromForm] IFormFileCollection files,
            [FromQuery] Guid workspaceId,
            [FromQuery] bool reprocess,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            if (files == null || files.Count == 0)
                return Results.BadRequest("No files provided.");

            if (workspaceId == Guid.Empty)
                return Results.BadRequest("workspaceId is required.");

            var results = new List<UploadAttachmentResponse>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                await using var stream = file.OpenReadStream();

                var result = await mediator.Send(
                    new RegisterUploadedFileCommand
                    {
                        WorkspaceId = workspaceId,
                        FileName = file.FileName,
                        MimeType = file.ContentType ?? "application/octet-stream",
                        FileSize = file.Length,
                        InputStream = stream,
                        Reprocess = reprocess
                    },
                    ct);

                results.Add(new UploadAttachmentResponse
                {
                    VirtualFileId = result.VirtualFileId,
                    Blake3Hash = result.Blake3Hash,
                    Deduplicated = result.Deduplicated
                });
            }

            return Results.Ok(new BatchUploadResponse { Results = results });
        })
        .WithName("UploadAttachmentBatch")
        .WithSummary("Upload multiple files and enqueue ingestion")
        .Accepts<IFormFileCollection>("multipart/form-data")
        .Produces<BatchUploadResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

public sealed class UploadAttachmentResponse
{
    public Guid VirtualFileId { get; init; }
    public string Blake3Hash { get; init; } = "";
    public bool Deduplicated { get; init; }
}

public sealed class BatchUploadResponse
{
    public List<UploadAttachmentResponse> Results { get; init; } = [];
}
