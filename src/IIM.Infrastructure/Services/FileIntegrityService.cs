using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Services;

public class FileIntegrityService : IFileIntegrityService
{
	private readonly IWorkspaceManager _workspace;
	private readonly IObjectStorageProvider _storage;
	private readonly IHashService _hashService;
	private readonly ILogger<FileIntegrityService> _logger;

	private const string EvidenceBucket = "evidence";

	public FileIntegrityService(
		IWorkspaceManager workspace,
		IObjectStorageProvider storage,
		IHashService hashService,
		ILogger<FileIntegrityService> logger)
	{
		_workspace = workspace;
		_storage = storage;
		_hashService = hashService;
		_logger = logger;
	}

	public async Task<bool> VerifyAsync(Guid virtualFileId, CancellationToken ct = default)
	{
		var vf = await _workspace.GetVirtualFileByIdAsync(virtualFileId, ct);
		if (vf == null)
		{
			_logger.LogWarning("Verify: VirtualFile {Id} not found.", virtualFileId);
			return false;
		}

		var stored = await _workspace.GetStoredFileByHashAsync(vf.StoredFileHash!, ct);
		if (stored == null)
		{
			_logger.LogError("Verify: StoredFile {Hash} not found.", vf.StoredFileHash);
			return false;
		}

		await using var stream =
			await _storage.GetObjectAsync(EvidenceBucket, stored.Blake3Hash, ct);

		var recomputed = await _hashService.ComputeBlake3Async(stream, ct);

		bool match = string.Equals(stored.Blake3Hash, recomputed, StringComparison.OrdinalIgnoreCase);

		_logger.LogInformation(
			"Integrity check for VirtualFile {Id}: {Result}",
			virtualFileId,
			match ? "PASSED" : "FAILED"
		);

		return match;
	}
}
