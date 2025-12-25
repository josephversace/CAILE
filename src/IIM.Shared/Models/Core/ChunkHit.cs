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

}

