// src/IIM.Ingestion/Services/EntityLinkingJob.cs
using GraphRag.Graphs;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services;

public sealed class EntityLinkingJob
{
	private readonly IGraphStore _graph;
	private readonly IQdrantService _qdrant;
	private readonly ILogger<EntityLinkingJob> _logger;

	public EntityLinkingJob(
		IGraphStore graph,
		IQdrantService qdrant,
		ILogger<EntityLinkingJob> logger)
	{
		_graph = graph;
		_qdrant = qdrant;
		_logger = logger;
	}

	public async Task ExecuteAsync(string blake3Hash, Guid workspaceId, CancellationToken ct)
	{
		_logger.LogInformation(
			"Starting entity linking for {Hash} in workspace {Workspace}",
			blake3Hash[..Math.Min(12, blake3Hash.Length)],
			workspaceId);

		// 1. Get entities for this document from Neo4j
		var entities = await GetEntitiesForDocumentAsync(blake3Hash, ct);
		if (entities.Count == 0)
		{
			_logger.LogDebug("No entities found for {Hash}, skipping linking", blake3Hash[..12]);
			return;
		}

		// 2. Get chunks for this document from Qdrant
		var chunks = await _qdrant.GetChunksByHashAsync(blake3Hash, ct);
		if (chunks.Count == 0)
		{
			_logger.LogDebug("No chunks found for {Hash}, skipping linking", blake3Hash[..12]);
			return;
		}

		// 3. For each chunk, find which entities are mentioned
		var chunkEntityMap = new Dictionary<int, List<string>>();

		foreach (var chunk in chunks)
		{
			var mentionedEntities = entities
				.Where(e => ChunkMentionsEntity(chunk.Text, e.Title, e.Aliases))
				.Select(e => e.Id)
				.ToList();

			if (mentionedEntities.Count > 0)
			{
				chunkEntityMap[chunk.ChunkIndex] = mentionedEntities;
			}
		}

		if (chunkEntityMap.Count == 0)
		{
			_logger.LogDebug("No entity mentions found in chunks for {Hash}", blake3Hash[..12]);
			return;
		}

		// 4. Update Qdrant payloads with entity references
		await UpdateQdrantPayloadsAsync(blake3Hash, chunkEntityMap, ct);

		// 5. Update Neo4j with MENTIONED_IN_CHUNK relationships
		await CreateChunkMentionRelationshipsAsync(blake3Hash, chunkEntityMap, entities, ct);

		_logger.LogInformation(
			"Linked {EntityCount} entities to {ChunkCount} chunks for {Hash}",
			entities.Count,
			chunkEntityMap.Count,
			blake3Hash[..12]);
	}

	// ────────────────────────────────────────────────────────────────
	// NEO4J: Get entities for document
	// ────────────────────────────────────────────────────────────────

	private async Task<List<EntityInfo>> GetEntitiesForDocumentAsync(
		string blake3Hash,
		CancellationToken ct)
	{
		var entities = new List<EntityInfo>();

		// Traverse from Document node via MENTIONS relationship
		await foreach (var rel in _graph.GetOutgoingRelationshipsAsync(blake3Hash, ct))
		{
			if (rel.Type != "MENTIONS")
				continue;

			// Get the entity node
			await foreach (var node in _graph.GetNodesAsync(new GraphTraversalOptions { Take = 1000 }, ct))
			{
				if (node.Id != rel.TargetId)
					continue;

				// Skip structural nodes
				if (node.Label is "Document" or "Workspace" or "Community")
					continue;

				var title = node.Properties.TryGetValue("title", out var t)
					? t?.ToString() ?? node.Id
					: node.Id;

				var aliases = new List<string>();
				if (node.Properties.TryGetValue("aliases", out var a) && a is IEnumerable<object> aliasList)
				{
					aliases = aliasList.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
				}

				entities.Add(new EntityInfo(node.Id, title, node.Label, aliases));
				break;
			}
		}

		return entities;
	}

	// ────────────────────────────────────────────────────────────────
	// QDRANT: Update payloads with entity_ids
	// ────────────────────────────────────────────────────────────────

