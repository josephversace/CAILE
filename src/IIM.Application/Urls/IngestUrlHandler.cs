using System.Text;
using System.Text.Json;
using Blake3;
using IIM.Ingestion.Services;          // IngestionRunOptions, IIngestionRunner, StepIO
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using SmartReader;

namespace IIM.Application.Urls;

public record IngestUrlCommand(
	string Url,
	Guid WorkspaceId,
	IngestionRunOptions? Options = null
) : IRequest<IngestUrlResult>;

public class IngestUrlHandler : IRequestHandler<IngestUrlCommand, IngestUrlResult>
{
	private readonly IFileStore _fileStore;
	private readonly IWorkspaceManager _workspaces;
	private readonly IPlaywrightService _playwright;
	private readonly IDoclingService _docling;
	private readonly IIngestionRunner _runner;
	private readonly DocumentShapeDetector _documentShapeDetector = new();

	private readonly IAriaSnapshotParser _ariaSnapshotParser;
	private readonly ICanonicalDocumentBuilder _canonicalDocumentBuilder;
	private readonly ILogger<IngestUrlHandler> _logger;

	public IngestUrlHandler(
		IFileStore fileStore,
		IWorkspaceManager workspaces,
		IPlaywrightService playwright,
		IDoclingService docling,
		IIngestionRunner runner,
		IAriaSnapshotParser ariaSnapshotParser,
		ICanonicalDocumentBuilder canonicalDocumentBuilder,
		ILogger<IngestUrlHandler> logger)
	{
		_fileStore = fileStore;
		_workspaces = workspaces;
		_playwright = playwright;
		_docling = docling;
		_runner = runner;

		_ariaSnapshotParser = ariaSnapshotParser;
		_canonicalDocumentBuilder = canonicalDocumentBuilder;
		_logger = logger;
	}

