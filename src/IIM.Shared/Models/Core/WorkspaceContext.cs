using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{

	public sealed class WorkspaceContext
	{
		public required Guid WorkspaceId { get; init; }
		public required WorkspaceIntent Intent { get; init; }

		// From Qdrant - only NEW this request
		public List<SemanticChunk> SemanticChunks { get; init; } = [];

		// From Neo4j - only NEW this request
		public List<EntitySummary> Entities { get; init; } = [];
		public List<RelationshipSummary> Relationships { get; init; } = [];

		// From PostgreSQL
		public List<TimelineEventSummary> Timeline { get; init; } = [];

		// Track what's new (for client to cache)
		public List<string> NewChunkIds { get; init; } = [];
		public List<string> NewEntityIds { get; init; } = [];
		public List<string> NewRelationshipIds { get; init; } = [];

		public DateTimeOffset BuiltAt { get; init; } = DateTimeOffset.UtcNow;
		public int TotalTokenEstimate { get; init; }
	}

	public sealed record SemanticChunk(
		string Blake3Hash,
		int ChunkIndex,
		string Text,
		float Score,
		string? FileName = null,
		List<string>? EntityIds = null  // ADD THIS
	);

	public sealed record EntitySummary(
		string Id,
		string Name,
		string Type,
		IReadOnlyDictionary<string, object?> Properties
	);

	public sealed record RelationshipSummary(
		string SourceId,
		string TargetId,
		string Type,
		IReadOnlyDictionary<string, object?> Properties
	);

	public sealed record FileSummary(
		Guid VirtualFileId,
		string FileName,
		string MimeType,
		long FileSize,
		DateTimeOffset CreatedAt
	);

	public sealed record TimelineEventSummary(
		Guid Id,
		DateTimeOffset Timestamp,
		string EventType,
		string Description
	);
}