	private async Task UpdateQdrantPayloadsAsync(
		string blake3Hash,
		Dictionary<int, List<string>> chunkEntityMap,
		CancellationToken ct)
	{
		foreach (var (chunkIndex, entityIds) in chunkEntityMap)
		{
			await _qdrant.UpdateChunkPayloadAsync(
				blake3Hash,
				chunkIndex,
				new Dictionary<string, object>
				{
					["entity_ids"] = entityIds,
					["entity_count"] = entityIds.Count,
					["entity_linked_at"] = DateTimeOffset.UtcNow.ToString("O")
				},
				ct);
		}
	}

	// ────────────────────────────────────────────────────────────────
	// NEO4J: Create MENTIONED_IN_CHUNK relationships
	// ────────────────────────────────────────────────────────────────

	private async Task CreateChunkMentionRelationshipsAsync(
		string blake3Hash,
		Dictionary<int, List<string>> chunkEntityMap,
		List<EntityInfo> entities,
		CancellationToken ct)
	{
		var relationships = new List<GraphRelationshipUpsert>();

		foreach (var (chunkIndex, entityIds) in chunkEntityMap)
		{
			var chunkNodeId = $"{blake3Hash}:chunk_{chunkIndex}";

			// Ensure chunk node exists
			await _graph.UpsertNodeAsync(
				id: chunkNodeId,
				label: "Chunk",
				properties: new Dictionary<string, object?>
				{
					["blake3_hash"] = blake3Hash,
					["chunk_index"] = chunkIndex,
					["document_id"] = blake3Hash
				},
				ct);

			// Link document to chunk
			relationships.Add(new GraphRelationshipUpsert(
				SourceId: blake3Hash,
				TargetId: chunkNodeId,
				Type: "HAS_CHUNK",
				Properties: new Dictionary<string, object?>
				{
					["chunk_index"] = chunkIndex
				},
				Bidirectional: false
			));

			// Link entities to chunk
			foreach (var entityId in entityIds)
			{
				relationships.Add(new GraphRelationshipUpsert(
					SourceId: entityId,
					TargetId: chunkNodeId,
					Type: "MENTIONED_IN_CHUNK",
					Properties: new Dictionary<string, object?>
					{
						["linked_at"] = DateTimeOffset.UtcNow.ToString("O")
					},
					Bidirectional: false
				));
			}
		}

		if (relationships.Count > 0)
		{
			await _graph.UpsertRelationshipsAsync(relationships, ct);
		}
	}

	// ────────────────────────────────────────────────────────────────
	// TEXT MATCHING
	// ────────────────────────────────────────────────────────────────

	private static bool ChunkMentionsEntity(string chunkText, string entityName, List<string>? aliases)
	{
		if (string.IsNullOrWhiteSpace(chunkText) || string.IsNullOrWhiteSpace(entityName))
			return false;

		var textLower = chunkText.ToLowerInvariant();

		// Check main name
		if (ContainsWord(textLower, entityName.ToLowerInvariant()))
			return true;

		// Check aliases
		if (aliases != null)
		{
			foreach (var alias in aliases)
			{
				if (!string.IsNullOrWhiteSpace(alias) &&
					ContainsWord(textLower, alias.ToLowerInvariant()))
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Word-boundary aware contains check to avoid false positives
	/// e.g., "John" shouldn't match "Johnson"
	/// </summary>
	private static bool ContainsWord(string text, string word)
	{
		if (word.Length < 3)
			return false; // Skip very short matches

		var index = text.IndexOf(word, StringComparison.Ordinal);
		while (index >= 0)
		{
			var beforeOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
			var afterOk = index + word.Length >= text.Length ||
						  !char.IsLetterOrDigit(text[index + word.Length]);

			if (beforeOk && afterOk)
				return true;

			index = text.IndexOf(word, index + 1, StringComparison.Ordinal);
		}

		return false;
	}

	// ────────────────────────────────────────────────────────────────
	// INTERNAL TYPES
	// ────────────────────────────────────────────────────────────────

	private sealed record EntityInfo(
		string Id,
		string Title,
		string Type,
		List<string>? Aliases
	);
}