	public async Task<IngestUrlResult> Handle(IngestUrlCommand request, CancellationToken ct)
	{
		Article? article = null;
		WebCaptureResult? capture = null;
		string extractionMethod = "none";

		// ──────────────────────────────────────────────────────────────
		// 1) CAPTURE (Playwright -> SmartReader fallback)
		// ──────────────────────────────────────────────────────────────
		try
		{
			capture = await _playwright.CaptureAsync(request.Url, true, ct);

			if (!string.IsNullOrWhiteSpace(capture?.RawHtml))
			{
				article = Reader.ParseArticle(request.Url, capture.RawHtml);
				extractionMethod = "playwright";
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Playwright capture failed for {Url}, trying direct fetch", request.Url);
		}

		if (article == null || !article.IsReadable)
		{
			try
			{
				article = await Reader.ParseArticleAsync(request.Url);

				if (capture == null)
				{
					capture = new WebCaptureResult("", "", article.Content ?? "", article.Title ?? "");
					extractionMethod = "smartreader";
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Direct fetch also failed for {Url}", request.Url);
			}
		}

		if (capture == null || string.IsNullOrWhiteSpace(capture.RawHtml))
		{
			_logger.LogError("Failed to capture content for URL: {Url}", request.Url);
			return new IngestUrlResult(false, null) { Error = "Failed to capture content from the URL." };
		}

		// ──────────────────────────────────────────────────────────────
		// 2) STORE RAW HTML AS A STORED FILE (quarantine/{hash})
		// ──────────────────────────────────────────────────────────────
		var htmlBytes = Encoding.UTF8.GetBytes(capture.RawHtml);
		string storedHash = Hasher.Hash(htmlBytes).ToString();

		if (!await _fileStore.ExistsAsync("quarantine", storedHash, ct))
		{
			await using var ms = new MemoryStream(htmlBytes, writable: false);
			await _fileStore.WriteAsync("quarantine", storedHash, ms, ct);

			await _workspaces.CreateStoredFileAsync(new StoredFile
			{
				Bucket = "quarantine",
				Blake3Hash = storedHash,
				MimeType = "text/html",
				FileSize = htmlBytes.LongLength
			}, ct);

			_logger.LogInformation("New web resource stored: {Hash} (Source: {Url})",
				storedHash, request.Url);
		}

		// ──────────────────────────────────────────────────────────────
		// 3) CREATE VIRTUAL FILE
		// ──────────────────────────────────────────────────────────────
		string safeTitle = string.IsNullOrWhiteSpace(capture.PageTitle)
			? new Uri(request.Url).Host
			: SanitizeFileName(capture.PageTitle);

		string fileName = $"{safeTitle}_{DateTime.UtcNow:yyyyMMdd}.md";

		var vf = await _workspaces.CreateVirtualFileAsync(new VirtualFile
		{
			WorkspaceId = request.WorkspaceId,
			FileName = fileName,
			StoredFileHash = storedHash,
			CreatedAt = DateTime.UtcNow,
			Status = Shared.Enums.FileUploadStatus.Completed,
			CustomMetadata = new Dictionary<string, string>
			{
				{ "OriginalUrl", request.Url },
				{ "Title", safeTitle ?? "Untitled" },
				{ "MimeType", "text/html" },
				{ "IngestedAt", DateTime.UtcNow.ToString("o") }
			}
		}, ct);

		// ──────────────────────────────────────────────────────────────
		// 4) OPTIONAL SCREENSHOT (if provided)
		// NOTE: your current code had this reversed + used UTF8 bytes.
		// Playwright screenshots are typically base64 PNG/JPG.
		// ──────────────────────────────────────────────────────────────
		if (!string.IsNullOrWhiteSpace(capture.Screenshot))
		{
			byte[] screenshotBytes;

			try
			{
				screenshotBytes = Convert.FromBase64String(capture.Screenshot);
			}
			catch
			{
				// if it's not base64, fall back to utf8 (still better than crashing)
				screenshotBytes = Encoding.UTF8.GetBytes(capture.Screenshot);
			}

			string screenshotHash = Hasher.Hash(screenshotBytes).ToString();

			await EnsureDerivedAsync(_fileStore, screenshotHash, screenshotBytes, ct);

			await _workspaces.AddProcessedFileAsync(new Shared.Models.ProcessedFile
			{
				StoredFileHash = storedHash,
				DerivedHash = screenshotHash,
				ProcessorName = "WebCapture.Screenshot",
				ProcessorKind = "capture",
				ProcessorVersion = "1.0",
				ProcessedAt = DateTimeOffset.UtcNow,
				MetadataJson = JsonSerializer.Serialize(new
				{
					SourceUrl = request.Url,
					Title = capture.PageTitle,
					Format = "unknown" // png/jpg if you want to detect
				})
			}, ct);

			vf.CustomMetadata["ScreenshotHash"] = screenshotHash;
			await _workspaces.UpdateVirtualFileAsync(vf, ct);
		}


		DocumentShapeResult shapeResult = _documentShapeDetector.Detect(capture.RawHtml);

		// ──────────────────────────────────────────────────────────────
		// 5) CANONICALIZE TO MARKDOWN (URL-SPECIFIC LOGIC STAYS HERE)
		// ──────────────────────────────────────────────────────────────
		var ariaTree = !string.IsNullOrWhiteSpace(capture.AriaSnapshot)
			? _ariaSnapshotParser.Parse(capture.AriaSnapshot)
			: null;

		DoclingDocument? doclingResult = null;
		await using (var ms = new MemoryStream(htmlBytes, writable: false))
		{
			doclingResult = await _docling.ParseAsync(ms, "webpage.html", ct);
		}

	
		var canonical = _canonicalDocumentBuilder.Build(
			request.Url,
			capture.PageTitle ?? article?.Title,
			article,
			ariaTree,
			shapeResult,
			doclingResult
		);



		var finalMarkdown = canonical.Markdown;
		if (string.IsNullOrWhiteSpace(finalMarkdown))
			return new IngestUrlResult(false, vf.Id) { Error = "Canonical markdown was empty." };

		// ──────────────────────────────────────────────────────────────
		// 6) WRITE MARKDOWN TO DERIVED + CREATE PROCESSEDFILE(TextExtraction)
		// This is the handshake point into the step runner.
		// ──────────────────────────────────────────────────────────────
		var mdBytes = Encoding.UTF8.GetBytes(finalMarkdown);
		string markdownHash = Hasher.Hash(mdBytes).ToString();

		await EnsureDerivedAsync(_fileStore, markdownHash, mdBytes, ct);

		await _workspaces.AddProcessedFileAsync(new Shared.Models.ProcessedFile
		{
			StoredFileHash = storedHash,
			DerivedHash = markdownHash,
			ProcessorName = "WebMarkdown",
			ProcessorKind = "extraction",
			ProcessorVersion = "web-canonical-v1",
			ProcessedAt = DateTimeOffset.UtcNow,
			MetadataJson = JsonSerializer.Serialize(new
			{
				SourceUrl = request.Url,
				Extraction = extractionMethod,
				Title = capture.PageTitle ?? article?.Title,
				DerivedMimeType = "text/markdown"
			})
		}, ct);

		// ──────────────────────────────────────────────────────────────
		// 7) HAND OFF TO RUNNER (NO CHUNK/QDRANT/PIPELINE HERE)
		// Default if null; and we can set a URL-friendly default plan if caller didn’t specify.
		// ──────────────────────────────────────────────────────────────
		var runOptions = request.Options ?? new IngestionRunOptions
		{
			// For URLs we already created TextExtraction, so we don’t need DocExtractTextStep.
			OnlySteps = new[]
			{
				IngestionStepIds.IocRegexExtract,
				IngestionStepIds.ChunkBuild,
				IngestionStepIds.EmbedIndexQdrant,

                // optional: AI after embedding (or remove if you want it off by default)
                IngestionStepIds.AiTextAnalysis
			},
			IncludeDependencies = true,
			ContinueOnError = true,
			Force = false
		};

		await _runner.RunAsync(vf.Id, runOptions, ct);

		return new IngestUrlResult(true, vf.Id);
	}

	private static async Task EnsureDerivedAsync(
	IFileStore files,
	string hash,
	byte[] bytes,
	CancellationToken ct)
	{
		if (await files.ExistsAsync("derived", hash, ct))
			return;

		await using var ms = new MemoryStream(bytes, writable: false);
		await files.WriteAsync("derived", hash, ms, ct);
	}


	private static string SanitizeFileName(string name)
	{
		var invalidChars = Path.GetInvalidFileNameChars();
		var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
		return sanitized.Length > 100 ? sanitized[..100].Trim() : sanitized.Trim();
	}
}
