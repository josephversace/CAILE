using System.Text;
using System.Text.Json;
using Blake3;
using IIM.Ingestion.Services;
using IIM.Shared.Models;

namespace IIM.Ingestion.Steps;

public sealed class DocumentShapeDetectStep : IIngestionStep
{
	public string Id => IngestionStepIds.DocShapeDetect;
	public string Version => "1.0";

	public bool RequiresBytes => false;
	public bool IsFatal => false;

	public IReadOnlyList<string> DependsOn =>
		new[] { IngestionStepIds.DocExtractText };

	public async ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		if (ctx.Bag.TryGetValue("text.hash", out var h) && h is string textHash)
			return (textHash, Version);

		var processed = await ctx.Workspace.GetDerivedHashForProcessedFile(
			ctx.StoredFile.Blake3Hash,
			processorName: "TextExtraction",
			latestOnly: true,
			ct);

		if (!processed.Any())
			return ("no-text", Version);



		return (processed[0], Version);
	}


	public async Task<(string OutputHash, string? MetadataJson)>ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{

		var text = await ctx.GetExtractedTextAsync(ct);

		if (string.IsNullOrWhiteSpace(text))
		{
			const string skip = "{\"skipped\":\"no_text\"}";
			return ("no-text", skip);
		}

		var result = ctx.ShapeDetector.Detect(text);

		var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
		{
			WriteIndented = false
		});

		ctx.Bag["document_shape"] = result;

		var outputHash = Blake3Hex(json);

		return (outputHash, json);
	}


	public Task<bool> VerifyAsync(
		IngestionStepContext ctx,
		string outputHash,
		CancellationToken ct)
	{
		// Deterministic + cheap → no re-verification needed
		return Task.FromResult(true);
	}

	private static string Blake3Hex(string input)
	{
		using var hasher = new Blake3HashAlgorithm();
		var bytes = Encoding.UTF8.GetBytes(input);
		hasher.TransformFinalBlock(bytes, 0, bytes.Length);
		return Convert.ToHexString(hasher.Hash!).ToLowerInvariant();
	}



}
