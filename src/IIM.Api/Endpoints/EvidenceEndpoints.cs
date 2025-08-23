using IIM.Application.Commands.Evidence;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.DTOs;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Api.Endpoints;

public static class EvidenceEndpoints
{
    public static void MapEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        var evidence = app.MapGroup("/api/evidence")
            .RequireAuthorization();

        // Ingest evidence with file upload
        evidence.MapPost("/ingest", async (
            HttpRequest request,
            [FromServices] IMediator mediator,
            [FromServices] ILogger<Program> logger) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new ErrorResponse(
                    ErrorCode: "INVALID_REQUEST",
                    Message: "Form data required"
                ));
            }

            try
            {
                var form = await request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();

                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new ErrorResponse(
                        ErrorCode: "NO_FILE",
                        Message: "No file provided"
                    ));
                }

                // Map form data to DTO
                var ingestRequest = new EvidenceIngestRequest(
                    CaseNumber: form["caseNumber"].ToString(),
                    CollectedBy: form["collectedBy"].ToString(),
                    CollectionLocation: form["collectionLocation"].ToString(),
                    DeviceSource: form["deviceSource"].ToString(),
                    Description: form["description"].ToString(),
                    CollectionDate: DateTimeOffset.TryParse(form["collectionDate"], out var date)
                        ? date : DateTimeOffset.UtcNow,
                    CustomFields: ParseCustomFields(form)
                );

                // Create command from DTO
                using var stream = file.OpenReadStream();
                var command = new IngestEvidenceCommand
                {
                    FileStream = stream,
                    FileName = file.FileName,
                    // Map the metadata directly, not through a Request property
                    Metadata = new EvidenceMetadata(
                        ingestRequest.CaseNumber,
                        ingestRequest.CollectedBy,
                        ingestRequest.CollectionDate ?? DateTimeOffset.UtcNow,
                        ingestRequest.CollectionLocation,
                        ingestRequest.DeviceSource,
                        ingestRequest.Description,
                        ingestRequest.CustomFields
                    )
                };

                var result = await mediator.Send(command);

                // Map to response DTO
                var response = MapToEvidenceResponse(result);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ingest evidence");
                return Results.Problem(new ErrorResponse(
                    ErrorCode: "INGESTION_FAILED",
                    Message: "Evidence ingestion failed",
                    Details: ex.Message
                ));
            }
        })
        .WithName("IngestEvidence")
        .DisableAntiforgery() // Required for file uploads
        .Produces<EvidenceResponse>(200)
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(500);

        // Verify evidence integrity
        evidence.MapPost("/{evidenceId}/verify", async (
            string evidenceId,
            [FromServices] IEvidenceManager evidenceManager) =>
        {
            var isValid = await evidenceManager.VerifyIntegrityAsync(evidenceId);

            var response = new EvidenceVerificationResponse(
                EvidenceId: evidenceId,
                IntegrityValid: isValid,
                Timestamp: DateTimeOffset.UtcNow,
                Message: isValid ? "Integrity verified" : "Integrity check failed"
            );

            return Results.Ok(response);
        })
        .WithName("VerifyEvidence")
        .Produces<EvidenceVerificationResponse>(200);

        // Get chain of custody report
        evidence.MapGet("/{evidenceId}/chain", async (
            string evidenceId,
            [FromServices] IEvidenceManager evidenceManager) =>
        {
            var report = await evidenceManager.GenerateChainOfCustodyAsync(evidenceId);
            return Results.Ok(report);
        })
        .WithName("GetChainOfCustody")
        .Produces<ChainOfCustodyReport>(200);

        // Search evidence
        evidence.MapGet("/search", async (
            [FromServices] IMediator mediator,
            [AsParameters] SearchEvidenceRequest request) =>
        {
            var query = new SearchEvidenceQuery(request);
            var result = await mediator.Send(query);

            return Results.Ok(new EvidenceListResponse(
                Evidence: result,
                TotalCount: result.Count,
                Page: request.Page,
                PageSize: request.PageSize
            ));
        })
        .WithName("SearchEvidence")
        .Produces<EvidenceListResponse>(200);

        // Process evidence
        evidence.MapPost("/{evidenceId}/process", async (
            string evidenceId,
            [FromBody] EvidenceProcessRequest request,
            [FromServices] IMediator mediator) =>
        {
            var command = new ProcessEvidenceCommand
            {
                EvidenceId = evidenceId,
                ProcessingType = request.ProcessingType,
                Parameters = request.Parameters
            };

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("ProcessEvidence")
        .Produces<ProcessedEvidence>(200);

        // Export evidence
        evidence.MapPost("/{evidenceId}/export", async (
            string evidenceId,
            [FromBody] EvidenceExportRequest request,
            [FromServices] IMediator mediator) =>
        {
            var command = new ExportEvidenceCommand
            {
                EvidenceId = evidenceId,
                ExportPath = request.ExportPath,
                IncludeProcessedVersions = request.IncludeProcessedVersions,
                GenerateVerificationScript = request.GenerateVerificationScript,
                Format = request.Format
            };

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("ExportEvidence")
        .Produces<EvidenceExportResponse>(200);
    }

    // Helper methods
    private static Dictionary<string, string>? ParseCustomFields(IFormCollection form)
    {
        var customFields = new Dictionary<string, string>();

        foreach (var key in form.Keys.Where(k => k.StartsWith("custom_")))
        {
            customFields[key.Replace("custom_", "")] = form[key].ToString();
        }

        return customFields.Any() ? customFields : null;
    }

    private static EvidenceResponse MapToEvidenceResponse(Evidence result)
    {
        return new EvidenceResponse(
            Id: result.Id,
            CaseId: result.CaseId,
            CaseNumber: result.CaseNumber,
            OriginalFileName: result.OriginalFileName,
            FileSize: result.FileSize,
            Type: result.Type.ToString(),
            Status: result.Status.ToString(),
            IntegrityValid: result.IntegrityVerified,
            Hashes: new Dictionary<string, string> { [result.HashAlgorithm] = result.Hash },
            Signature: result.DigitalSignature,
            IngestTimestamp: result.IngestTimestamp,
            StoragePath: result.StoragePath,
            Metadata: new EvidenceMetadata(
                result.CaseNumber,
                result.CollectedBy,
                result.CollectionDate,
                result.CollectionLocation,
                result.DeviceSource,
                result.Description,
                result.CustomFields
            ),
            ChainOfCustody: result.ChainOfCustody,
            ProcessedVersions: result.ProcessedVersions
        );
    }
}