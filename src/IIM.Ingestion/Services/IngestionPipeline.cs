using System.Text;
using IIM.Ingestion.Extensions;
using IIM.Ingestion.Interfaces;
using IIM.Ingestion.Models;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services;

public sealed class IngestionPipeline : IIngestionPipeline
{
	private readonly IWorkspaceManager _workspace;
	private readonly IFileStore _files;
	private readonly IDoclingService _docling;
	private readonly IMultimodalVisionService _vision;
	private readonly IGraphRagPipeline? _graphRag;
	private readonly IEmbeddingService _embedding;
	private readonly IQdrantService _qdrant;
	private readonly DocumentExtractionRouter _documentRouter;
	private readonly CaileConfig _caileConfig;
	private readonly ILogger<IngestionPipeline> _logger;

	public IngestionPipeline(
		IWorkspaceManager workspace,
		IFileStore files,
		IDoclingService docling,
		IMultimodalVisionService vision,
		IGraphRagPipeline? graphRag,
		IEmbeddingService embedding,
		DocumentExtractionRouter documentRouter,
		IQdrantService qdrant,
		CaileConfig caileConfig,
		ILogger<IngestionPipeline> logger)
	{
		_workspace = workspace;
		_files = files;
		_docling = docling;
		_vision = vision;
		_graphRag = graphRag;
		_embedding = embedding;
		_qdrant = qdrant;
		_caileConfig = caileConfig;
		_documentRouter = documentRouter;
		_logger = logger;
	}

	public async Task<IngestionResult> IngestAsync(Guid virtualFileId, CancellationToken ct)
	{
		// 1. Load VirtualFile + StoredFile
		var vf = await _workspace.GetVirtualFileByIdAsync(virtualFileId, ct)
			?? throw new InvalidOperationException($"VirtualFile {virtualFileId} not found.");

		var stored = vf.StoredFile
			?? throw new InvalidOperationException("StoredFile missing.");

		var blake3Hash = stored.Blake3Hash;

		_logger.LogInformation("Ingesting {FileName} [{Hash}]", vf.FileName, blake3Hash[..12]);

		// 2. Check if already indexed (dedup at vector level)
		if (await _qdrant.ExistsAsync(blake3Hash, ct))
		{
			_logger.LogInformation(
				"Hash {Hash} already embedded. Attaching to workspace.",
				blake3Hash[..12]);

			await _qdrant.AttachFileToExistingChunksAsync(
				blake3Hash,
				vf.WorkspaceId,
				vf.Id,
				ct);

			return new IngestionResult
			{
				StoredId = blake3Hash,
				Deduplicated = true,
				CompletedAt = DateTime.UtcNow
			};
		}

		var bytes = await _files.ReadAsync(stored.Bucket, stored.StoragePath, ct);

		// 3. Extract text based on mime type
		var extractedText = await ExtractTextAsync(bytes, vf.FileName, stored.MimeType, ct);

		if (string.IsNullOrWhiteSpace(extractedText))
		{
			_logger.LogWarning("No extractable text for {FileName}", vf.FileName);

			return new IngestionResult
			{
				CompletedAt = DateTime.UtcNow,
				StoredId = blake3Hash
			};
		}

		// 4. CRITICAL PATH: Vector indexing (must succeed)
		var vectorResult = await IndexVectorsAsync(
			blake3Hash,
			extractedText,
			vf,
			stored.MimeType,
			ct);

		// 5. BEST-EFFORT: Knowledge graph extraction (failure doesn't block ingestion)
		GraphExtractionResult? graphResult = null;

		if (_graphRag != null)
		{
			graphResult = await TryExtractKnowledgeGraphAsync(
				blake3Hash,
				extractedText,
				vf,
				ct);
		}

		return new IngestionResult
		{
			StoredId = blake3Hash,
			ChunkCount = vectorResult.ChunkCount,
			VectorCount = vectorResult.VectorCount,
			EntityCount = graphResult?.EntityCount ?? 0,
			RelationshipCount = graphResult?.RelationshipCount ?? 0,
			GraphExtractionFailed = graphResult == null && _graphRag != null,
			CompletedAt = DateTime.UtcNow
		};
	}

	// ────────────────────────────────────────────────────────────────
	// TEXT EXTRACTION
	// ────────────────────────────────────────────────────────────────

