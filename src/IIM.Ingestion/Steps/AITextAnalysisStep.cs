using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;

namespace IIM.Ingestion.Services;

public sealed class AiTextAnalysisStep : IIngestionStep
{
	public string Id => IngestionStepIds.AiTextAnalysis;
	public string Version => "1.0";
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


		const string promptPreamble = @"
You are a forensic analyst examining a document. Analyze this document thoroughly and provide your findings in the following structure:

## 1. Document Classification
- **Type**: (e.g., Police Report, Financial Record, Email Thread, Chat Log, Legal Document, Intelligence Report, ESP/CyberTipline Report, etc.)
- **Source**: Identify the originating organization/system if apparent
- **Date Range**: Any dates mentioned or time period covered
- **Classification/Sensitivity**: Note any markings or implied sensitivity level

## 2. Executive Summary
Provide a 2-3 sentence overview of what this document contains and its significance.

## 3. Key Entities Identified
Extract and categorize:
- **People**: Names, roles, relationships, identifying information
- **Organizations**: Companies, agencies, platforms mentioned
- **Locations**: Addresses, cities, countries, IP geolocations
- **Accounts/Identifiers**: Usernames, email addresses, phone numbers, IPs, device IDs
- **Financial**: Account numbers, transactions, amounts, cryptocurrency addresses

## 4. Timeline of Events
List key events in chronological order with dates/times if available.

## 5. Critical Findings
What are the 3-5 most important facts or findings an investigator should know immediately?

## 6. Red Flags & Anomalies
Note any inconsistencies, suspicious patterns, or items requiring follow-up.

## 7. Investigative Leads
Suggest 3-5 specific next steps or pivot points for further investigation.

## 8. Related Indicators (IoCs)
List any technical indicators that should be searched/correlated:
- IP addresses
- Domains/URLs
- Email addresses
- Hashes
- Usernames across platforms

Analyze the following document:

---
";


		var chatClient = await ctx.AgentFactory.GetChatClientAsync();
		var modelName = ctx.AgentFactory.CurrentChatModel;

		var response = await chatClient.GetResponseAsync(
			new List<ChatMessage>
			{
			new(ChatRole.User, promptPreamble + textForAnalysis)
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
