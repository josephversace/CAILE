
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Models;
using IIM.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using IIM.Shared.Interfaces;
using IIM.Application.Evidence;

namespace IIM.Api.Endpoints;

/// <summary>
/// Evidence management endpoints for handling digital evidence with chain of custody
/// </summary>
public static class EvidenceEndpoints
{
    /// <summary>
    /// Maps all evidence-related endpoints for upload, retrieval, and management
    /// </summary>
    public static void MapEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        var evidence = app.MapGroup("/api/evidence")
            .WithTags("Evidence")
            .WithOpenApi();

        // ========================================
        // EVIDENCE UPLOAD WORKFLOW
        // ========================================

        // Initiate evidence upload - checks for duplicates and gets upload URL
        evidence.MapPost("/initiate-upload", async (
            [FromBody] InitiateEvidenceUploadRequest request,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new InitiateEvidenceUploadCommand
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
        .WithName("InitiateEvidenceUpload")
        .WithSummary("Initiate evidence upload with deduplication check")
        .Produces<InitiateEvidenceUploadResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Confirm evidence upload completion
        evidence.MapPost("/confirm-upload", async (
            [FromBody] ConfirmEvidenceUploadRequest request,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ConfirmEvidenceUploadCommand
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
            [FromServices] IEvidenceManager evidenceManager,
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
            var metadata = new EvidenceMetadata
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
            var evidenceContext = await evidenceManager.IngestEvidenceAsync(
                stream,
                file.FileName,
                metadata,
                ct);

            return Results.Created($"/api/evidence/{evidenceContext.Hash}", evidenceContext);
        })
        .WithName("IngestEvidence")
        .WithSummary("Direct evidence ingestion for smaller files")
        .Produces<EvidenceContext>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .DisableAntiforgery(); // Required for file uploads

        // ========================================
        // EVIDENCE RETRIEVAL
        // ========================================

        // Get evidence by ID
        evidence.MapGet("/{evidenceId}", async (
            string evidenceId,
            [FromServices] IEvidenceManager evidenceManager,
            CancellationToken ct) =>
        {
            var evidenceItem = await evidenceManager.GetEvidenceAsync(evidenceId, ct);
            return evidenceItem != null
                ? Results.Ok(evidenceItem)
                : Results.NotFound(new { error = $"Evidence {evidenceId} not found" });
        })
        .WithName("GetEvidence")
        .WithSummary("Get evidence details by ID")
        .Produces<Evidence>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Get evidence by case
        evidence.MapGet("/case/{caseId}", async (
            string caseId,
            [FromServices] IEvidenceManager evidenceManager,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var evidenceList = await evidenceManager.GetEvidenceByCaseAsync(caseId, ct);

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
        .WithName("GetEvidenceByCase")
        .WithSummary("Get all evidence for a specific case")
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

        // Update evidence metadata
        evidence.MapPut("/{evidenceId}/metadata", async (
            string evidenceId,
            [FromBody] UpdateEvidenceMetadataRequest request,
            [FromServices] IEvidenceManager evidenceManager,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var updated = await evidenceManager.UpdateMetadataAsync(
                evidenceId,
                request.Metadata,
                httpContext.User?.Identity?.Name ?? "Unknown",
                ct);

            return updated
                ? Results.NoContent()
                : Results.NotFound();
        })
        .WithName("UpdateEvidenceMetadata")
        .WithSummary("Update evidence metadata")
        .RequireAuthorization()
        .ProducesProblem(StatusCodes.Status404NotFound);

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
        evidence.MapGet("/{evidenceId}/chain-of-custody", async (
            string evidenceId,
            [FromServices] IEvidenceManager evidenceManager,
            CancellationToken ct) =>
        {
            var evidence = await evidenceManager.GetEvidenceAsync(evidenceId, ct);
            if (evidence == null)
            {
                return Results.NotFound(new { error = $"Evidence {evidenceId} not found" });
            }

            return Results.Ok(evidence.ChainOfCustody);
        })
        .WithName("GetChainOfCustody")
        .WithSummary("Get chain of custody for evidence")
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
        evidence.MapGet("/{evidenceId}/processed", async (
            string evidenceId,
            [FromServices] IEvidenceManager evidenceManager,
            CancellationToken ct) =>
        {
            var evidence = await evidenceManager.GetEvidenceAsync(evidenceId, ct);
            if (evidence == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(evidence.ProcessedVersions);
        })
        .WithName("GetProcessedVersions")
        .WithSummary("Get all processed versions of evidence")
        .Produces<List<ProcessedEvidence>>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Verify evidence integrity
        evidence.MapPost("/{evidenceId}/verify", async (
            string evidenceId,
            [FromServices] IEvidenceManager evidenceManager,
            CancellationToken ct) =>
        {
            var result = await evidenceManager.VerifyIntegrityAsync(evidenceId, ct);
            return Results.Ok(result);
        })
        .WithName("VerifyEvidenceIntegrity")
        .WithSummary("Verify evidence integrity using stored hashes")
        .Produces<IntegrityVerificationResult>();
    }
}
