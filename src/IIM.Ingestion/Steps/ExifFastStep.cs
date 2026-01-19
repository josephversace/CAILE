using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;

namespace IIM.Ingestion.Services;

public sealed class MetaExifFastStep : IIngestionStep
{
	public string Id => IngestionStepIds.MetaExifFast;
	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => Array.Empty<string>();
	public bool IsFatal => false;

	public bool RequiresBytes => true;

	public ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		// Fix: Explicitly cast or define the tuple to match the interface's nullability/naming
		(string InputHash, string? ParametersHash) result = (ctx.StoredFile.Blake3Hash, "fast");
		return ValueTask.FromResult(result);
	}

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		var stored = ctx.StoredFile;

		var exif = await ctx.ExifTool.RunAsync(
			ctx.Bytes,
			ctx.VirtualFile.FileName,
			stored.Blake3Hash,
			ExifToolProfile.Fast,
			ct);

		string json = exif?.RawJson is not null
			? JsonSerializer.Serialize(exif.RawJson)
			: JsonSerializer.Serialize(new { status = "unavailable" });

		var outHash = StepIO.HashUtf8(ctx.Hasher, json);
		await StepIO.EnsureDerivedAsync(ctx.Files, outHash, Encoding.UTF8.GetBytes(json), ct);

		await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = stored.Blake3Hash,
			DerivedHash = outHash,
			ProcessorName = "Exif",
			ProcessorKind = "extraction",
			ProcessorVersion = Version,
			ParametersHash = "fast",
			MetadataJson = JsonSerializer.Serialize(new
			{
				file = ctx.VirtualFile.FileName,
				mime = stored.MimeType
			})
		}, ct);

		return (outHash, "{\"status\":\"ok\"}");
	}

	public async Task<bool> VerifyAsync(IngestionStepContext ctx, string? outputHash, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(outputHash)) return true;
		return await ctx.Files.ExistsAsync(StepIO.DerivedCollection, outputHash, ct);
	}
}
