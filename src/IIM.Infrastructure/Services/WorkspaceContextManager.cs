// src/IIM.Infrastructure/Services/WorkspaceContextManager.cs
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
	private readonly ILogger<WorkspaceContextManager> _logger;

	public WorkspaceContextManager(
		IQdrantService qdrant,
		IGraphStore graph,
		IWorkspaceManager workspace,
		IEmbeddingService embedding,
		ILogger<WorkspaceContextManager> logger)
	{
		_qdrant = qdrant;
		_graph = graph;
		_workspace = workspace;
		_embedding = embedding;
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
		List<SemanticChunk> newChunks = [];
		List<EntitySummary> newEntities = [];
		List<RelationshipSummary> newRelationships = [];
		List<TimelineEventSummary> timeline = [];


		if (plan.UseDeterministicSection)
		{
			var fileHash = fileHashes.FirstOrDefault();

			var result = await _workspace.GetMetadataJsonAsync(
				fileHash!,
				processorName: "TextExtraction",
				latestOnly: true,
				ct);

			string metadata = result[0] ?? string.Empty;

			if (!string.IsNullOrWhiteSpace(metadata))
			{
				newChunks.Add(new SemanticChunk(fileHash, -1,  metadata, 1.0f));
			}
		}


		// ════════════════════════════════════════════════════════
		// 1. QDRANT: Always search, filter out cached chunks
		// ════════════════════════════════════════════════════════
		List<SemanticChunk> allRelevantChunks = [];

		if (plan.UseQdrant && plan.QdrantTopK > 0)
		{
			if (fileHashes.Count > 0)
			{
				allRelevantChunks = await FetchSemanticChunksForFilesAsync(fileHashes, userQuery, plan.QdrantTopK, ct);
			}
			else if (workspaceId != Guid.Empty)
			{
				allRelevantChunks = await FetchSemanticChunksForWorkspaceAsync(workspaceId, userQuery, plan.QdrantTopK, ct);
			}

			// Filter to only NEW chunks
			newChunks = allRelevantChunks
				.Where(c => !cache.Chunks.Contains(ChunkKey(c)))
				.ToList();

			_logger.LogDebug(
				"Qdrant: {Total} relevant, {New} new, {Cached} cached",
				allRelevantChunks.Count, newChunks.Count,
				allRelevantChunks.Count - newChunks.Count);
		}

		// ════════════════════════════════════════════════════════
		// 2. NEO4J ENTITIES: Get from chunks, filter out cached
		// ════════════════════════════════════════════════════════
		if (plan.UseNeo4j && plan.IncludeEntities)
		{
			List<EntitySummary> allRelevantEntities = [];

			// Get entities linked to ALL relevant chunks (not just new ones)
			// This ensures we find entities even if chunk was cached
			if (allRelevantChunks.Count > 0 && ChunksHaveEntityLinks(allRelevantChunks))
			{
				allRelevantEntities = await FetchEntitiesFromChunksAsync(allRelevantChunks, ct);
			}
			else if (fileHashes.Count > 0)
			{
				allRelevantEntities = await FetchEntitiesForFilesAsync(fileHashes, ct);
			}
			else if (workspaceId != Guid.Empty)
			{
				allRelevantEntities = await FetchEntitiesForWorkspaceAsync(workspaceId, ct);
			}

			// Filter to only NEW entities
			newEntities = allRelevantEntities
				.Where(e => !cache.Entities.Contains(e.Id))
				.ToList();

			_logger.LogDebug(
				"Neo4j entities: {Total} relevant, {New} new, {Cached} cached",
				allRelevantEntities.Count, newEntities.Count,
				allRelevantEntities.Count - newEntities.Count);
		}

		// ════════════════════════════════════════════════════════
		// 3. NEO4J RELATIONSHIPS: Between relevant entities, filter cached
		// ════════════════════════════════════════════════════════
		if (plan.UseNeo4j && plan.IncludeRelationships)
		{
			// Get relationships between ALL relevant entities (cached + new)
			// Need to combine cached entity IDs with new ones
			var allRelevantEntityIds = cache.Entities
				.Concat(newEntities.Select(e => e.Id))
				.Distinct()
				.ToList();

			if (allRelevantEntityIds.Count > 0)
			{
				var allRelevantRelationships = await FetchRelationshipsBetweenEntitiesAsync(
					allRelevantEntityIds, ct);

				// Filter to only NEW relationships
				newRelationships = allRelevantRelationships
					.Where(r => !cache.Relationships.Contains(RelationshipKey(r)))
					.ToList();

				_logger.LogDebug(
					"Neo4j relationships: {Total} relevant, {New} new, {Cached} cached",
					allRelevantRelationships.Count, newRelationships.Count,
					allRelevantRelationships.Count - newRelationships.Count);
			}
		}

		// ════════════════════════════════════════════════════════
		// 4. TIMELINE: Only on first request (no caching needed)
		// ════════════════════════════════════════════════════════
		if (plan.IncludeTimeline && workspaceId != Guid.Empty &&
			cache.Chunks.Count == 0) // First request
		{
			timeline = await FetchTimelineAsync(workspaceId, ct);
		}

		var context = new WorkspaceContext
		{
			WorkspaceId = workspaceId,
			Intent = intent,
			SemanticChunks = newChunks,
			Entities = newEntities,
			Relationships = newRelationships,
			Timeline = timeline,
			NewChunkIds = newChunks.Select(ChunkKey).ToList(),
			NewEntityIds = newEntities.Select(e => e.Id).ToList(),
			NewRelationshipIds = newRelationships.Select(RelationshipKey).ToList(),
			TotalTokenEstimate = EstimateTokens(newChunks, newEntities, newRelationships, timeline)
		};

		_logger.LogInformation(
			"Context built: {Chunks} new chunks, {Entities} new entities, {Rels} new relationships",
			newChunks.Count, newEntities.Count, newRelationships.Count);

		return context;
	}

	private static string ChunkKey(SemanticChunk c) => $"{c.Blake3Hash}:{c.ChunkIndex}";

	private static string RelationshipKey(RelationshipSummary r) =>
		$"{r.SourceId}-[{r.Type}]->{r.TargetId}";



	private async Task<string> BuildDeterministicFileContextAsync(
	string fileHash,
	CancellationToken ct)
	{
		var metadata = await _workspace.GetMetadataJsonAsync(
			fileHash,
			processorName: "TextExtraction",
			latestOnly: true,
			ct);

		if (metadata.Count == 0)
			return string.Empty;

		// For now: metadata JSON already includes preview / stats
		// You will later swap this for the full extracted text
		return metadata[0];
	}



	// ════════════════════════════════════════════════════════════════
	// QDRANT: Semantic Chunk Retrieval
	// ════════════════════════════════════════════════════════════════

	private async Task<List<SemanticChunk>> FetchSemanticChunksForFilesAsync(
		IReadOnlyList<string> fileHashes,
		string query,
		int topK,
		CancellationToken ct)
	{
		if (!_embedding.IsReady || string.IsNullOrWhiteSpace(query) || fileHashes.Count == 0)
			return [];

		var queryVector = await EmbedQueryAsync(query, ct);
		if (queryVector == null)
			return [];

		// Use balanced search that handles multi-file distribution
		var hits = await _qdrant.SearchByHashesBalancedAsync(
			queryVector,
			fileHashes.ToList(),
			topK,
			minPerFile: 2,
			ct);

		_logger.LogDebug(
			"Retrieved {Count} chunks across {Files} files",
			hits.Count,
			hits.Select(h => h.Blake3Hash).Distinct().Count());

		return hits.Select(h => new SemanticChunk(
			h.Blake3Hash,
			h.ChunkIndex,
			h.Text,
			h.Score,
			h.FileName,
			h.EntityIds
		)).ToList();
	}

	private async Task<List<SemanticChunk>> FetchSemanticChunksForWorkspaceAsync(
		Guid workspaceId,
		string query,
		int topK,
		CancellationToken ct)
	{
		if (!_embedding.IsReady || string.IsNullOrWhiteSpace(query))
			return [];

		var queryVector = await EmbedQueryAsync(query, ct);
		if (queryVector == null)
			return [];

		// Get all file hashes for workspace
		var files = await _workspace.GetVirtualFilesByWorkspaceAsync(workspaceId, ct);
		var hashes = files
			.Where(f => f.StoredFile != null)
			.Select(f => f.StoredFile!.Blake3Hash)
			.Distinct()
			.ToList();

		if (hashes.Count == 0)
		{
			_logger.LogDebug("No files found in workspace {WorkspaceId}", workspaceId);
			return [];
		}

		var hits = await _qdrant.SearchByHashesAsync(queryVector, hashes, topK, ct);

		return hits.Select(h => new SemanticChunk(
			h.Blake3Hash,
			h.ChunkIndex,
			h.Text,
			h.Score,
			h.FileName,
			h.EntityIds
		)).ToList();
	}

	private async Task<float[]?> EmbedQueryAsync(string query, CancellationToken ct)
	{
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

	private static bool ChunksHaveEntityLinks(List<SemanticChunk> chunks)
	{
		return chunks.Any(c => c.EntityIds?.Count > 0);
	}

	// ════════════════════════════════════════════════════════════════
	// NEO4J: Entity Retrieval
	// ════════════════════════════════════════════════════════════════

	private async Task<List<EntitySummary>> FetchEntitiesFromChunksAsync(
		List<SemanticChunk> chunks,
		CancellationToken ct)
	{
		var entityIds = chunks
			.Where(c => c.EntityIds != null)
			.SelectMany(c => c.EntityIds!)
			.Distinct()
			.Take(50)
			.ToHashSet();

		if (entityIds.Count == 0)
			return [];

		var result = new List<EntitySummary>();
		var options = new GraphTraversalOptions { Take = 500 };

		await foreach (var node in _graph.GetNodesAsync(options, ct))
		{
			if (!entityIds.Contains(node.Id))
				continue;

			if (IsStructuralNode(node.Label))
				continue;

			result.Add(CreateEntitySummary(node));

			// Stop if we've found all requested entities
			if (result.Count >= entityIds.Count)
				break;
		}

		return result;
	}

	private async Task<List<EntitySummary>> FetchEntitiesForFilesAsync(
		IReadOnlyList<string> fileHashes,
		CancellationToken ct)
	{
		var result = new List<EntitySummary>();
		var seenIds = new HashSet<string>();

		foreach (var hash in fileHashes)
		{
			// Get entities via MENTIONS relationships from document nodes
			await foreach (var rel in _graph.GetOutgoingRelationshipsAsync(hash, ct))
			{
				if (rel.Type != "MENTIONS")
					continue;

				if (seenIds.Contains(rel.TargetId))
					continue;

				// Fetch the entity node
				var entity = await FindNodeByIdAsync(rel.TargetId, ct);
				if (entity != null && !IsStructuralNode(entity.Label))
				{
					result.Add(CreateEntitySummary(entity));
					seenIds.Add(entity.Id);
				}

				// Cap at 50 entities per file scope
				if (result.Count >= 50)
					break;
			}

			if (result.Count >= 50)
				break;
		}

		return result;
	}

	private async Task<List<EntitySummary>> FetchEntitiesForWorkspaceAsync(
		Guid workspaceId,
		CancellationToken ct)
	{
		var result = new List<EntitySummary>();
		var workspaceIdStr = workspaceId.ToString();
		var options = new GraphTraversalOptions { Take = 200 };

		await foreach (var node in _graph.GetNodesAsync(options, ct))
		{
			if (IsStructuralNode(node.Label))
				continue;

			// Filter by workspace_id property if present
			if (node.Properties.TryGetValue("workspace_id", out var wsId))
			{
				if (wsId?.ToString() != workspaceIdStr)
					continue;
			}

			result.Add(CreateEntitySummary(node));
		}

		return result;
	}

	private async Task<GraphNode?> FindNodeByIdAsync(string nodeId, CancellationToken ct)
	{
		var options = new GraphTraversalOptions { Take = 1000 };

		await foreach (var node in _graph.GetNodesAsync(options, ct))
		{
			if (node.Id == nodeId)
				return node;
		}

		return null;
	}

	// ════════════════════════════════════════════════════════════════
	// NEO4J: Relationship Retrieval
	// ════════════════════════════════════════════════════════════════

	private async Task<List<RelationshipSummary>> FetchRelationshipsBetweenEntitiesAsync(
		List<string> entityIds,
		CancellationToken ct)
	{
		var result = new List<RelationshipSummary>();
		var entityIdSet = entityIds.ToHashSet();
		var options = new GraphTraversalOptions { Take = 500 };

		await foreach (var rel in _graph.GetRelationshipsAsync(options, ct))
		{
			// Only include relationships where both endpoints are in our entity set
			if (!entityIdSet.Contains(rel.SourceId) || !entityIdSet.Contains(rel.TargetId))
				continue;

			// Skip structural relationships
			if (IsStructuralRelationship(rel.Type))
				continue;

			result.Add(new RelationshipSummary(
				rel.SourceId,
				rel.TargetId,
				rel.Type,
				rel.Properties
			));
		}

		return result;
	}

	// ════════════════════════════════════════════════════════════════
	// POSTGRESQL: Timeline Retrieval
	// ════════════════════════════════════════════════════════════════

	private async Task<List<TimelineEventSummary>> FetchTimelineAsync(
		Guid workspaceId,
		CancellationToken ct)
	{
		var events = await _workspace.GetWorkspaceTimelineAsync(workspaceId, ct);

		return events
			.OrderByDescending(e => e.Timestamp)
			.Take(20)
			.Select(e => new TimelineEventSummary(
				e.Id,
				e.Timestamp,
				e.EventType,
				e.Description
			))
			.ToList();
	}

	// ════════════════════════════════════════════════════════════════
	// HELPERS
	// ════════════════════════════════════════════════════════════════

	private static bool IsStructuralNode(string label)
	{
		return label is "Document" or "Workspace" or "Community" or "Chunk";
	}

	private static bool IsStructuralRelationship(string type)
	{
		return type is "MENTIONS" or "CONTAINS" or "CONTAINS_DOCUMENT" or "HAS_CHUNK" or "MENTIONED_IN_CHUNK";
	}

	private static EntitySummary CreateEntitySummary(GraphNode node)
	{
		var name = node.Properties.TryGetValue("title", out var title)
			? title?.ToString() ?? node.Id
			: node.Id;

		return new EntitySummary(
			node.Id,
			name,
			node.Label,
			node.Properties
		);
	}

	private static int EstimateTokens(
		List<SemanticChunk> chunks,
		List<EntitySummary> entities,
		List<RelationshipSummary> relationships,
		List<TimelineEventSummary> timeline)
	{
		// Rough estimate: ~4 chars per token
		var chunkChars = chunks.Sum(c => c.Text?.Length ?? 0);
		var entityChars = entities.Sum(e => (e.Name?.Length ?? 0) + (e.Type?.Length ?? 0) + 50);
		var relChars = relationships.Count * 80;
		var timelineChars = timeline.Sum(t => (t.Description?.Length ?? 0) + 50);

		return (chunkChars + entityChars + relChars + timelineChars) / 4;
	}
}