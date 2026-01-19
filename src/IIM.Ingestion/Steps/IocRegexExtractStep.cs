using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Ingestion.Services;

public sealed class IocRegexExtractStep : IIngestionStep
{
	public string Id => IngestionStepIds.IocRegexExtract;
	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => new[]
	{
		IngestionStepIds.DocExtractText,
		IngestionStepIds.ExcelCanonicalize,
		IngestionStepIds.AiImageDescribe
	};
	public bool IsFatal => false;

	public bool RequiresBytes => false;

	public async ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		var textHash = await StepIO.GetBestTextHashAsync(ctx, ct) ?? ctx.StoredFile.Blake3Hash;
		return (textHash, "v1");
	}

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		if (!ctx.TryGetExtractedText(out var text))
		{
			const string skip = "{\"skipped\":\"no_text\"}";
			return ("no-text", skip);
		}


		var textHash = await StepIO.GetBestTextHashAsync(ctx, ct);
		if (string.IsNullOrWhiteSpace(textHash))
			return (null, "{\"skipped\":true,\"reason\":\"no_text_source\"}");

	
		if (string.IsNullOrWhiteSpace(text))
			return (null, "{\"skipped\":true,\"reason\":\"missing_text_blob\"}");

		// Optional: reuse shape if already computed
		DocumentShapeResult shape;
		if (ctx.Bag.TryGetValue("document_shape", out var s) && s is DocumentShapeResult cached)
		{
			shape = cached;
		}
		else
		{
			shape = ctx.ShapeDetector.Detect(text);
			ctx.Bag["document_shape"] = shape;
		}

		var extraction = ctx.IndicatorExtractor.Extract(text, shape);

		var json = JsonSerializer.Serialize(extraction);
		var outHash = StepIO.HashUtf8(ctx.Hasher, json);

		await StepIO.EnsureDerivedAsync(
			ctx.Files,
			outHash,
			Encoding.UTF8.GetBytes(json),
			ct);

		await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = ctx.StoredFile.Blake3Hash,
			DerivedHash = outHash,
			ProcessorName = "RegExtraction",
			ProcessorKind = "extraction",
			ProcessorVersion = Version,
			ParametersHash = "v1",
			MetadataJson = JsonSerializer.Serialize(new
			{
				sourceTextHash = textHash,
				shape = shape.Shapes.ToString(),
				confidence = shape.Confidence
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
