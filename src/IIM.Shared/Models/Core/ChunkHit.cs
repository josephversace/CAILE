using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models;


/// <summary>
/// Metadata stored with each chunk.
/// </summary>
public sealed class ChunkMetadata
{
	public Guid WorkspaceId { get; init; }
	public Guid VirtualFileId { get; init; }

	public string? FileName { get; init; }

	public string? MimeType { get; init; }

	// Optional / derived
	public string? Classification { get; init; }
	public List<string>? Entities { get; init; }

	public DateTimeOffset IndexedAt { get; init; } = DateTime.UtcNow;

	// ────────────────────────────────────────────────────────────────────────
	// SECTION TRACKING (V2 - for citations)
	// ────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Full section path (e.g., "Library Reference > ComputeHash > Parameters").
	/// </summary>
	public string? SectionPath { get; init; }

	/// <summary>
	/// Immediate parent section header (e.g., "## ComputeHash").
	/// </summary>
	public string? ParentSection { get; init; }

	/// <summary>
	/// Header level of parent section (1-6), or 0 if no parent section.
	/// </summary>
	public int ParentSectionLevel { get; init; }
}


/// <summary>
/// Chunk data for batch storage.
/// </summary>
public class ChunkData
{
	public required int ChunkIndex { get; init; }
	public required float[] Embedding { get; init; }
	public required string Text { get; init; }
	public ChunkMetadata? Metadata { get; init; }
}

/// <summary>
/// Search result.
/// </summary>
public class ChunkHit
{
	public required string Blake3Hash { get; init; }
	public required int ChunkIndex { get; init; }
	public required string Text { get; init; }
	public required float Score { get; init; }

	public string? FileName { get; init; }
	public string? MimeType { get; init; }
	public string? Classification { get; init; }
	public List<string>? Entities { get; init; }
	public List<string>? EntityIds { get; set; }

	public List<string>? WorkspaceIds { get; set; }
	public List<string>? VirtualFileIds { get; set; }

	// ────────────────────────────────────────────────────────────────────────
	// SECTION TRACKING (V2 - for citations)
	// ────────────────────────────────────────────────────────────────────────

	public string? SectionPath { get; set; }
	public string? ParentSection { get; set; }

	// ────────────────────────────────────────────────────────────────────────
	// FIX #5 — EMBEDDING ROLE
	// ────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Role of this embedding in retrieval vs reasoning.
	/// "authoritative" (default) or "search_only".
	/// </summary>
	public string EmbeddingRole { get; init; } = "authoritative";

	/// <summary>
	/// If derived, the source authoritative chunk index.
	/// </summary>
	public int? SourceChunkIndex { get; init; }
}
