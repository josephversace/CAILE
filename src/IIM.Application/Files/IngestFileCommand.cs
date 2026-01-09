using IIM.Ingestion.Services;          // <-- IngestionRunOptions
using IIM.Ingestion.Services;          // <-- IIngestionRunner (wherever it lives in your solution)
using IIM.Shared.Enums;
using IIM.Shared.Events;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Files;

/// <summary>
/// Command to ingest a file using the ingestion runner (step pipeline).
/// </summary>
public sealed record IngestFileCommand(
	Guid VirtualFileId,
	IngestionRunOptions? Options = null   // <-- caller can pass, null uses default
) : IRequest<IngestFileResult>;

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
	private readonly IIngestionRunner _runner;
	private readonly IWorkspaceManager _workspace;
	private readonly IIngestionNotifier _notifier;
	private readonly ILogger<IngestFileHandler> _logger;

	public IngestFileHandler(
		IIngestionRunner runner,
		IWorkspaceManager workspace,
		IIngestionNotifier notifier,
		ILogger<IngestFileHandler> logger)
	{
		_runner = runner;
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
			// ✅ Runner owns defaults: options ??= IngestionRunOptions.Default
			var result = await _runner.RunAsync(cmd.VirtualFileId, cmd.Options, ct);

			vf.Status = FileUploadStatus.Completed;
			await _workspace.UpdateVirtualFileAsync(vf, ct);

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

			vf.Status = FileUploadStatus.Failed;
			await _workspace.UpdateVirtualFileAsync(vf, ct);

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