	private async Task<string?> ExtractTextAsync(
		byte[] bytes,
		string fileName,
		string mimeType,
		CancellationToken ct)
	{
		if (mimeType.StartsWith("image/"))
		{
			return await HandleImageAsync(bytes, ct);
		}

		if (mimeType == "application/pdf" || mimeType.Contains("officedocument"))
		{
			var extracted = await _documentRouter.ExtractAsync(bytes, fileName, mimeType, ct);

			_logger.LogInformation(
				"Document extracted using {Engine} (fallback={Fallback})",
				extracted.Engine,
				extracted.UsedFallback);

			return extracted.Text;
		}

		if (mimeType.StartsWith("text/"))
		{
			return Encoding.UTF8.GetString(bytes);
		}

		_logger.LogInformation("Unsupported type {Mime}; metadata-only ingestion.", mimeType);
		return null;
	}

	private async Task<string?> HandleImageAsync(byte[] bytes, CancellationToken ct)
	{
		if (!_vision.IsReady)
			return null;

		return await _vision.AnalyzeImageAsync(
			"Extract all visible text and investigative details.",
			bytes,
			ct);
	}

	// ────────────────────────────────────────────────────────────────
	// VECTOR INDEXING (Critical Path)
	// ────────────────────────────────────────────────────────────────

	private async Task<VectorIndexResult> IndexVectorsAsync(
		string blake3Hash,
		string text,
		VirtualFile vf,
		string mimeType,
		CancellationToken ct)
	{
		if (!_embedding.IsReady)
		{
			_logger.LogWarning("Embedding service not ready, skipping vector indexing");
			return new VectorIndexResult { ChunkCount = 0, VectorCount = 0 };
		}

		// Chunk with semantic awareness
		var chunks = ChunkText(text, blake3Hash, vf.FileName, mimeType);

		if (chunks.Count == 0)
		{
			return new VectorIndexResult { ChunkCount = 0, VectorCount = 0 };
		}

		var embeddings = await _embedding.EmbedAsync(chunks, ct);

		var chunkData = chunks.Zip(embeddings, (chunk, embedding) => new ChunkData
		{
			ChunkIndex = chunk.ChunkIndex,
			Embedding = embedding,
			Text = chunk.Text,
			Metadata = new ChunkMetadata
			{
				// Use the properties that ChunkMetadata actually has
				Entities = null, // Will be populated by entity linking later
				IndexedAt = DateTimeOffset.UtcNow
			}
		}).ToList();

		await _qdrant.StoreChunksAsync(blake3Hash, chunkData, ct);

		_logger.LogInformation(
			"Stored {Count} vectors for hash {Hash}",
			chunkData.Count,
			blake3Hash[..12]);

		return new VectorIndexResult
		{
			ChunkCount = chunks.Count,
			VectorCount = chunkData.Count
		};
	}

	// ────────────────────────────────────────────────────────────────
	// KNOWLEDGE GRAPH EXTRACTION (Best-Effort)
	// ────────────────────────────────────────────────────────────────

	private async Task<GraphExtractionResult?> TryExtractKnowledgeGraphAsync(
		string blake3Hash,
		string text,
		VirtualFile vf,
		CancellationToken ct)
	{
		try
		{
			// GraphRAG now handles Neo4j storage internally
			var graphResult = await _graphRag!.ProcessAsync(
				documents: [new DocumentInput(vf.FileName, text)],
				documentId: blake3Hash,
				workspaceId: vf.WorkspaceId,
				virtualFileId: vf.Id,
				fileName: vf.FileName,
				_caileConfig.GraphRag.ToGraphRagConfig(),
				ct);

			if (graphResult.IsEmpty)
			{
				_logger.LogDebug("No entities extracted for {Hash}", blake3Hash[..12]);
				return null;
			}

			_logger.LogInformation(
				"GraphRAG completed for {Hash}: {Entities} entities, {Relationships} relationships, {Neo4jNodes} Neo4j nodes",
				blake3Hash[..12],
				graphResult.Entities.Count,
				graphResult.Relationships.Count,
				graphResult.Neo4jNodeCount);

			// Queue post-processing for chunk-entity linking (eventual consistency)
			await QueueEntityLinkingAsync(blake3Hash, vf.WorkspaceId, ct);

			return new GraphExtractionResult
			{
				EntityCount = graphResult.Entities.Count,
				RelationshipCount = graphResult.Relationships.Count
			};
		}
		catch (OperationCanceledException)
		{
			throw; // Don't swallow cancellation
		}
		catch (Exception ex)
		{
			_logger.LogWarning(
				ex,
				"Graph extraction failed for {Hash}. Vector indexing succeeded, graph enrichment skipped.",
				blake3Hash[..12]);

			return null;
		}
	}

