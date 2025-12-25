using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Files;

public sealed class RegisterUploadedFileHandler
	: IRequestHandler<RegisterUploadedFileCommand, RegisterUploadedFileResult>
{
	private readonly IWorkspaceManager _workspace;
	private readonly IFileStore _files;
	private readonly IHashService _hashes;
	private readonly IBackgroundJobClient _jobs;
	private readonly ILogger<RegisterUploadedFileHandler> _logger;
	private readonly IMediator _mediator;

	public RegisterUploadedFileHandler(
		IWorkspaceManager workspace,
		IFileStore files,
		IHashService hashes,
		IBackgroundJobClient jobs,
		IMediator mediator,
		ILogger<RegisterUploadedFileHandler> logger)
	{
		_workspace = workspace;
		_files = files;
		_hashes = hashes;
		_jobs = jobs;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<RegisterUploadedFileResult> Handle(
		RegisterUploadedFileCommand cmd,
		CancellationToken ct)
	{
		if (cmd.WorkspaceId == Guid.Empty)
			throw new ArgumentException("WorkspaceId is required.");

		if (cmd.InputStream == null || !cmd.InputStream.CanRead)
			throw new ArgumentException("InputStream must be readable.");

		// ------------------------------------------------------------
		// 1. Compute all hashes in single pass (BLAKE3, MD5, SHA-256)
		// ------------------------------------------------------------
		cmd.InputStream.Position = 0;
		var hashes = await _hashes.ComputeAllHashesAsync(cmd.InputStream, ct);

		_logger.LogDebug(
			"Computed hashes - BLAKE3: {Blake3}, MD5: {Md5}, SHA256: {Sha256}",
			hashes.Blake3[..12],
			hashes.Md5,
			hashes.Sha256);

		// ------------------------------------------------------------
		// 2. Deduplication check (by BLAKE3)
		// ------------------------------------------------------------
		var existingStored = await _workspace.GetStoredFileByHashAsync(hashes.Blake3, ct);

		if (existingStored != null)
		{
			_logger.LogInformation(
				"Deduplicated upload: {FileName} → {Hash}",
				cmd.FileName,
				hashes.Blake3[..12]);

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

			_jobs.Enqueue<IngestionJob>(job => job.RunAsync(created.Id, CancellationToken.None));

			return new RegisterUploadedFileResult
			{
				VirtualFileId = created.Id,
				Blake3Hash = hashes.Blake3,
				Deduplicated = true
			};
		}

		// ------------------------------------------------------------
		// 3. Write file to SeaweedFS (quarantine)
		// ------------------------------------------------------------
		cmd.InputStream.Position = 0;

		var sanitizedName = SanitizeFileName(cmd.FileName);
		var objectKey = $"{hashes.Blake3}";

		await _files.WriteAsync("quarantine", objectKey, cmd.InputStream, ct);

		_logger.LogInformation(
			"Stored new file {FileName} at quarantine/{ObjectKey}",
			cmd.FileName,
			objectKey);

		// ------------------------------------------------------------
		// 4. Create StoredFile (with all hashes)
		// ------------------------------------------------------------
		var stored = new StoredFile
		{
			Blake3Hash = hashes.Blake3,
			Md5Hash = hashes.Md5,
			Sha256Hash = hashes.Sha256,
			FileSize = cmd.FileSize,
			MimeType = cmd.MimeType,
			Bucket = "quarantine",
			StoragePath = objectKey,
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
			StoredFileHash = hashes.Blake3,
			CreatedAt = DateTime.UtcNow,
			Status = FileUploadStatus.Pending
		};

		var createdVf = await _workspace.CreateVirtualFileAsync(virtualFile, ct);

		// ------------------------------------------------------------
		// 6. Enqueue ingestion job (runs in background via Hangfire)
		// ------------------------------------------------------------
		_jobs.Enqueue<IngestionJob>(job => job.RunAsync(createdVf.Id, CancellationToken.None));

		return new RegisterUploadedFileResult
		{
			VirtualFileId = createdVf.Id,
			Blake3Hash = hashes.Blake3,
			Deduplicated = false
		};
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
