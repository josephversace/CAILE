using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IIM.Ingestion.Models;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace IIM.Ingestion.Services
{


	public sealed class PdfPigTextExtractionService : IFastTextExtractor
	{
		private readonly ILogger<PdfPigTextExtractionService> _logger;

		public PdfPigTextExtractionService(
			ILogger<PdfPigTextExtractionService> logger)
		{
			_logger = logger;
		}

		public async Task<ExtractedDocument?> TryExtractAsync(
			byte[] bytes,
			string fileName,
			string mimeType,
			CancellationToken ct)
		{
			// PdfPig only handles PDFs
			if (!mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
				return null;

			try
			{
				// PdfPig is synchronous; keep async boundary clean
				return await Task.Run(() => Extract(bytes, fileName), ct)
								 .ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				// Fast extractor must NEVER kill ingestion
				_logger.LogDebug(ex, "PdfPig extraction failed for {File}", fileName);
				return null;
			}
		}

		private ExtractedDocument? Extract(byte[] bytes, string fileName)
		{
			using var ms = new MemoryStream(bytes, writable: false);
			using var document = PdfDocument.Open(ms);

			var sb = new StringBuilder(capacity: bytes.Length / 4);
			int pageCount = 0;
			int pagesWithText = 0;

			foreach (var page in document.GetPages())
			{
				pageCount++;

				// IMPORTANT: never use page.Text
				var text = ContentOrderTextExtractor.GetText(page);

				if (!string.IsNullOrWhiteSpace(text))
				{
					pagesWithText++;
					sb.AppendLine(text.TrimEnd());
					sb.AppendLine();
				}
			}

			var finalText = sb.ToString().Trim();

			// Hard gate — let router decide escalation
			if (finalText.Length < 256 || pagesWithText == 0)
				return null;

			return new ExtractedDocument(
				Text: finalText,
				UsedFallback: false,              // first-tier extractor
				Engine: "pdfpig",
				Metadata: new Dictionary<string, object?>
				{
					["extractor"] = "PdfPig",
					["pages_total"] = pageCount,
					["pages_with_text"] = pagesWithText,
					["file_name"] = fileName
				},
				Artifacts: null                   // no layout artifacts at fast tier
			);
		}
	}

}
