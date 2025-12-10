using System;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models.Core;
using MagikaSharp;

namespace IIM.Application.Files
{
	public class CompleteUploadHandler
		: IRequestHandler<CompleteUploadCommand, Unit>
	{
		private readonly IFileStore _files;
		private readonly IHashService _hashes;
		private readonly IWorkspaceManager _workspace;
		private readonly IMediator _mediator;

		public CompleteUploadHandler(
			IFileStore files,
			IHashService hashes,
			IWorkspaceManager workspace,
			IMediator mediator)
		{
			_files = files;
			_hashes = hashes;
			_workspace = workspace;
			_mediator = mediator;
		}

		public async Task<Unit> Handle(CompleteUploadCommand cmd, CancellationToken ct)
		{
			// ------------------------------------------------------------
			// 1. Read uploaded bytes from SeaweedFS
			// ------------------------------------------------------------
			var bytes = await _files.ReadAsync(cmd.SeaweedFileId, ct);

			if (bytes == null || bytes.Length == 0)
				throw new InvalidOperationException("Uploaded file is empty or missing in SeaweedFS.");

			using var stream = new MemoryStream(bytes);

			// ------------------------------------------------------------
			// 2. Hashing (required for StoredFile)
			// ------------------------------------------------------------
			var blake3 = await _hashes.ComputeBlake3Async(stream, ct);
			stream.Position = 0;
			var sha256 = await _hashes.ComputeSha256Async(stream, ct);

			// ------------------------------------------------------------
			// 3. Magika file type detection
			// ------------------------------------------------------------
			MagikaTypeInfo info;
			using (var magika = new MagikaSession())
			{
				info = magika.IdentifyBytes(bytes);  // MagikaSharp requires byte[]
			}

			var mime = info.MimeType ?? "application/octet-stream";
		

			// ------------------------------------------------------------
			// 4. Dedup check (StoredFileExistsAsync)
			// ------------------------------------------------------------
			if (await _workspace.StoredFileExistsAsync(blake3, ct))
			{
				var existing = await _workspace.GetStoredFileByHashAsync(blake3, ct);

				var vfile = new VirtualFile
				{
					WorkspaceId = cmd.WorkspaceId,
					FileName = cmd.OriginalFileName,
					StoredFileHash = blake3,
			
					CreatedAt = DateTime.UtcNow
				};

				var created = await _workspace.CreateVirtualFileAsync(vfile, ct);

				// Trigger ingestion
				await _mediator.Send(new IngestFileCommand(created.Id), ct);

				return Unit.Value;
			}

			// ------------------------------------------------------------
			// 5. Create StoredFile (requires Blake3)
			// ------------------------------------------------------------
			var stored = new StoredFile
			{
				Blake3Hash = blake3,
				Sha256Hash = sha256,
				StoragePath = cmd.SeaweedFileId,
				MimeType = mime,
				FirstSeenAt = DateTime.UtcNow
			};

			await _workspace.CreateStoredFileAsync(stored, ct);

			// ------------------------------------------------------------
			// 6. Create VirtualFile
			// ------------------------------------------------------------
			var virtualFile = new VirtualFile
			{
				WorkspaceId = cmd.WorkspaceId,
				FileName = cmd.OriginalFileName,
				StoredFileHash = blake3,
				CreatedAt = DateTime.UtcNow
			};

			var vf = await _workspace.CreateVirtualFileAsync(virtualFile, ct);

			// ------------------------------------------------------------
			// 7. Trigger ingestion
			// ------------------------------------------------------------
			await _mediator.Send(new IngestFileCommand(vf.Id), ct);

			return Unit.Value;
		}
	}
}
