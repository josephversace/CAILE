using IIM.Ingestion.Models;
using IIM.Ingestion.Services;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

public sealed class DocumentExtractionRouter
{
	private readonly KreuzbergExtractionService _kreuzberg;
	private readonly DoclingExtractionService _docling;
	private readonly CaileConfig _config;
	private readonly ILogger<DocumentExtractionRouter> _logger;

	public DocumentExtractionRouter(
		KreuzbergExtractionService kreuzberg,
		DoclingExtractionService docling,
		CaileConfig config,
		ILogger<DocumentExtractionRouter> logger)
	{
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

		return await _docling.ExtractAsync(bytes, fileName, mimeType, ct);

		if (!_config.Kreuzberg.Preferred)
		{
			return await _docling.ExtractAsync(bytes, fileName, mimeType, ct);
		}

		try
		{
			return await _kreuzberg.ExtractAsync(bytes, fileName, mimeType, ct);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Kreuzberg failed, falling back to Docling");
			return await _docling.ExtractAsync(bytes, fileName, mimeType, ct);
		}
	}
}
