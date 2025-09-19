using Hangfire;
using IIM.Application.AI.DataEnrichment;
using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Api.Endpoints
{
    public static class WebhookEndpoints
    {
        public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
        {
            var webhooks = app.MapGroup("/api/webhooks")
                .WithTags("Webhooks")
                .ExcludeFromDescription(); // Hide from Swagger if desired

            // SeaweedFS S3 event notification
            // IIM.Api/Endpoints/WebhookEndpoints.cs

            webhooks.MapPost("/seaweedfs/s3-event", async (
                [FromBody] S3EventNotification notification,
                [FromServices] IBackgroundJobClient backgroundJobs,
                [FromServices] ILogger<Program> logger,
                CancellationToken ct) =>
            {
                foreach (var record in notification.Records)
                {
                    if (record.EventName.StartsWith("s3:ObjectCreated"))
                    {
                        // Queue directly to DataEnrichmentOrchestrator
                        var jobId = backgroundJobs.Enqueue<IDataReasoningService>(
                            service => ((DataEnrichmentOrchestrator)service).ProcessUploadedFile(
                                record.S3.Bucket.Name,
                                record.S3.Object.Key,
                                record.S3.Object.Size
                            ));

                        logger.LogInformation(
                            "Queued processing job {JobId} for {Bucket}/{Key}",
                            jobId, record.S3.Bucket.Name, record.S3.Object.Key);
                    }
                }

                return Results.Ok();
            });
        }
    }

    // DTOs for webhooks
    public record S3EventNotification(S3EventRecord[] Records);
    public record S3EventRecord(
        string EventName,
        S3Entity S3
    );
    public record S3Entity(
        S3Bucket Bucket,
        S3Object Object
    );
    public record S3Bucket(string Name);
    public record S3Object(string Key, long Size);

    public record UploadCompleteNotification(
        Guid WorkspaceId,
        string FileName,
        string? Path,
        string Bucket,
        string ObjectKey,
        long FileSize
    );
}