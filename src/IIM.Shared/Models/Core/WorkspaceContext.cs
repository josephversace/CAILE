// ═══════════════════════════════════════════════════════════════════════════════
// WORKSPACE CONTEXT MODELS
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;

namespace IIM.Shared.Models;

/// <summary>
/// Context assembled for a workspace query.
/// Contains semantic chunks, entities, relationships, and metadata about retrieval.
/// </summary>
public sealed class WorkspaceContext
{
	public Guid WorkspaceId { get; init; }
	public WorkspaceIntent Intent { get; init; }


	public string? PromptProfileKey { get; set; } = "";

	/// <summary>
	/// Semantic chunks retrieved for this query.
	/// May be full document text (ChunkIndex = -1) or individual chunks.
	/// </summary>
	public List<SemanticChunk> SemanticChunks { get; init; } = [];

	/// <summary>
	/// Entities extracted from the knowledge graph.
	/// </summary>
	public List<EntitySummary> Entities { get; init; } = [];

	/// <summary>
	/// Relationships between entities.
	/// </summary>
	public List<RelationshipSummary> Relationships { get; init; } = [];

	/// <summary>
	/// Timeline events (if relevant to query).
	/// </summary>
	public List<TimelineEventSummary> Timeline { get; init; } = [];

	/// <summary>
	/// How context was retrieved:
	/// - "full_text" = Single file, full document
	/// - "full_text_multi" = Multiple files, all full text
	/// - "semantic_search" = Vector similarity search
	/// - "empty" = No context available
	/// </summary>
	public string RetrievalMode { get; init; } = "semantic_search";

	/// <summary>
	/// IDs of newly retrieved chunks (not in cache).
	/// Format: "blake3hash:chunkindex"
	/// </summary>
	public List<string> NewChunkIds { get; init; } = [];

	/// <summary>
	/// IDs of newly retrieved entities.
	/// </summary>
	public List<string> NewEntityIds { get; init; } = [];

	/// <summary>
	/// IDs of newly retrieved relationships.
	/// Format: "sourceId-[type]->targetId"
	/// </summary>
	public List<string> NewRelationshipIds { get; init; } = [];

	/// <summary>
	/// Estimated token count of this context.
	/// </summary>
	public int TotalTokenEstimate { get; init; }

	/// <summary>
	/// Whether this context contains full document text (not chunked).
	/// </summary>
	public bool IsFullText => RetrievalMode.StartsWith("full_text");
}

/// <summary>
/// A semantic chunk from vector search or full document.
/// </summary>
/// <param name="Blake3Hash">File hash this chunk belongs to.</param>
/// <param name="ChunkIndex">Index within file (-1 = full document).</param>
/// <param name="Text">The chunk text content.</param>
/// <param name="Score">Similarity score (1.0 for full text).</param>
/// <param name="FileName">Original file name if available.</param>
/// <param name="EntityIds">Entity IDs mentioned in this chunk.</param>
public sealed record SemanticChunk(
	string Blake3Hash,
	int ChunkIndex,
	string Text,
	float Score,
	string? FileName = null,
	List<string>? EntityIds = null
)
{
	/// <summary>
	/// Whether this represents a full document rather than a chunk.
	/// </summary>
	public bool IsFullDocument => ChunkIndex == -1;
}

/// <summary>
/// Summary of an entity from the knowledge graph.
/// </summary>
public sealed record EntitySummary(
	string Id,
	string Name,
	string Type,
	IReadOnlyDictionary<string, object?>? Properties = null
);

/// <summary>
/// Summary of a relationship from the knowledge graph.
/// </summary>
public sealed record RelationshipSummary(
	string SourceId,
	string TargetId,
	string Type,
	IReadOnlyDictionary<string, object?>? Properties = null
);

/// <summary>
/// Summary of a timeline event.
/// </summary>
public sealed record TimelineEventSummary(
	string Id,
	DateTimeOffset Timestamp,
	string EventType,
	string Description
);

/// <summary>
/// Cache of already-retrieved context to avoid duplication in multi-turn conversations.
/// </summary>
public sealed record RetrievedContextCache(
	HashSet<string> Chunks,
	HashSet<string> Entities,
	HashSet<string> Relationships
)
{
	public static RetrievedContextCache Empty => new([], [], []);
}