	/// <summary>
	/// Queue a background job to link entities to chunks.
	/// This runs AFTER both Qdrant and Neo4j have their data,
	/// maintaining eventual consistency without blocking ingestion.
	/// </summary>
	private Task QueueEntityLinkingAsync(
		string blake3Hash,
		Guid workspaceId,
		CancellationToken ct)
	{
		// TODO: Implement with your job queue (Hangfire, etc.)
		// The job would:
		// 1. Fetch entities from Neo4j for this hash
		// 2. Fetch chunks from Qdrant for this hash
		// 3. For each chunk, find which entities are mentioned (text matching or NER)
		// 4. Update Qdrant payloads with entity references
		// 5. Update Neo4j with MENTIONED_IN relationships

		_logger.LogDebug(
			"Queued entity linking for {Hash} in workspace {Workspace}",
			blake3Hash[..12],
			workspaceId);

		return Task.CompletedTask;
	}

	// ────────────────────────────────────────────────────────────────
	// CHUNKING (Semantic-Aware)
	// ────────────────────────────────────────────────────────────────

	/// <summary>
	/// Semantic-aware text chunking that handles:
	/// - Paragraph boundaries
	/// - Section headers
	/// - List structures
	/// - Tables (preserve as units)
	/// - Abbreviations (don't split on "Dr." etc.)
	/// </summary>
	private List<EmbeddingWorkItem> ChunkText(
		string text,
		string blake3Hash,
		string fileName,
		string mimeType,
		int targetChunkSize = 512,
		int maxChunkSize = 1024,
		int overlapTokens = 50)
	{
		var chunks = new List<EmbeddingWorkItem>();

		if (string.IsNullOrWhiteSpace(text))
			return chunks;

		// Step 1: Split into semantic blocks (paragraphs, sections)
		var blocks = SplitIntoSemanticBlocks(text);

		// Step 2: Merge small blocks, split large ones
		var normalizedBlocks = NormalizeBlockSizes(blocks, targetChunkSize, maxChunkSize);

		// Step 3: Create chunks with overlap
		var chunkIndex = 0;
		string? previousBlockTail = null;

		foreach (var block in normalizedBlocks)
		{
			var chunkText = block;

			// Add overlap from previous chunk
			if (previousBlockTail != null && overlapTokens > 0)
			{
				chunkText = previousBlockTail + " " + chunkText;
			}

			chunks.Add(new EmbeddingWorkItem
			{
				Blake3Hash = blake3Hash,
				ChunkIndex = chunkIndex++,
				Text = chunkText.Trim(),
				MaxTokens = 256,
				SemanticType = DetectSemanticType(block),
				Metadata = new Dictionary<string, string>
				{
					["file_name"] = fileName,
					["mime_type"] = mimeType
				}
			});

			// Store tail for next chunk's overlap
			previousBlockTail = GetOverlapTail(block, overlapTokens);
		}

		return chunks;
	}

	// ... (all the chunking helper methods remain unchanged) ...

	private static List<string> SplitIntoSemanticBlocks(string text)
	{
		var blocks = new List<string>();
		var paragraphs = text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);

		foreach (var para in paragraphs)
		{
			var trimmed = para.Trim();
			if (string.IsNullOrEmpty(trimmed))
				continue;

			if (LooksLikeTable(trimmed))
			{
				blocks.Add(trimmed);
				continue;
			}

			if (LooksLikeList(trimmed))
			{
				blocks.Add(trimmed);
				continue;
			}

			var sentences = SplitIntoSentences(trimmed);
			blocks.AddRange(sentences);
		}

