
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Models;
using IIM.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using IIM.Shared.Interfaces;
using IIM.Application.Files;

namespace IIM.Api.Endpoints;

/// <summary>
/// Evidence management endpoints for handling digital evidence with chain of custody
/// </summary>
public static class FileEndpoints
{
    /// <summary>
    /// Maps all evidence-related endpoints for upload, retrieval, and management
    /// </summary>
    public static void MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var evidence = app.MapGroup("/api/files")
            .WithTags("ManagedFiles")
            .WithOpenApi();

        // ========================================
        // File UPLOAD WORKFLOW
        // ========================================

        // Initiate evidence upload - checks for duplicates and gets upload URL
        evidence.MapPost("/initiate-upload", async (
            [FromBody] InitiateFileUploadRequest request,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new InitiateFileUploadCommand
            {
                FileHash = request.FileHash,
                FileName = request.FileName,
                FileSize = request.FileSize,
                ContentType = request.ContentType,
                Metadata = request.Metadata,
                UserId = httpContext.User?.Identity?.Name ?? "Anonymous"
            };

            var response = await mediator.Send(command, ct);
            return Results.Ok(response);
        })
        .WithName("InitiateFileUpload")
        .WithSummary("Initiate file upload with deduplication check")
        .Produces<InitiateFileUploadResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Confirm evidence upload completion
        evidence.MapPost("/confirm-upload", async (
            [FromBody] ConfirmFileUploadRequest request,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ConfirmFileUploadCommand
            {
                EvidenceId = request.EvidenceId,
                ETag = request.ETag,
                ClientHash = request.ClientHash
            };

            var response = await mediator.Send(command, ct);
            return response.Success
                ? Results.Ok(response)
                : Results.BadRequest(response);
        })
        .WithName("ConfirmEvidenceUpload")
        .WithSummary("Confirm evidence upload and verify integrity")
        .Produces<ConfirmEvidenceUploadResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Direct evidence ingestion (for smaller files)
        evidence.MapPost("/ingest", async (
            HttpRequest request,
            [FromServices] IMediator mediator,
            [FromServices] IManagedFileManager evidenceManager,
            CancellationToken ct) =>
        {
            // Validate content length
            if (!request.ContentLength.HasValue || request.ContentLength.Value == 0)
            {
                return Results.BadRequest(new { error = "No file content provided" });
            }

            // Parse multipart form data
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Content type must be multipart/form-data" });
            }

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "No file provided" });
            }

            // Extract metadata from form
            var metadata = new FileMetadata
            {
                CaseNumber = form["caseNumber"].ToString(),
                CollectedBy = form["collectedBy"].ToString() ?? request.HttpContext.User?.Identity?.Name ?? "Unknown",
                CollectionDate = DateTimeOffset.TryParse(form["collectionDate"], out var date)
                    ? date
                    : DateTimeOffset.UtcNow,
                CollectionLocation = form["collectionLocation"].ToString(),
                Description = form["description"].ToString(),
                SessionId = form["sessionId"].ToString()
            };

            // Ingest evidence
            using var stream = file.OpenReadStream();
            var evidenceContext = await evidenceManager.IngestFileAsync(
                stream,
                file.FileName,
                metadata,
                ct);

            return Results.Created($"/api/evidence/{evidenceContext.Hash}", evidenceContext);
        })
        .WithName("IngestEvidence")
        .WithSummary("Direct evidence ingestion for smaller files")
        .Produces<ManagedFileContext>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .DisableAntiforgery(); // Required for file uploads

        // ========================================
        // EVIDENCE RETRIEVAL
        // ========================================

        // Get evidence by ID
        evidence.MapGet("/{fileId}", async (
            string fileId,
            [FromServices] IManagedFileManager fileManager,
            CancellationToken ct) =>
        {
            var fileItem = await fileManager.GetFileAsync(evidenceId, ct);
            return fileItem != null
                ? Results.Ok(fileItem)
                : Results.NotFound(new { error = $"Evidence {evidenceId} not found" });
        })
        .WithName("GetFile")
        .WithSummary("Get file details by ID")
        .Produces<ManagedFile>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Get evidence by case
        evidence.MapGet("/workspace/{workspaceId}", async (
            string workspaceId,
            [FromServices] IManagedFileManager evidenceManager,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var evidenceList = await evidenceManager.GetFilesByWorkspaceAsync(workspaceId, ct);

            // Apply pagination
            var paginatedList = evidenceList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Results.Ok(new
            {
                Evidence = paginatedList,
                TotalCount = evidenceList.Count,
                Page = page,
                PageSize = pageSize
            });
        })
        .WithName("GetFilesByWorkspace")
        .WithSummary("Get all files for a specific workspace")
        .Produces<object>();

        //// Search evidence
        //evidence.MapPost("/search", async (
        //    [FromBody] SearchEvidenceRequest request,
        //    [FromServices] IEvidenceManager evidenceManager,
        //    CancellationToken ct) =>
        //{
        //    var results = await evidenceManager.SearchEvidenceAsync(
        //        request.SearchTerm,
        //        request.CaseId,
        //        request.EvidenceType,
        //        request.StartDate,
        //        request.EndDate,
        //        ct);

        //    return Results.Ok(results);
        //})
        //.WithName("SearchEvidence")
        //.WithSummary("Search evidence with filters")
        //.Produces<List<Evidence>>();

        // ========================================
        // EVIDENCE MANAGEMENT
        // ========================================

        ////To do : Implement this once we have the schema set
        //// Update evidence metadata
        //evidence.MapPut("/{evidenceId}/metadata", async (
        //    string evidenceId,
        //    [FromBody] UpdateEvidenceMetadataRequest request,
        //    [FromServices] IEvidenceManager evidenceManager,
        //    HttpContext httpContext,
        //    CancellationToken ct) =>
        //{
        //    var updated = await evidenceManager.UpdateMetadataAsync(
        //        evidenceId,
        //        request.Metadata,
        //        httpContext.User?.Identity?.Name ?? "Unknown",
        //        ct);

        //    return updated
        //        ? Results.NoContent()
        //        : Results.NotFound();
        //})
        //.WithName("UpdateEvidenceMetadata")
        //.WithSummary("Update evidence metadata")
        //.RequireAuthorization()
        //.ProducesProblem(StatusCodes.Status404NotFound);

        //// Delete evidence (soft delete with audit trail)
        //evidence.MapDelete("/{evidenceId}", async (
        //    string evidenceId,
        //    [FromServices] IEvidenceManager evidenceManager,
        //    HttpContext httpContext,
        //    CancellationToken ct,
        //    [FromQuery] string? reason = null) =>
        //{
        //    var deleted = await evidenceManager.DeleteEvidenceAsync(
        //        evidenceId,
        //        reason ?? "Deleted via API",
        //        httpContext.User?.Identity?.Name ?? "Unknown",
        //        ct);

        //    return deleted
        //        ? Results.NoContent()
        //        : Results.NotFound();
        //})
        //.WithName("DeleteEvidence")
        //.WithSummary("Soft delete evidence with audit trail")
        //.RequireAuthorization()
        //.ProducesProblem(StatusCodes.Status404NotFound);

        // ========================================
        // CHAIN OF CUSTODY
        // ========================================

        // Get chain of custody
        evidence.MapGet("/{fileId}/chain-of-custody", async (
            string fileId,
            [FromServices] IManagedFileManager fileManager,
            CancellationToken ct) =>
        {
            var auditLog = await fileManager.GetAuditLogAsync(fileId, ct);
            if (auditLog == null)
            {
                return Results.NotFound(new { error = $"Audit Log for {fileId} not found" });
            }

            return Results.Ok(auditLog);
        })
        .WithName("GetChainOfCustody")
        .WithSummary("Get chain of custody for file")
        .Produces<List<ChainOfCustodyEntry>>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        //// Add chain of custody entry
        //evidence.MapPost("/{evidenceId}/chain-of-custody", async (
        //    string evidenceId,
        //    [FromBody] AddChainOfCustodyRequest request,
        //    [FromServices] IEvidenceManager evidenceManager,
        //    HttpContext httpContext,
        //    CancellationToken ct) =>
        //{
        //    var entry = new ChainOfCustodyEntry
        //    {
        //        Action = request.Action,
        //        Actor = httpContext.User?.Identity?.Name ?? "Unknown",
        //        Details = request.Details,
        //        Notes = request.Notes,
        //        Metadata = request.Metadata
        //    };

        //    var added = await evidenceManager.AddChainOfCustodyEntryAsync(
        //        evidenceId,
        //        entry,
        //        ct);

        //    return added
        //        ? Results.Created($"/api/evidence/{evidenceId}/chain-of-custody", entry)
        //        : Results.NotFound();
        //})
        //.WithName("AddChainOfCustodyEntry")
        //.WithSummary("Add entry to chain of custody")
        //.RequireAuthorization()
        //.Produces<ChainOfCustodyEntry>(StatusCodes.Status201Created)
        //.ProducesProblem(StatusCodes.Status404NotFound);

        // ========================================
        // EVIDENCE PROCESSING
        // ========================================

        // Get processed versions
        evidence.MapGet("/{fileId}/processed", async (
            string fileId,
            [FromServices] IManagedFileManager fileManager,
            CancellationToken ct) =>
        {
            var evidence = await fileManager.GetFilesAsync(fileId, ct);
            if (evidence == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(evidence.ProcessedVersions);
        })
        .WithName("GetProcessedVersions")
        .WithSummary("Get all processed versions of evidence")
        .Produces<List<ProcessedFile>>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Verify evidence integrity
        evidence.MapPost("/{evidenceId}/verify", async (
            string fileId,
            [FromServices] IManagedFileManager fileManager,
            CancellationToken ct) =>
        {
            var result = await fileManager.GetFilesAsync(fileId, ct);
            return Results.Ok(result);
        })
        .WithName("VerifyEvidenceIntegrity")
        .WithSummary("Verify evidence integrity using stored hashes")
        .Produces<IntegrityVerificationResult>();
    }
}
