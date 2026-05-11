using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Configuration;
using Microsoft.Extensions.AI;

namespace IIM.Ingestion.Services;

public sealed class AiImageDescribeStep : IIngestionStep
{
	public PromptResolver PromptResolver { get; init; }
	public IPromptSnapshotProvider PromptSnapshotProvider { get; init; }

    public AiImageDescribeStep(PromptResolver promptResolver, IPromptSnapshotProvider promptSnapshot)
    {
		PromptResolver = promptResolver;
		PromptSnapshotProvider = promptSnapshot;
        
    }

    public string Id => IngestionStepIds.AiImageDescribe;

	private const string VisionPromptKey = "analysis.image.default";

	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => new[] { IngestionStepIds.MetaExifFast };
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
		if (!StepIO.IsImage(ctx.StoredFile.MimeType))
			return (null, "{\"skipped\":true,\"reason\":\"not_image\"}");

		var exifHash = await StepIO.GetLatestDerivedHashAsync(ctx.Workspace, ctx.StoredFile.Blake3Hash, "Exif", ct);
		var exifJson = await StepIO.ReadDerivedTextAsync(ctx.Files, exifHash, ct) ?? "{}";

		var snapshot = await PromptSnapshotProvider.GetSnapshotAsync(ct: ct);

		var resolvedPrompt = PromptResolver.Resolve(
			snapshot: snapshot,
			explicitPrompt: null,
			overrideKey: null,             
			defaultKey: VisionPromptKey
		);



		var chatClient = await ctx.AgentFactory.GetChatClientAsync();
		var modelName = ctx.AgentFactory.CurrentChatModel;

		var messages = new List<ChatMessage>
		{
			new(ChatRole.User, resolvedPrompt.Content),
			new(ChatRole.User, $"EXIF:\n{exifJson}"),
			new(ChatRole.User, new List<AIContent> { new DataContent(ctx.Bytes, ctx.StoredFile.MimeType) })
		};

		var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
		if (string.IsNullOrWhiteSpace(response?.Text))
			return (null, "{\"skipped\":true,\"reason\":\"no_model_output\"}");

		var analysisText = response.Text!;
		var outHash = StepIO.HashUtf8(ctx.Hasher, analysisText);
		await StepIO.EnsureDerivedAsync(ctx.Files, outHash, Encoding.UTF8.GetBytes(analysisText), ct);

		await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = ctx.StoredFile.Blake3Hash,
			DerivedHash = outHash,
			ProcessorName = "ImageDescription",
			ProcessorKind = "vision",
			ProcessorVersion = Version,
			ParametersHash = "prompt:v1",
			MetadataJson = JsonSerializer.Serialize(new
			{
				model = modelName,
				exifHash,
				file = ctx.VirtualFile.FileName
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
