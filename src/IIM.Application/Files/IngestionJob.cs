using IIM.Shared.Mediator;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Files;

/// <summary>
/// Hangfire job for file ingestion.
/// </summary>
public sealed class IngestionJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<IngestionJob> _logger;

    public IngestionJob(IMediator mediator, ILogger<IngestionJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task RunAsync(Guid virtualFileId, CancellationToken ct)
    {
        _logger.LogInformation("Starting ingestion job for {VirtualFileId}", virtualFileId);

        try
        {
            var result = await _mediator.Send(new IngestFileCommand(virtualFileId), ct);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Ingestion completed for {VirtualFileId}: {ChunkCount} chunks, {EntityCount} entities",
                    virtualFileId,
                    result.ChunkCount,
                    result.EntityCount);
            }
            else
            {
                _logger.LogWarning(
                    "Ingestion failed for {VirtualFileId}: {Error}",
                    virtualFileId,
                    result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion job failed for {VirtualFileId}", virtualFileId);
            throw; // Re-throw so Hangfire can retry
        }
    }
}
