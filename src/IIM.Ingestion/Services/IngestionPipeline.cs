using System.IO;
using IIM.Ingestion.Interfaces;
using IIM.Ingestion.Models;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services;

public class IngestionPipeline : IIngestionPipeline
{
	private readonly IWorkspaceManager _workspace;
	private readonly IDoclingService _docling;
	private readonly IFileStore _files;
	private readonly IHashService _hashes;
	private readonly IGraphRagPipeline _graphRag;
	private readonly IQdrantService? _qdrant;
	private readonly IEmbeddingGenerator<string, Embedding<float>>? _embedding;
	private readonly ILogger<IngestionPipeline> _logger;

	public IngestionPipeline(
		IWorkspaceManager workspace,
		IDoclingService docling,
		IFileStore files,
		IHashService hashes,
		IGraphRagPipeline graphRag,
		IQdrantService qdrant,
		IEmbeddingGenerator<string, Embedding<float>>? embedding,
		ILogger<IngestionPipeline> logger)
	{
		_workspace = workspace;
		_docling = docling;
		_files = files;
		_hashes = hashes;
		_graphRag = graphRag;
		_qdrant = qdrant;
		_embedding = embedding;
		_logger = logger;
	}

	public async Task<IngestionResult> IngestAsync(Guid evidenceId, CancellationToken ct)
	{
		var evidence = await _workspace.GetVirtualFileByIdAsync(evidenceId, ct);
		if (evidence == null)
			throw new InvalidOperationException($"VirtualFile {evidenceId} not found.");

		if (evidence.StoredFile == null)
			throw new InvalidOperationException($"StoredFile missing for VirtualFile {evidenceId}.");

		_logger.LogInformation("Loading file {FileName} for ingestion.", evidence.FileName);

		var fileBytes = await _files.ReadAsync(evidence.StoredFile.StoragePath, ct);
		_logger.LogInformation("Loaded {Length} bytes from storage.", fileBytes.Length);

		using var mem = new MemoryStream(fileBytes);

		var hash = _hashes.ComputeBlake3Async(fileBytes);
		_logger.LogDebug("Computed BLAKE3 hash {Hash}.", hash);

		// 4. Docling parsing
		mem.Position = 0;
		var doclingOutput = await _docling.ParseAsync(mem, evidence.FileName, ct);
		_logger.LogInformation("Docling extracted {Pages} pages, {TextBlocks} text blocks, {Tables} tables in {Time:F2}s.",
			doclingOutput.PageCount,
			doclingOutput.TextBlockCount,
			doclingOutput.TableCount,
			doclingOutput.ProcessingTimeSeconds);

		if (!doclingOutput.IsSuccess)
		{
			_logger.LogWarning("Docling parsing had issues: {Errors}", string.Join(", ", doclingOutput.Errors));
		}

		mem.Position = 0;
		var graphInput = new DocumentInput(evidence.FileName, mem);
		var graphResult = await _graphRag.ProcessAsync([graphInput]);

		_logger.LogInformation("GraphRag produced {Chunks} chunks and {Entities} entities.",
			graphResult.TextUnits.Count, graphResult.Entities.Count);

		int vectorCount = 0;

		if (_qdrant != null && _embedding != null && graphResult.TextUnits.Count > 0)
		{
			var caseId = evidence.WorkspaceId.ToString();

			// Batch embed all chunks
			var texts = graphResult.TextUnits.Select(t => t.Text).ToList();
			var embeddings = await _embedding.GenerateAsync(texts, cancellationToken: ct);

			_logger.LogDebug("Generated {Count} embeddings.", embeddings.Count);

			// Store each chunk with its embedding
			for (int i = 0; i < graphResult.TextUnits.Count; i++)
			{
				var chunk = graphResult.TextUnits[i];
				var vector = embeddings[i].Vector.ToArray();

				await _qdrant.StoreEmbeddingAsync(
					fileId: evidenceId,
					caseId: caseId,
					chunkId: chunk.Id,
					embedding: vector,
					text: chunk.Text,
					ct: ct);

				vectorCount++;
			}

			_logger.LogInformation("Qdrant stored {Count} vectors.", vectorCount);
		}

		return new IngestionResult
		{
			EvidenceId = evidenceId,
			ChunkCount = graphResult.TextUnits.Count,
			EntityCount = graphResult.Entities.Count,
			VectorCount = vectorCount
		};
	}
}