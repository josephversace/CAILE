using IIM.Ingestion.Models;
using IIM.Ingestion.Services;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

public sealed class DocumentExtractionRouter
{
	private readonly IFastTextExtractor _fast; // PdfPig
	private readonly KreuzbergExtractionService _kreuzberg;
	private readonly DoclingExtractionService _docling;
	private readonly CaileConfig _config;
	private readonly ILogger<DocumentExtractionRouter> _logger;

	public DocumentExtractionRouter(
		IFastTextExtractor fast,
		KreuzbergExtractionService kreuzberg,
		DoclingExtractionService docling,
		CaileConfig config,
		ILogger<DocumentExtractionRouter> logger)
	{
		_fast = fast;
		_kreuzberg = kreuzberg;
		_docling = docling;
		_config = config;
		_logger = logger;
	}

	public async Task<ExtractedDocument> ExtractAsync(
		byte[] bytes,
		string fileName,
		string mimeType,
		CancellationToken ct)
	{
		// ─────────────────────────────────────────────────────────────
		// 1️⃣ Tier 0/1 — Fast, in-process (PdfPig)
		// ─────────────────────────────────────────────────────────────
		var fast = await _fast.TryExtractAsync(bytes, fileName, mimeType, ct);
		
		if (fast is not null && IsGoodEnough(fast.Text))
		{
			_logger.LogDebug("Fast extraction (PdfPig) succeeded for {File}", fileName);
			return fast;
		}

		// ─────────────────────────────────────────────────────────────
		// 2️⃣ Tier 2 — Kreuzberg (preferred Docker path)
		// ─────────────────────────────────────────────────────────────
		if (_config.Kreuzberg.Preferred)
		{
			try
			{
				var k = await _kreuzberg.ExtractAsync(bytes, fileName, mimeType, ct);
				if (IsGoodEnough(k.Text))
				{
					return k with { UsedFallback = true };
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Kreuzberg failed for {File}", fileName);
			}
		}

		// ─────────────────────────────────────────────────────────────
		// 3️⃣ Tier 3 — Docling (last resort)
		// ─────────────────────────────────────────────────────────────
		_logger.LogInformation("Escalating to Docling for {File}", fileName);

		var d = await _docling.ExtractAsync(bytes, fileName, mimeType, ct);
		return d with { UsedFallback = true };
	}

	// Cheap, deterministic quality gate
	private static bool IsGoodEnough(string text)
	{
		if (string.IsNullOrWhiteSpace(text)) return false;
		if (text.Length < 500) return false;

		var printableRatio =
			text.Count(c => !char.IsControl(c)) / (double)text.Length;

		return printableRatio > 0.95;
	}
}