		return blocks;
	}

	private static List<string> SplitIntoSentences(string text)
	{
		var sentences = new List<string>();
		var abbreviations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"mr", "mrs", "ms", "dr", "prof", "sr", "jr",
			"vs", "etc", "inc", "ltd", "corp",
			"jan", "feb", "mar", "apr", "jun", "jul", "aug", "sep", "oct", "nov", "dec",
			"st", "ave", "blvd", "rd",
			"fig", "no", "vol", "pp", "ed", "eds",
			"i.e", "e.g", "cf", "al"
		};

		var current = new StringBuilder();
		var i = 0;

		while (i < text.Length)
		{
			var c = text[i];
			current.Append(c);

			if (c == '.' || c == '!' || c == '?')
			{
				var wordBefore = GetWordBefore(text, i);
				var isAbbreviation = abbreviations.Contains(wordBefore.TrimEnd('.'));
				var nextChar = i + 1 < text.Length ? text[i + 1] : '\0';
				var isEndOfSentence = char.IsWhiteSpace(nextChar) &&
									   (i + 2 >= text.Length || char.IsUpper(text[i + 2]));

				if (!isAbbreviation && (isEndOfSentence || c == '!' || c == '?'))
				{
					var sentence = current.ToString().Trim();
					if (!string.IsNullOrEmpty(sentence))
					{
						sentences.Add(sentence);
					}
					current.Clear();
				}
			}

			i++;
		}

		var remaining = current.ToString().Trim();
		if (!string.IsNullOrEmpty(remaining))
		{
			sentences.Add(remaining);
		}

		return sentences;
	}

	private static string GetWordBefore(string text, int position)
	{
		var end = position;
		var start = position;

		while (start > 0 && text[start - 1] != ' ')
		{
			start--;
		}

		return text[start..(end + 1)];
	}

	private static List<string> NormalizeBlockSizes(
		List<string> blocks,
		int targetSize,
		int maxSize)
	{
		var normalized = new List<string>();
		var buffer = new StringBuilder();

		foreach (var block in blocks)
		{
			if (block.Length > maxSize)
			{
				if (buffer.Length > 0)
				{
					normalized.Add(buffer.ToString().Trim());
					buffer.Clear();
				}

				normalized.AddRange(SplitLargeBlock(block, targetSize));
				continue;
			}

			if (buffer.Length + block.Length + 1 > targetSize && buffer.Length > 0)
			{
				normalized.Add(buffer.ToString().Trim());
				buffer.Clear();
			}

			if (buffer.Length > 0)
				buffer.Append(' ');
			buffer.Append(block);
		}

		if (buffer.Length > 0)
		{
			normalized.Add(buffer.ToString().Trim());
		}

		return normalized;
	}

	private static List<string> SplitLargeBlock(string block, int targetSize)
	{
		var result = new List<string>();
		var words = block.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var current = new StringBuilder();

		foreach (var word in words)
		{
			if (current.Length + word.Length + 1 > targetSize && current.Length > 0)
			{
				result.Add(current.ToString().Trim());
				current.Clear();
			}

			if (current.Length > 0)
				current.Append(' ');
			current.Append(word);
		}

		if (current.Length > 0)
		{
			result.Add(current.ToString().Trim());
		}

		return result;
	}

	private static bool LooksLikeTable(string text)
	{
		var lines = text.Split('\n');
		if (lines.Length < 2) return false;

		var pipeCount = lines.Count(l => l.Contains('|'));
		if (pipeCount > lines.Length / 2) return true;

		var tabCount = lines.Count(l => l.Contains('\t'));
		if (tabCount > lines.Length / 2) return true;

		return false;
	}

	private static bool LooksLikeList(string text)
	{
		var lines = text.Split('\n');
		if (lines.Length < 2) return false;

		var listMarkers = lines.Count(l =>
		{
			var trimmed = l.TrimStart();
			return trimmed.StartsWith("- ") ||
				   trimmed.StartsWith("* ") ||
				   trimmed.StartsWith("• ") ||
				   (trimmed.Length > 2 && char.IsDigit(trimmed[0]) && trimmed[1] == '.');
		});

		return listMarkers > lines.Length / 2;
	}

	private static string? GetOverlapTail(string text, int targetTokens)
	{
		if (string.IsNullOrEmpty(text) || targetTokens <= 0)
			return null;

		var targetChars = targetTokens * 4;

		if (text.Length <= targetChars)
			return text;

		var startPos = text.Length - targetChars;
		var spacePos = text.IndexOf(' ', startPos);

		if (spacePos > 0 && spacePos < text.Length)
		{
			return text[(spacePos + 1)..];
		}

		return text[startPos..];
	}

	private static string DetectSemanticType(string text)
	{
		if (LooksLikeTable(text)) return "table";
		if (LooksLikeList(text)) return "list";
		if (text.Length < 100) return "short";
		return "prose";
	}

	// ────────────────────────────────────────────────────────────────
	// RESULT TYPES
	// ────────────────────────────────────────────────────────────────

	private record VectorIndexResult
	{
		public int ChunkCount { get; init; }
		public int VectorCount { get; init; }
	}

	private record GraphExtractionResult
	{
		public int EntityCount { get; init; }
		public int RelationshipCount { get; init; }
	}
}