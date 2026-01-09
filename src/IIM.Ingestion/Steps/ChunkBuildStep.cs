using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Ingestion.Chunking;
using IIM.Ingestion.Models;
using IIM.Shared.Models;

namespace IIM.Ingestion.Services;

public sealed class ChunkBuildStep : IIngestionStep
{
	public string Id => IngestionStepIds.ChunkBuild;
	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => new[]
	{
		IngestionStepIds.DocExtractText,
		IngestionStepIds.ExcelCanonicalize,
		IngestionStepIds.AiImageDescribe
	};
	public bool IsFatal => true;

	public bool RequiresBytes => true;

	public async ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		var textHash = await StepIO.GetBestTextHashAsync(ctx, ct) ?? ctx.StoredFile.Blake3Hash;
		return (textHash, "v1");
	}

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		var textHash = await StepIO.GetBestTextHashAsync(ctx, ct);
		if (string.IsNullOrWhiteSpace(textHash))
			return (null, "{\"skipped\":true,\"reason\":\"no_text_source\"}");

		var text = await StepIO.ReadDerivedTextAsync(ctx.Files, textHash, ct);
		if (string.IsNullOrWhiteSpace(text))
			return (null, "{\"skipped\":true,\"reason\":\"missing_text_blob\"}");

		var shape = ctx.ShapeDetector.Detect(text);

		var options = ChunkingStrategyFactory.SelectOptionsForShape(shape) with
		{
			FileName = ctx.VirtualFile.FileName,
			MimeType = ctx.StoredFile.MimeType,
			Blake3Hash = ctx.StoredFile.Blake3Hash
		};

		var chunking = ctx.ChunkingFactory.Chunk(text, shape, options);

		// Store bounded chunk metadata in derived (NOT all text duplicated)
		var chunkMeta = new
		{
			sourceTextHash = textHash,
			shape = shape.Shapes.ToString(),
			confidence = shape.Confidence,
			strategy = chunking.StrategyName,
			count = chunking.Chunks.Count,
			// Minimal chunk descriptors (no full text)
			chunks = chunking.Chunks.Select(c => new
			{
				c.Index,
				len = c.Text?.Length ?? 0,
				c.SectionPath,
				c.ParentSection,
				contentType = c.ContentType.ToString()
			}).ToList()
		};

		var json = JsonSerializer.Serialize(chunkMeta);
		var outHash = StepIO.HashUtf8(ctx.Hasher, json);
		await StepIO.EnsureDerivedAsync(ctx.Files, outHash, Encoding.UTF8.GetBytes(json), ct);

		// Save ProcessedFile row
		await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = ctx.StoredFile.Blake3Hash,
			DerivedHash = outHash,
			ProcessorName = "ChunkBuild",
			ProcessorKind = "extraction",
			ProcessorVersion = Version,
			ParametersHash = "v1",
			MetadataJson = JsonSerializer.Serialize(new
			{
				sourceTextHash = textHash,
				count = chunking.Chunks.Count,
				strategy = chunking.StrategyName
			})
		}, ct);

		// Also stash chunking result in-run to avoid recompute in Embed step (same run only)
		ctx.Bag["chunking"] = chunking;
		ctx.Bag["shape"] = shape;
		ctx.Bag["sourceTextHash"] = textHash;

		return (outHash, "{\"status\":\"ok\"}");
	}

	public async Task<bool> VerifyAsync(IngestionStepContext ctx, string? outputHash, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(outputHash)) return true;
		return await ctx.Files.ExistsAsync(StepIO.DerivedCollection, outputHash, ct);
	}
}
