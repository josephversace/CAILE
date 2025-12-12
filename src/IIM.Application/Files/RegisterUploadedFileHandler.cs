using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Files;

public sealed class RegisterUploadedFileHandler
	: IRequestHandler<RegisterUploadedFileCommand, Guid>
{
	private readonly IWorkspaceManager _workspace;
	private readonly IFileStore _files;
	private readonly IHashService _hashes;
	private readonly IMediator _mediator;
	private readonly ILogger<RegisterUploadedFileHandler> _logger;

	public RegisterUploadedFileHandler(
		IWorkspaceManager workspace,
		IFileStore files,
		IHashService hashes,
		IMediator mediator,
		ILogger<RegisterUploadedFileHandler> logger)
	{
		_workspace = workspace;
		_files = files;
		_hashes = hashes;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<Guid> Handle(
		RegisterUploadedFileCommand cmd,
		CancellationToken ct)
	{
		if (cmd.WorkspaceId == Guid.Empty)
			throw new ArgumentException("WorkspaceId is required.");

		if (cmd.InputStream == null || !cmd.InputStream.CanRead)
			throw new ArgumentException("InputStream must be readable.");

		// ------------------------------------------------------------
		// 1. Compute BLAKE3 hash (streaming)
		// ------------------------------------------------------------
		string blake3;
		cmd.InputStream.Position = 0;
		blake3 = await _hashes.ComputeBlake3Async(cmd.InputStream, ct);

		_logger.LogDebug("Computed BLAKE3 hash {Hash}", blake3);

		// ------------------------------------------------------------
		// 2. Deduplication check
		// ------------------------------------------------------------
		var existingStored = await _workspace.GetStoredFileByHashAsync(blake3, ct);

		if (existingStored != null)
		{
			_logger.LogInformation(
				"Deduplicated upload: {FileName} → {Hash}",
				cmd.FileName,
				blake3);

			var vf = new VirtualFile
			{
				WorkspaceId = cmd.WorkspaceId,
				FileName = cmd.FileName,
				FileSize = cmd.FileSize,
				StoredFileHash = existingStored.Blake3Hash,
				CreatedAt = DateTime.UtcNow,
				Status = FileUploadStatus.Completed
			};

			var created = await _workspace.CreateVirtualFileAsync(vf, ct);

			if (cmd.Reprocess)
			{
				await _mediator.Send(
					new IngestFileCommand(created.Id),
					ct);
			}

			return created.Id;
		}

		// ------------------------------------------------------------
		// 3. Write file to SeaweedFS (quarantine)
		// ------------------------------------------------------------
		cmd.InputStream.Position = 0;

		var sanitizedName = SanitizeFileName(cmd.FileName);
		var storagePath = $"quarantine/{blake3}/{sanitizedName}";

		await _files.WriteAsync(cmd.InputStream, storagePath, ct);

		_logger.LogInformation(
			"Stored new file {FileName} at {Path}",
			cmd.FileName,
			storagePath);

		// ------------------------------------------------------------
		// 4. Create StoredFile
		// ------------------------------------------------------------
		var stored = new StoredFile
		{
			Blake3Hash = blake3,
			FileSize = cmd.FileSize,
			MimeType = cmd.MimeType,
			Bucket = "quarantine",
			StoragePath = storagePath,
			OriginalFileName = cmd.FileName,
			FirstWorkspaceId = cmd.WorkspaceId,
			FirstSeenAt = DateTimeOffset.UtcNow
		};

		await _workspace.CreateStoredFileAsync(stored, ct);

		// ------------------------------------------------------------
		// 5. Create VirtualFile
		// ------------------------------------------------------------
		var virtualFile = new VirtualFile
		{
			WorkspaceId = cmd.WorkspaceId,
			FileName = cmd.FileName,
			FileSize = cmd.FileSize,
			StoredFileHash = blake3,
			CreatedAt = DateTime.UtcNow,
			Status = FileUploadStatus.Pending
		};

		var createdVf = await _workspace.CreateVirtualFileAsync(virtualFile, ct);

		// ------------------------------------------------------------
		// 6. Trigger ingestion (always for new content)
		// ------------------------------------------------------------
		await _mediator.Send(
			new IngestFileCommand(createdVf.Id),
			ct);

		return createdVf.Id;
	}

	private static string SanitizeFileName(string fileName)
	{
		var invalid = Path.GetInvalidFileNameChars();
		var sanitized = string.Join("_",
			fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));

		return string.IsNullOrWhiteSpace(sanitized)
			? "file"
			: sanitized;
	}
}
