using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Ingestion.Services;

public sealed class ExcelCanonicalizeStep : IIngestionStep
{
	public string Id => IngestionStepIds.ExcelCanonicalize;
	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => new[] { IngestionStepIds.ExcelStructureDetect };
	public bool IsFatal => true;

	public bool RequiresBytes => true;

	public async ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		if (!StepIO.IsXlsx(ctx.StoredFile.MimeType))
			return (ctx.StoredFile.Blake3Hash, "v1");

		var structureHash = await StepIO.GetLatestDerivedHashAsync(ctx.Workspace, ctx.StoredFile.Blake3Hash, "ExcelStructure", ct)
			?? ctx.StoredFile.Blake3Hash;

		return (structureHash, "v1");
	}

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		if (!StepIO.IsXlsx(ctx.StoredFile.MimeType))
			return (null, "{\"skipped\":true,\"reason\":\"not_xlsx\"}");

		var structureHash = await StepIO.GetLatestDerivedHashAsync(ctx.Workspace, ctx.StoredFile.Blake3Hash, "ExcelStructure", ct);
		if (string.IsNullOrWhiteSpace(structureHash))
			return (null, "{\"skipped\":true,\"reason\":\"missing_excel_structure\"}");

		var structureJson = await StepIO.ReadDerivedTextAsync(ctx.Files, structureHash, ct);
		if (string.IsNullOrWhiteSpace(structureJson))
			return (null, "{\"skipped\":true,\"reason\":\"missing_structure_blob\"}");

		var canonical = ctx.ExcelCanonicalizer.CanonicalizeJson(structureJson);
		var outHash = StepIO.HashUtf8(ctx.Hasher, canonical);

		await StepIO.EnsureDerivedAsync(ctx.Files, outHash, Encoding.UTF8.GetBytes(canonical), ct);

		await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = ctx.StoredFile.Blake3Hash,
			DerivedHash = outHash,
			ProcessorName = "ExcelCanonical",
			ProcessorKind = "extraction",
			ProcessorVersion = Version,
			ParametersHash = "v1",
			MetadataJson = JsonSerializer.Serialize(new
			{
				source = "ExcelStructure",
				structureHash
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
