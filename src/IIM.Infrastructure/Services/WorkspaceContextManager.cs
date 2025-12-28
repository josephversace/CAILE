// ═══════════════════════════════════════════════════════════════════════════════
// WORKSPACE CONTEXT MANAGER V2
// ═══════════════════════════════════════════════════════════════════════════════
//
// Tiered context retrieval strategy:
//   - Single small file → Full text from SeaweedFS
//   - Single large file → Semantic search within file
//   - Multiple files → Budget-aware retrieval per file
//   - Workspace → Semantic search across all files
//
// ═══════════════════════════════════════════════════════════════════════════════

using System.Text.Json;
using GraphRag.Graphs;
using IIM.Infrastructure.Data;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Services;

public sealed class WorkspaceContextManager : IWorkspaceContextManager
{
	private readonly IQdrantService _qdrant;
	private readonly IGraphStore _graph;
	private readonly IWorkspaceManager _workspace;
	private readonly IEmbeddingService _embedding;
	private readonly IFileStore _files;
	private readonly ILogger<WorkspaceContextManager> _logger;

	// Default context budgets by model capability
	private static readonly Dictionary<string, int> ModelContextBudgets = new()
	{
		["phi-4-mini"] = 3500,      // ~4k context, leave room for response
		["phi-3.5"] = 3500,
		["qwen2.5"] = 28000,        // ~32k context
		["llama-3"] = 7000,         // ~8k context
		["default"] = 6000          // Conservative default
	};

	// Reserve tokens for system prompt, response, etc.
	private const int SystemPromptReserve = 1000;
	private const int ResponseReserve = 1500;

	public WorkspaceContextManager(
		IQdrantService qdrant,
		IGraphStore graph,
		IWorkspaceManager workspace,
		IEmbeddingService embedding,
		IFileStore files,
		ILogger<WorkspaceContextManager> logger)
	{
		_qdrant = qdrant;
		_graph = graph;
		_workspace = workspace;
		_embedding = embedding;
		_files = files;
		_logger = logger;
	}

	public async Task<WorkspaceContext> BuildAsync(
		Guid workspaceId,
		IReadOnlyList<string> fileHashes,
		string userQuery,
		WorkspaceIntent intent,
		WorkspaceEvidencePlan plan,
		RetrievedContextCache cache,
		CancellationToken ct)
	{
		// Get context budget
		var budget = GetContextBudget(plan.ModelId) - SystemPromptReserve - ResponseReserve;

		_logger.LogDebug(
			"Building context with budget={Budget} tokens for {FileCount} files",
			budget, fileHashes.Count);

		// Route to appropriate strategy
		if (fileHashes.Count == 1)
		{
			return await BuildSingleFileContextAsync(
				fileHashes[0], workspaceId, userQuery, intent, plan, budget, cache, ct);
		}
		else if (fileHashes.Count > 1)
		{
			return await BuildMultiFileContextAsync(
				fileHashes, workspaceId, userQuery, intent, plan, budget, cache, ct);
		}
		else
		{
			return await BuildWorkspaceContextAsync(
				workspaceId, userQuery, intent, plan, budget, cache, ct);
		}
	}

	// ════════════════════════════════════════════════════════════════════════════
	// SINGLE FILE CONTEXT
	// ════════════════════════════════════════════════════════════════════════════

	private async Task<WorkspaceContext> BuildSingleFileContextAsync(
		string fileHash,
		Guid workspaceId,
		string userQuery,
		WorkspaceIntent intent,
		WorkspaceEvidencePlan plan,
		int budget,
		RetrievedContextCache cache,
		CancellationToken ct)
	{
		// Get document metadata
		var metadatas = await _workspace.GetMetadataJsonAsync(fileHash, "TextExtraction", true, ct);

		if (metadatas == null || !metadatas.Any())
		{

			_logger.LogWarning("No metadata found for file {Hash}, falling back to semantic search", fileHash[..12]);
			return await BuildSemanticSearchContextAsync(
				[fileHash], workspaceId, userQuery, intent, plan, budget, cache, ct);
		}

		var json = metadatas[0];


		var metadata = JsonSerializer.Deserialize<DocumentIngestionMetadata>(
			json,
			new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			}
		);

