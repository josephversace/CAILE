using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Configuration;
using Microsoft.Extensions.AI;

namespace IIM.Ingestion.Services;

public sealed class AiTextAnalysisStep : IIngestionStep
{
	public PromptResolver PromptResolver { get; init; }
	public IPromptSnapshotProvider PromptSnapshotProvider { get; init; }

	public AiTextAnalysisStep(PromptResolver promptResolver, IPromptSnapshotProvider promptSnapshot)
	{
		PromptResolver = promptResolver;
		PromptSnapshotProvider = promptSnapshot;

	}
	public string Id => IngestionStepIds.AiTextAnalysis;
	public string Version => "1.0";

	private const string TextPromptKey = "analysis.text.default";
	public IReadOnlyList<string> DependsOn => new[]
	{
		IngestionStepIds.DocExtractText,
		IngestionStepIds.ExcelCanonicalize
	};
	public bool IsFatal => false;

	public bool RequiresBytes => false;

	public async ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		// Use best available *document* text (not image analysis)
		if (StepIO.IsImage(ctx.StoredFile.MimeType))
			return (ctx.StoredFile.Blake3Hash, "prompt:v1");

		var textHash = await StepIO.GetBestTextHashAsync(ctx, ct) ?? ctx.StoredFile.Blake3Hash;
		return (textHash, "prompt:v1");
	}

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		if (StepIO.IsImage(ctx.StoredFile.MimeType))
			return (null, "{\"skipped\":true,\"reason\":\"image\"}");

		// Identity already guaranteed text exists; execution just consumes it
		var text = await ctx.GetExtractedTextAsync(ct);
		if (string.IsNullOrWhiteSpace(text))
			return (null, "{\"skipped\":true,\"reason\":\"missing_text\"}");

		const int maxChars = 100_000;
		var truncated = text.Length > maxChars;
		var textForAnalysis =
			truncated ? text[..maxChars] + "\n\n[TRUNCATED]" : text;

		var snapshot = await PromptSnapshotProvider.GetSnapshotAsync(ct: ct);

		var resolvedPrompt = PromptResolver.Resolve(
			snapshot: snapshot,
			explicitPrompt: null,
			overrideKey: null,
			defaultKey: TextPromptKey
		);


		var chatClient = await ctx.AgentFactory.GetChatClientAsync();
		var modelName = ctx.AgentFactory.CurrentChatModel;

		var response = await chatClient.GetResponseAsync(
			new List<ChatMessage>
			{
			new(ChatRole.System, resolvedPrompt.Content),
						new(ChatRole.User, textForAnalysis)
			},
			cancellationToken: ct);

		if (string.IsNullOrWhiteSpace(response?.Text))
			return (null, "{\"skipped\":true,\"reason\":\"no_model_output\"}");

		var analysisText = response.Text!;
		var outHash = StepIO.HashUtf8(ctx.Hasher, analysisText);

		await StepIO.EnsureDerivedAsync(
			ctx.Files,
			outHash,
			Encoding.UTF8.GetBytes(analysisText),
			ct);

		// 🔑 identity text hash (for traceability)
		var (textHash, _) = await GetIdentityAsync(ctx, ct);

		await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = ctx.StoredFile.Blake3Hash,
			DerivedHash = outHash,
			ProcessorName = "TextAnalysis",
			ProcessorKind = "classification",
			ProcessorVersion = Version,
			ParametersHash = "prompt:v1",
			MetadataJson = JsonSerializer.Serialize(new
			{
				model = modelName,
				sourceTextHash = textHash,
				documentLength = text.Length,
				wasTruncated = truncated
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
