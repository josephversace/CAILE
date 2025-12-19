using IIM.Ingestion.Interfaces;
using IIM.Shared.Enums;
using IIM.Shared.Events;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Files;

/// <summary>
/// Command to ingest a file (extract text, chunk, embed, store).
/// </summary>
public sealed record IngestFileCommand(Guid VirtualFileId) : IRequest<IngestFileResult>;

public sealed class IngestFileResult
{
    public required string Blake3Hash { get; init; }
    public required bool Success { get; init; }
    public int ChunkCount { get; init; }
    public int EntityCount { get; init; }
    public int VectorCount { get; init; }
    public bool Deduplicated { get; init; }
    public string? Error { get; init; }
}

public sealed class IngestFileHandler : IRequestHandler<IngestFileCommand, IngestFileResult>
{
    private readonly IIngestionPipeline _pipeline;
    private readonly IWorkspaceManager _workspace;
    private readonly IIngestionNotifier _notifier;
    private readonly ILogger<IngestFileHandler> _logger;

    public IngestFileHandler(
        IIngestionPipeline pipeline,
        IWorkspaceManager workspace,
        IIngestionNotifier notifier,
        ILogger<IngestFileHandler> logger)
    {
        _pipeline = pipeline;
        _workspace = workspace;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<IngestFileResult> Handle(IngestFileCommand cmd, CancellationToken ct)
    {
        var vf = await _workspace.GetVirtualFileByIdAsync(cmd.VirtualFileId, ct);
        if (vf == null)
        {
            return new IngestFileResult
            {
                Blake3Hash = "",
                Success = false,
                Error = $"VirtualFile {cmd.VirtualFileId} not found"
            };
        }

        try
        {
            var result = await _pipeline.IngestAsync(cmd.VirtualFileId, ct);

            // Update VirtualFile status
            vf.Status = FileUploadStatus.Completed;
            await _workspace.UpdateVirtualFileAsync(vf, ct);

            // Notify clients
            await _notifier.NotifyFileIngestedAsync(new FileIngestedEvent
            {
                VirtualFileId = vf.Id,
                WorkspaceId = vf.WorkspaceId,
                Success = true,
                Blake3Hash = result.StoredId,
                FileName = vf.FileName,
                MimeType = vf.StoredFile?.MimeType,
                ChunkCount = result.ChunkCount,
                EntityCount = result.EntityCount
            }, ct);

            return new IngestFileResult
            {
                Blake3Hash = result.StoredId ?? "",
                Success = true,
                ChunkCount = result.ChunkCount,
                EntityCount = result.EntityCount,
                VectorCount = result.VectorCount,
                Deduplicated = result.Deduplicated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for {VirtualFileId}", cmd.VirtualFileId);

            // Update VirtualFile status
            vf.Status = FileUploadStatus.Failed;
            await _workspace.UpdateVirtualFileAsync(vf, ct);

            // Notify clients of failure
            await _notifier.NotifyFileIngestedAsync(new FileIngestedEvent
            {
                VirtualFileId = vf.Id,
                WorkspaceId = vf.WorkspaceId,
                Success = false,
                Blake3Hash = vf.StoredFileHash,
                FileName = vf.FileName,
                Error = ex.Message
            }, ct);

            return new IngestFileResult
            {
                Blake3Hash = vf.StoredFileHash ?? "",
                Success = false,
                Error = ex.Message
            };
        }
    }
}
