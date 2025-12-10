using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models.Core;
using MagikaSharp;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Files;

public class UploadFileDirectHandler
	: IRequestHandler<UploadFileDirectCommand, UploadFileDirectResult>
{
	private readonly IFileStore _files;
	private readonly IHashService _hashes;
	private readonly IWorkspaceManager _workspace;
	private readonly IMediator _mediator;
	private readonly ILogger<UploadFileDirectHandler> _logger;

	public UploadFileDirectHandler(
		IFileStore files,
		IHashService hashes,
		IWorkspaceManager workspace,
		IMediator mediator,
		ILogger<UploadFileDirectHandler> logger)
	{
		_files = files;
		_hashes = hashes;
		_workspace = workspace;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<UploadFileDirectResult> Handle(UploadFileDirectCommand cmd, CancellationToken ct)
	{
		if (cmd.File.Length == 0)
			throw new InvalidOperationException("Cannot upload empty file.");

		_logger.LogInformation("Processing upload: {FileName} ({Size} bytes) for workspace {WorkspaceId}",
			cmd.File.FileName, cmd.File.Length, cmd.WorkspaceId);

		// 1. Read file to memory (needed for hashing + Magika)
		byte[] bytes;
		await using (var stream = cmd.File.OpenReadStream())
		{
			using var ms = new MemoryStream();
			await stream.CopyToAsync(ms, ct);
			bytes = ms.ToArray();
		}

		// 2. Compute hashes
		using var hashStream = new MemoryStream(bytes);
		var blake3 = await _hashes.ComputeBlake3Async(hashStream, ct);

		hashStream.Position = 0;
		var sha256 = await _hashes.ComputeSha256Async(hashStream, ct);

		_logger.LogDebug("Hashes: BLAKE3={Blake3}, SHA256={Sha256}", blake3, sha256);

		// 3. Dedup check
		if (await _workspace.StoredFileExistsAsync(blake3, ct))
		{
			_logger.LogInformation("File deduplicated: {Hash} already exists.", blake3);

			var existing = await _workspace.GetStoredFileByHashAsync(blake3, ct);

			var vf = new VirtualFile
			{
				WorkspaceId = cmd.WorkspaceId,
				FileName = cmd.File.FileName,
				StoredFileHash = existing!.Blake3Hash,
				CreatedAt = DateTime.UtcNow
			};

			var saved = await _workspace.CreateVirtualFileAsync(vf, ct);

			// Still trigger ingestion for the new virtual file
			await _mediator.Send(new IngestFileCommand(saved.Id), ct);

			return new UploadFileDirectResult(saved.Id, blake3, bytes.Length, WasDeduplicated: true);
		}

		// 4. Detect file type with Magika
		string mime;
		using (var magika = new MagikaSession())
		{
			var type = magika.IdentifyBytes(bytes);
			mime = type.MimeType ?? "application/octet-stream";
		}

		// 5. Upload to SeaweedFS
		var ext = Path.GetExtension(cmd.File.FileName);
		var sanitizedName = SanitizeFileName(Path.GetFileNameWithoutExtension(cmd.File.FileName));
		var storagePath = $"quarantine/{blake3}/{sanitizedName}{ext}";

		await _files.WriteAsync(bytes, storagePath, ct);

		_logger.LogDebug("File written to SeaweedFS: {Path}", storagePath);

		// 6. Create StoredFile
		var stored = new StoredFile
		{
			Blake3Hash = blake3,
			Sha256Hash = sha256,
			StoragePath = storagePath,
			MimeType = mime,
			FileSize = bytes.Length,
			FirstSeenAt = DateTime.UtcNow
		};

		await _workspace.CreateStoredFileAsync(stored, ct);

		// 7. Create VirtualFile
		var virtualFile = new VirtualFile
		{
			WorkspaceId = cmd.WorkspaceId,
			FileName = cmd.File.FileName,
			StoredFileHash = blake3,
			CreatedAt = DateTime.UtcNow
		};

		var created = await _workspace.CreateVirtualFileAsync(virtualFile, ct);

		_logger.LogInformation("Upload complete: FileId={FileId}, Hash={Hash}", created.Id, blake3);

		// 8. Trigger ingestion pipeline
		await _mediator.Send(new IngestFileCommand(created.Id), ct);

		return new UploadFileDirectResult(created.Id, blake3, bytes.Length, WasDeduplicated: false);
	}

	private static string SanitizeFileName(string fileName)
	{
		var invalid = Path.GetInvalidFileNameChars();
		var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
		return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
	}
}