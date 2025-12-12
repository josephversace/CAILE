using System.Text;
using IIM.Ingestion.Interfaces;
using IIM.Ingestion.Models;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

public sealed class IngestionPipeline : IIngestionPipeline
{
	private readonly IWorkspaceManager _workspace;
	private readonly IFileStore _files;
	private readonly IDoclingService _docling;
	private readonly IMultimodalVisionService _vision;
	private readonly IGraphRagPipeline _graphRag;
	private readonly IEmbeddingService _embedding;
	private readonly IQdrantService _qdrant;
	private readonly ILogger<IngestionPipeline> _logger;

	public IngestionPipeline(
		IWorkspaceManager workspace,
		IFileStore files,
		IDoclingService docling,
		IMultimodalVisionService vision,
		IGraphRagPipeline graphRag,
		IEmbeddingService embedding,
		IQdrantService qdrant,
		ILogger<IngestionPipeline> logger)
	{
		_workspace = workspace;
		_files = files;
		_docling = docling;
		_vision = vision;
		_graphRag = graphRag;
		_embedding = embedding;
		_qdrant = qdrant;
		_logger = logger;
	}

	public async Task<IngestionResult> IngestAsync(Guid virtualFileId, CancellationToken ct)
	{
		// ------------------------------------------------------------
		// 1. Load VirtualFile + StoredFile
		// ------------------------------------------------------------
		var vf = await _workspace.GetVirtualFileByIdAsync(virtualFileId, ct)
			?? throw new InvalidOperationException($"VirtualFile {virtualFileId} not found.");

		var stored = vf.StoredFile
			?? throw new InvalidOperationException("StoredFile missing.");

		_logger.LogInformation("Ingesting {FileName}", vf.FileName);

		var bytes = await _files.ReadAsync(stored.StoragePath, ct);
		using var stream = new MemoryStream(bytes);

		// ------------------------------------------------------------
		// 2. File classification (Magika already ran earlier)
		// ------------------------------------------------------------
		var mime = stored.MimeType;

		string? extractedText = null;

		// ------------------------------------------------------------
		// 3. Route extraction
		// ------------------------------------------------------------
		if (mime.StartsWith("image/"))
		{
			extractedText = await HandleImageAsync(bytes, ct);
		}
		else if (mime == "application/pdf" || mime.Contains("officedocument"))
		{
			extractedText = await HandleDocumentAsync(stream, vf.FileName, ct);
		}
		else if (mime.StartsWith("text/"))
		{
			extractedText = Encoding.UTF8.GetString(bytes);
		}
		else
		{
			_logger.LogInformation("Unsupported type {Mime}; metadata-only ingestion.", mime);
			return new IngestionResult { CompletedAt = DateTime.UtcNow, StoredId=stored.Blake3Hash };
		}

		if (string.IsNullOrWhiteSpace(extractedText))
		{
			_logger.LogWarning("No extractable text.");
			return new IngestionResult { CompletedAt = DateTime.UtcNow, StoredId = stored.Blake3Hash };
		}

		// ------------------------------------------------------------
		// 4. GraphRAG extraction (Neo4j)
		// ------------------------------------------------------------
		using var textStream = new MemoryStream(Encoding.UTF8.GetBytes(extractedText));

		var graphInput = new DocumentInput(vf.FileName, textStream);
		var graphResult = await _graphRag.ProcessAsync([graphInput]);

		_logger.LogInformation(
			"GraphRAG: {Chunks} chunks, {Entities} entities",
			graphResult.TextUnits.Count,
			graphResult.Entities.Count);

		// ------------------------------------------------------------
		// 5. Embeddings + Qdrant
		// ------------------------------------------------------------
		int vectorCount = 0;

		if (_embedding.IsReady && graphResult.TextUnits.Count > 0)
		{
			var texts = graphResult.TextUnits.Select(t => t.Text).ToList();
			var vectors = await _embedding.EmbedAsync(texts, ct);

			//await _qdrant.UpsertChunksAsync(
			//	workspaceId: vf.WorkspaceId,
			//	virtualFileId: vf.Id,
			//	storedFileHash: stored.Blake3Hash,
			//	fileName: vf.FileName,
			//	mimeType: stored.MimeType,
			//	chunks: graphResult.TextUnits.Select(t => (t.Id, t.Text)).ToList(),
			//	vectors: vectors,
			//	ct: ct);

			vectorCount = vectors.Count;
		}

		return new IngestionResult
		{
			StoredId = stored.Blake3Hash,
			ChunkCount = graphResult.TextUnits.Count,
			EntityCount = graphResult.Entities.Count,
			VectorCount = vectorCount
		};
	}

	// ------------------------------------------------------------
	// ROUTERS
	// ------------------------------------------------------------

	private async Task<string?> HandleDocumentAsync(
		Stream stream,
		string fileName,
		CancellationToken ct)
	{
		return await _docling.ParseAsync(stream, fileName, ct)
			.ContinueWith(t => t.Result.Markdown, ct);
	}

	private async Task<string?> HandleImageAsync(
		byte[] bytes,
		CancellationToken ct)
	{
		if (!_vision.IsReady)
			return null;

		return await _vision.AnalyzeImageAsync(
			"Extract all visible text and investigative details.",
			bytes,
			ct);
	}
}
