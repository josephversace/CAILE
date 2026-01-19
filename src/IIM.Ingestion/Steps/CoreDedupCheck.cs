using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services;

public sealed class CoreDedupCheckStep : IIngestionStep
{
	private readonly IQdrantService _qdrant;

	public CoreDedupCheckStep(IQdrantService qdrant) => _qdrant = qdrant;

	public string Id => IngestionStepIds.CoreDedupCheck;
	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => [];
	public bool IsFatal => false;

	public bool RequiresBytes => true;

	public ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
		=> ValueTask.FromResult((ctx.StoredFile.Blake3Hash, (string?)"v1"));

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		var blake3 = ctx.StoredFile.Blake3Hash;

		if (await _qdrant.ExistsAsync(blake3, ct))
		{
			ctx.Logger.LogInformation("Dedup hit for {Hash}. Attaching to workspace and stopping.", blake3[..12]);

			await _qdrant.AttachFileToExistingChunksAsync(blake3, ctx.VirtualFile.WorkspaceId, ctx.VirtualFile.Id, ctx.VirtualFile.FileName, ct);

			ctx.Bag["dedup"] = true;
			ctx.RequestStop();

			return ("dedup", JsonSerializer.Serialize(new { dedup = true }));
		}

		return (null, JsonSerializer.Serialize(new { dedup = false }));
	}

	public Task<bool> VerifyAsync(IngestionStepContext ctx, string? outputHash, CancellationToken ct)
		=> Task.FromResult(true); // side-effect only; skipping is fine
}
