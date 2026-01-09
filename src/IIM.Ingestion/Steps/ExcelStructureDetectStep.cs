using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Ingestion.Services;

public sealed class ExcelStructureDetectStep : IIngestionStep
{
	public string Id => IngestionStepIds.ExcelStructureDetect;
	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => Array.Empty<string>();
	public bool IsFatal => true;

	public bool RequiresBytes => true;

	public ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		// Identity: file content + detector params
		return ValueTask.FromResult((ctx.StoredFile.Blake3Hash, "v1"));
	}

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		if (!StepIO.IsXlsx(ctx.StoredFile.MimeType))
			return (null, "{\"skipped\":true,\"reason\":\"not_xlsx\"}");

		await using var ms = new MemoryStream(ctx.Bytes, writable: false);
		var result = ctx.ExcelDetector.Detect(ms, ctx.VirtualFile.FileName, options: null, auditSink: null, cancellationToken: ct);

		var json = JsonSerializer.Serialize(result);
		var outHash = StepIO.HashUtf8(ctx.Hasher, json);

		await StepIO.EnsureDerivedAsync(ctx.Files, outHash, Encoding.UTF8.GetBytes(json), ct);

		await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = ctx.StoredFile.Blake3Hash,
			DerivedHash = outHash,
			ProcessorName = "ExcelStructure",
			ProcessorKind = "extraction",
			ProcessorVersion = Version,
			ParametersHash = "v1",
			MetadataJson = JsonSerializer.Serialize(new
			{
				file = ctx.VirtualFile.FileName,
				mime = ctx.StoredFile.MimeType
			})
		}, ct);

		return (outHash, "{\"status\":\"ok\"}");
	}

	public async Task<bool> VerifyAsync(IngestionStepContext ctx, string? outputHash, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(outputHash)) return true; // no-op cases
		return await ctx.Files.ExistsAsync(StepIO.DerivedCollection, outputHash, ct);
	}
}
