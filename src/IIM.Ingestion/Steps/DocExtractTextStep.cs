using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services;

public sealed class DocExtractTextStep : IIngestionStep
{
	private readonly DocumentExtractionRouter _router;

	public DocExtractTextStep(DocumentExtractionRouter router)
	{
		_router = router;
	}

	public string Id => IngestionStepIds.DocExtractText;
	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => [];
	public bool IsFatal => false;

	public bool RequiresBytes => true;

	public ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
		=> ValueTask.FromResult((ctx.StoredFile.Blake3Hash, (string?)"v1"));

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		var stored = ctx.StoredFile;
		var vf = ctx.VirtualFile;

		// No-op for images (AI steps later)
		if (StepIO.IsImage(stored.MimeType))
		{
			return (null, JsonSerializer.Serialize(new { skipped = true, reason = "image" }));
		}

		// Excel steps later (structure + canonical). For now: skip extraction here.
		if (StepIO.IsXlsx(stored.MimeType))
		{
			return (null, JsonSerializer.Serialize(new { skipped = true, reason = "xlsx" }));
		}

		string? extractedText = await ExtractTextAsync(ctx, ct);

		if (string.IsNullOrWhiteSpace(extractedText))
		{
			return (null, JsonSerializer.Serialize(new { skipped = true, reason = "no_text" }));
		}

		extractedText = StepIO.NormalizeExtractedText(extractedText);
		
		extractedText = StepIO.NormalizeLineBreaks(extractedText);

		ctx.Bag["extracted_text"] = extractedText;

		var textHash = StepIO.HashUtf8(ctx.Hasher, extractedText);

		// Store derived text blob
		await WriteDerivedUtf8IfMissingAsync(ctx, textHash, extractedText, ct).ConfigureAwait(false);


		// Store ProcessedFile
		await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = stored.Blake3Hash,
			DerivedHash = textHash,
			ProcessorName = "TextExtraction",
			ProcessorKind = "extraction",
			ProcessorVersion = Version,
			ProcessedAt = DateTimeOffset.UtcNow,
			MetadataJson = JsonSerializer.Serialize(new
			{
				engine = stored.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ? "raw" : "router",
				file = vf.FileName,
				mime = stored.MimeType,
				chars = extractedText.Length
			})
		}, ct);

		// same-run hint for downstream steps
		ctx.Bag["text.hash"] = textHash;

		return (textHash, JsonSerializer.Serialize(new { ok = true, derived = textHash }));
	}

	public async Task<bool> VerifyAsync(IngestionStepContext ctx, string? outputHash, CancellationToken ct)
		=> !string.IsNullOrWhiteSpace(outputHash) && await ctx.Files.ExistsAsync("derived", outputHash, ct);

	private async Task<string?> ExtractTextAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		var stored = ctx.StoredFile;
		var vf = ctx.VirtualFile;

		if (stored.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
			return Encoding.UTF8.GetString(ctx.Bytes);

		// Your old rule: pdf + officedocument go through router
		if (stored.MimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
			stored.MimeType.Contains("officedocument", StringComparison.OrdinalIgnoreCase))
		{
			var doc = await _router.ExtractAsync(ctx.Bytes, vf.FileName, stored.MimeType, ct);
			ctx.Logger.LogInformation("Document extracted using {Engine} (fallback={Fallback})", doc.Engine, doc.UsedFallback);
			return doc.Text;
		}

		ctx.Logger.LogInformation("Unsupported type {Mime}; metadata-only ingestion.", stored.MimeType);
		return null;
	}

	private static async Task<bool> WriteDerivedUtf8IfMissingAsync(IngestionStepContext ctx, string hash, string text, CancellationToken ct)
	{
		if (await ctx.Files.ExistsAsync("derived", hash, ct))
			return false;

		await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text), writable: false);
		await ctx.Files.WriteAsync("derived", hash, ms, ct);
		return true;
	}
}