		if (metadata is null)
		{
			throw new InvalidOperationException("Failed to deserialize file metadata.");
		}


		var estimatedTokens = metadata.EstimatedTokens;

		_logger.LogDebug(
			"Single file: {Chars} chars, ~{Tokens} tokens, budget={Budget}",
			metadata.TotalChars, estimatedTokens, budget);

		// Decision: Full text or semantic search?
		if (estimatedTokens <= budget * 0.8) // Leave 20% headroom
		{
			// File fits in context - fetch full text
			return await BuildFullTextContextAsync(fileHash, metadata, workspaceId, intent, ct);
		}
		else
		{
			// File too large - use semantic search within this file
			return await BuildSemanticSearchContextAsync(
				[fileHash], workspaceId, userQuery, intent, plan, budget, cache, ct);
		}
	}

	private async Task<WorkspaceContext> BuildFullTextContextAsync(
		string fileHash,
		DocumentIngestionMetadata metadata,
		Guid workspaceId,
		WorkspaceIntent intent,
		CancellationToken ct)
	{
		// Fetch full text from SeaweedFS
		var derivedHash = await GetDerivedHashAsync(fileHash, ct);

		if (string.IsNullOrEmpty(derivedHash))
		{
			_logger.LogWarning("No derived hash found for {Hash}", fileHash[..12]);
			return EmptyContext(workspaceId, intent);
		}

		string? fullText = null;

		try
		{
			var textBytes = await _files.ReadAsync("derived", derivedHash, ct);
			fullText = System.Text.Encoding.UTF8.GetString(textBytes);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to read derived text for {Hash}", fileHash[..12]);
		}

		if (string.IsNullOrWhiteSpace(fullText))
		{
			return EmptyContext(workspaceId, intent);
		}

		_logger.LogInformation(
			"Using full text context for {Hash} ({Chars} chars)",
			fileHash[..12], fullText.Length);

		// Create a single "chunk" containing the full document
		var chunk = new SemanticChunk(
			Blake3Hash: fileHash,
			ChunkIndex: -1, // -1 indicates full document
			Text: fullText,
			Score: 1.0f,
			FileName: null, // Will be populated by caller if needed
			EntityIds: null
		);

		return new WorkspaceContext
		{
			WorkspaceId = workspaceId,
			Intent = intent,
			SemanticChunks = [chunk],
			Entities = [],
			Relationships = [],
			Timeline = [],
			RetrievalMode = "full_text",
			NewChunkIds = [$"{fileHash}:-1"],
			NewEntityIds = [],
			NewRelationshipIds = [],
			TotalTokenEstimate = metadata.EstimatedTokens
		};
	}

	// ════════════════════════════════════════════════════════════════════════════
	// MULTI-FILE CONTEXT
	// ════════════════════════════════════════════════════════════════════════════

	private async Task<WorkspaceContext> BuildMultiFileContextAsync(
		IReadOnlyList<string> fileHashes,
		Guid workspaceId,
		string userQuery,
		WorkspaceIntent intent,
		WorkspaceEvidencePlan plan,
		int budget,
		RetrievedContextCache cache,
		CancellationToken ct)
	{
		// Get metadata for all files
		var metadataByHash = new Dictionary<string, DocumentIngestionMetadata?>();
		var totalEstimatedTokens = 0;

		foreach (var hash in fileHashes)
		{
			var meta = await GetDocumentMetadataAsync(hash, ct);
			metadataByHash[hash] = meta;
			totalEstimatedTokens += meta?.EstimatedTokens ?? 0;
		}

		_logger.LogDebug(
			"Multi-file: {Count} files, ~{Tokens} total tokens, budget={Budget}",
			fileHashes.Count, totalEstimatedTokens, budget);

		// Strategy: If all files fit, use full text; otherwise semantic search
		if (totalEstimatedTokens <= budget * 0.8)
		{
			return await BuildMultiFileFullTextContextAsync(
				fileHashes, metadataByHash, workspaceId, intent, ct);
		}
		else
		{
			// Budget per file for semantic search
			var budgetPerFile = budget / fileHashes.Count;

			return await BuildSemanticSearchContextAsync(
				fileHashes, workspaceId, userQuery, intent, plan, budget, cache, ct);
		}
	}

	private async Task<WorkspaceContext> BuildMultiFileFullTextContextAsync(
		IReadOnlyList<string> fileHashes,
		Dictionary<string, DocumentIngestionMetadata?> metadataByHash,
		Guid workspaceId,
		WorkspaceIntent intent,
		CancellationToken ct)
	{
		var chunks = new List<SemanticChunk>();
		var totalTokens = 0;

		foreach (var hash in fileHashes)
		{
			var derivedHash = await GetDerivedHashAsync(hash, ct);
			if (string.IsNullOrEmpty(derivedHash)) continue;

			try
			{
				var textBytes = await _files.ReadAsync("derived", derivedHash, ct);
				var fullText = System.Text.Encoding.UTF8.GetString(textBytes);

				chunks.Add(new SemanticChunk(
					Blake3Hash: hash,
					ChunkIndex: -1,
					Text: fullText,
					Score: 1.0f,
					FileName: null,
					EntityIds: null
				));

				totalTokens += metadataByHash[hash]?.EstimatedTokens ?? (fullText.Length / 4);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to read derived text for {Hash}", hash[..12]);
			}
		}

		_logger.LogInformation(
			"Using full text context for {Count} files (~{Tokens} tokens)",
			chunks.Count, totalTokens);

		return new WorkspaceContext
		{
			WorkspaceId = workspaceId,
			Intent = intent,
			SemanticChunks = chunks,
			Entities = [],
			Relationships = [],
			Timeline = [],
			RetrievalMode = "full_text_multi",
			NewChunkIds = chunks.Select(c => $"{c.Blake3Hash}:-1").ToList(),
			NewEntityIds = [],
			NewRelationshipIds = [],
			TotalTokenEstimate = totalTokens
		};
	}

	// ════════════════════════════════════════════════════════════════════════════
	// WORKSPACE CONTEXT (no specific files selected)
	// ════════════════════════════════════════════════════════════════════════════

	private async Task<WorkspaceContext> BuildWorkspaceContextAsync(
		Guid workspaceId,
		string userQuery,
		WorkspaceIntent intent,
		WorkspaceEvidencePlan plan,
		int budget,
		RetrievedContextCache cache,
		CancellationToken ct)
	{
		// Get all file hashes in workspace
		var files = await _workspace.GetVirtualFilesByWorkspaceAsync(workspaceId, ct);
		var hashes = files
			.Where(f => f.StoredFile != null)
			.Select(f => f.StoredFile!.Blake3Hash)
			.Distinct()
			.ToList();

		if (hashes.Count == 0)
		{
			_logger.LogDebug("No files in workspace {WorkspaceId}", workspaceId);
			return EmptyContext(workspaceId, intent);
		}

		_logger.LogDebug(
			"Workspace search across {Count} files",
			hashes.Count);

		return await BuildSemanticSearchContextAsync(
			hashes, workspaceId, userQuery, intent, plan, budget, cache, ct);
	}

	// ════════════════════════════════════════════════════════════════════════════
	// SEMANTIC SEARCH CONTEXT
	// ════════════════════════════════════════════════════════════════════════════

	private async Task<WorkspaceContext> BuildSemanticSearchContextAsync(
		IReadOnlyList<string> fileHashes,
		Guid workspaceId,
		string userQuery,
		WorkspaceIntent intent,
		WorkspaceEvidencePlan plan,
		int budget,
		RetrievedContextCache cache,
		CancellationToken ct)
	{
		var chunks = new List<SemanticChunk>();
		var entities = new List<EntitySummary>();
		var relationships = new List<RelationshipSummary>();

		var semanticChunks = new List<SemanticChunk>();
		var seen = new HashSet<string>();

		void AddChunk(SemanticChunk chunk)
		{
			var key = $"{chunk.Blake3Hash}:{chunk.ChunkIndex}";
			if (seen.Add(key))
				semanticChunks.Add(chunk);
		}


		// ─────────────────────────────────────────────────────────────────────
		// 1. SEMANTIC CHUNK RETRIEVAL
		// ─────────────────────────────────────────────────────────────────────

		if (plan.UseQdrant && plan.QdrantTopK > 0 && !string.IsNullOrWhiteSpace(userQuery))
		{
			var queryVector = await EmbedQueryAsync(userQuery, ct);

			if (queryVector != null)
			{
				var hits = await _qdrant.SearchByHashesBalancedAsync(
					queryVector,
					fileHashes.ToList(),
					plan.QdrantTopK,
					minPerFile: Math.Max(1, plan.QdrantTopK / fileHashes.Count),
					ct);

				// Filter cached and convert
				var derivedHits = new List<ChunkHit>();

				foreach (var hit in hits)
				{
					var key = $"{hit.Blake3Hash}:{hit.ChunkIndex}";
					if (cache.Chunks.Contains(key))
						continue;

					if (hit.EmbeddingRole.Equals("search_only", StringComparison.OrdinalIgnoreCase))
					{
						derivedHits.Add(hit);
						continue; // never add derived to context
					}

					AddChunk(new SemanticChunk(
						hit.Blake3Hash,
						hit.ChunkIndex,
						hit.Text,
						hit.Score,
						hit.FileName,
						hit.EntityIds
					));
				}

				foreach (var derived in derivedHits)
				{
					// We need a source chunk index to expand
					if (derived.SourceChunkIndex == null)
						continue;

					// Pull neighbors from the same file
					// Radius = 1 (previous + next)
					var neighbors = await _qdrant.GetChunksByHashAsync(
						derived.Blake3Hash,
						ct);

					foreach (var n in neighbors)
					{
						if (Math.Abs(n.ChunkIndex - derived.SourceChunkIndex.Value) > 1)
							continue;

						var neighborKey = $"{derived.Blake3Hash}:{n.ChunkIndex}";
						if (cache.Chunks.Contains(neighborKey))
							continue;

						// IMPORTANT:
						// We only add authoritative chunks.
						// Derived chunks were tagged search_only and excluded earlier.
						AddChunk(new SemanticChunk(
							derived.Blake3Hash,
							n.ChunkIndex,
							n.Text,
							Score: derived.Score * 0.9f, // slight decay
							FileName: derived.FileName,
							EntityIds: n.EntityIds
						));
					}
				}


				chunks = semanticChunks;


				_logger.LogDebug(
					"Semantic search: {Total} hits, {New} new",
					hits.Count, chunks.Count);
			}
		}

		// ─────────────────────────────────────────────────────────────────────
		// 2. ENTITY RETRIEVAL (if plan includes it)
		// ─────────────────────────────────────────────────────────────────────

		if (plan.UseNeo4j && plan.IncludeEntities)
		{
			var entityIds = chunks
				.Where(c => c.EntityIds != null)
				.SelectMany(c => c.EntityIds!)
				.Distinct()
				.ToHashSet();

			if (entityIds.Count > 0)
			{
				entities = await FetchEntitiesByIdsAsync(entityIds, cache.Entities, ct);
			}
		}

		// ─────────────────────────────────────────────────────────────────────
		// 3. RELATIONSHIP RETRIEVAL (if plan includes it)
		// ─────────────────────────────────────────────────────────────────────

		if (plan.UseNeo4j && plan.IncludeRelationships && entities.Count > 0)
		{
			var entityIdSet = entities.Select(e => e.Id).ToHashSet();
			entityIdSet.UnionWith(cache.Entities);

			relationships = await FetchRelationshipsBetweenEntitiesAsync(
				entityIdSet.ToList(), cache.Relationships, ct);
		}

		// ─────────────────────────────────────────────────────────────────────
		// 4. BUDGET CHECK - Trim if needed
		// ─────────────────────────────────────────────────────────────────────

		var totalTokens = EstimateTokens(chunks, entities, relationships);

		if (totalTokens > budget)
		{
			(chunks, entities, relationships) = TrimToBudget(
				chunks, entities, relationships, budget);

			_logger.LogDebug(
				"Trimmed context from {Original} to {Trimmed} tokens",
				totalTokens, budget);
		}

		return new WorkspaceContext
		{
			WorkspaceId = workspaceId,
			Intent = intent,
			SemanticChunks = chunks,
			Entities = entities,
			Relationships = relationships,
			Timeline = [],
			RetrievalMode = "semantic_search",
			NewChunkIds = chunks.Select(c => $"{c.Blake3Hash}:{c.ChunkIndex}").ToList(),
			NewEntityIds = entities.Select(e => e.Id).ToList(),
			NewRelationshipIds = relationships.Select(r => $"{r.SourceId}-[{r.Type}]->{r.TargetId}").ToList(),
			TotalTokenEstimate = EstimateTokens(chunks, entities, relationships)
		};
	}

	// ════════════════════════════════════════════════════════════════════════════
	// HELPERS
	// ════════════════════════════════════════════════════════════════════════════

	private async Task<DocumentIngestionMetadata?> GetDocumentMetadataAsync(
		string fileHash,
		CancellationToken ct)
	{
		var metadataJsonList = await _workspace.GetMetadataJsonAsync(
			fileHash,
			processorName: "TextExtraction",
			latestOnly: true,
			ct);

		if (metadataJsonList.Count == 0 || string.IsNullOrEmpty(metadataJsonList[0]))
			return null;

		try
		{
			return JsonSerializer.Deserialize<DocumentIngestionMetadata>(metadataJsonList[0]);
		}
		catch (JsonException ex)
		{
			_logger.LogWarning(ex, "Failed to parse metadata for {Hash}", fileHash[..12]);
			return null;
		}
	}

	private async Task<string?> GetDerivedHashAsync(string fileHash, CancellationToken ct)
	{
		// Query ProcessedFile table for the derived hash
		var metadataJsonList = await _workspace.GetMetadataJsonAsync(
			fileHash,
			processorName: "TextExtraction",
			latestOnly: true,
			ct);

		// The derived hash should be stored in ProcessedFile.DerivedHash
		// We need a method to get it - for now, parse from metadata or add new method
		// This is a simplified approach - you may need to add a dedicated method
		var listHashes = await _workspace.GetDerivedHashForProcessedFile(fileHash, "TextExtraction", true, ct);

		return listHashes.FirstOrDefault();
	}

	private async Task<float[]?> EmbedQueryAsync(string query, CancellationToken ct)
	{
		if (!_embedding.IsReady)
			return null;

		var workItem = new EmbeddingWorkItem
		{
			Blake3Hash = "query",
			ChunkIndex = 0,
			Text = query,
			MaxTokens = 512,
			SemanticType = "query",
			Metadata = new Dictionary<string, string>()

		};

		var embeddings = await _embedding.EmbedAsync([workItem], ct);
		return embeddings.Count > 0 ? embeddings[0] : null;
	}

	private async Task<List<EntitySummary>> FetchEntitiesByIdsAsync(
		HashSet<string> entityIds,
		HashSet<string> cachedIds,
		CancellationToken ct)
	{
		var result = new List<EntitySummary>();
		var options = new GraphTraversalOptions { Take = 500 };

		await foreach (var node in _graph.GetNodesAsync(options, ct))
		{
			if (!entityIds.Contains(node.Id))
				continue;

			if (cachedIds.Contains(node.Id))
				continue;

			if (IsStructuralNode(node.Label))
				continue;

			result.Add(CreateEntitySummary(node));

			if (result.Count >= 50) // Cap entities
				break;
		}

		return result;
	}

	private async Task<List<RelationshipSummary>> FetchRelationshipsBetweenEntitiesAsync(
		List<string> entityIds,
		HashSet<string> cachedRelationships,
		CancellationToken ct)
	{
		var result = new List<RelationshipSummary>();
		var entityIdSet = entityIds.ToHashSet();
		var options = new GraphTraversalOptions { Take = 500 };

		await foreach (var rel in _graph.GetRelationshipsAsync(options, ct))
		{
			if (!entityIdSet.Contains(rel.SourceId) || !entityIdSet.Contains(rel.TargetId))
				continue;

			if (IsStructuralRelationship(rel.Type))
				continue;

			var key = $"{rel.SourceId}-[{rel.Type}]->{rel.TargetId}";
			if (cachedRelationships.Contains(key))
				continue;

			result.Add(new RelationshipSummary(
				rel.SourceId,
				rel.TargetId,
				rel.Type,
				rel.Properties
			));

			if (result.Count >= 30) // Cap relationships
				break;
		}

		return result;
	}

	private static int GetContextBudget(string? modelId)
	{
		if (string.IsNullOrEmpty(modelId))
			return ModelContextBudgets["default"];

		foreach (var (prefix, budget) in ModelContextBudgets)
		{
			if (modelId.Contains(prefix, StringComparison.OrdinalIgnoreCase))
				return budget;
		}

		return ModelContextBudgets["default"];
	}

	private static int EstimateTokens(
		List<SemanticChunk> chunks,
		List<EntitySummary> entities,
		List<RelationshipSummary> relationships)
	{
		var chunkTokens = chunks.Sum(c => (c.Text?.Length ?? 0) / 4);
		var entityTokens = entities.Sum(e => ((e.Name?.Length ?? 0) + (e.Type?.Length ?? 0) + 20) / 4);
		var relTokens = relationships.Count * 20; // ~80 chars per relationship / 4

		return chunkTokens + entityTokens + relTokens;
	}

	private static (List<SemanticChunk>, List<EntitySummary>, List<RelationshipSummary>) TrimToBudget(
		List<SemanticChunk> chunks,
		List<EntitySummary> entities,
		List<RelationshipSummary> relationships,
		int budget)
	{
		// Priority: chunks > entities > relationships
		var currentTokens = EstimateTokens(chunks, entities, relationships);

		// First, trim relationships
		while (currentTokens > budget && relationships.Count > 0)
		{
			relationships.RemoveAt(relationships.Count - 1);
			currentTokens = EstimateTokens(chunks, entities, relationships);
		}

		// Then, trim entities
		while (currentTokens > budget && entities.Count > 0)
		{
			entities.RemoveAt(entities.Count - 1);
			currentTokens = EstimateTokens(chunks, entities, relationships);
		}

		// Finally, trim chunks (by score - lowest first)
		var sortedChunks = chunks.OrderByDescending(c => c.Score).ToList();
		while (currentTokens > budget && sortedChunks.Count > 1)
		{
			sortedChunks.RemoveAt(sortedChunks.Count - 1);
			currentTokens = EstimateTokens(sortedChunks, entities, relationships);
		}

		return (sortedChunks, entities, relationships);
	}

	private static bool IsStructuralNode(string label) =>
		label is "Document" or "Workspace" or "Community" or "Chunk";

	private static bool IsStructuralRelationship(string type) =>
		type is "MENTIONS" or "CONTAINS" or "CONTAINS_DOCUMENT" or "HAS_CHUNK" or "MENTIONED_IN_CHUNK";

	private static EntitySummary CreateEntitySummary(GraphNode node)
	{
		var name = node.Properties.TryGetValue("title", out var title)
			? title?.ToString() ?? node.Id
			: node.Id;

		return new EntitySummary(node.Id, name, node.Label, node.Properties);
	}

	private static WorkspaceContext EmptyContext(Guid workspaceId, WorkspaceIntent intent) =>
		new()
		{
			WorkspaceId = workspaceId,
			Intent = intent,
			SemanticChunks = [],
			Entities = [],
			Relationships = [],
			Timeline = [],
			RetrievalMode = "empty",
			NewChunkIds = [],
			NewEntityIds = [],
			NewRelationshipIds = [],
			TotalTokenEstimate = 0
		};
}
