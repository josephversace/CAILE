using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;

namespace IIM.Ingestion.Services;

public sealed class AiImageDescribeStep : IIngestionStep
{
	public string Id => IngestionStepIds.AiImageDescribe;
	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => new[] { IngestionStepIds.MetaExifFast };
	public bool IsFatal => false;

	public bool RequiresBytes => true;
	public ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		// Identity: file content + model choice/prompt version
		return ValueTask.FromResult((ctx.StoredFile.Blake3Hash, "prompt:v1"));
	}

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		if (!StepIO.IsImage(ctx.StoredFile.MimeType))
			return (null, "{\"skipped\":true,\"reason\":\"not_image\"}");

		var exifHash = await StepIO.GetLatestDerivedHashAsync(ctx.Workspace, ctx.StoredFile.Blake3Hash, "Exif", ct);
		var exifJson = await StepIO.ReadDerivedTextAsync(ctx.Files, exifHash, ct) ?? "{}";

		const string prompt = @"
Perform a forensic and investigative analysis of this image. 
Break your response into the following sections:

1. **Text/OCR Extraction**: Transcribe all visible text, including documents, signs, license plates, or screens. Note font types or hand-writing styles.
2. **Key Entities**: Identify people (clothing, identifying features), vehicles (make/model), and specific objects.
3. **Environment & Context**: Describe the setting (indoor/outdoor), lighting, weather, and any geographic clues (architecture, language on signs).
4. **Digital/Technical Artifacts**: Identify if this is a screenshot, a photo of a screen, or an original photo. Note any visible timestamps or UI elements.
5. **Investigative Leads**: List 3-5 specific details that could be used for further pivot-point analysis.";

		var chatClient = await ctx.AgentFactory.GetChatClientAsync();
		var modelName = ctx.AgentFactory.CurrentChatModel;

		var messages = new List<ChatMessage>
		{
			new(ChatRole.User, prompt),
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
