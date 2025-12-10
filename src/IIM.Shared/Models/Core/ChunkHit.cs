using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models;

public record ChunkHit
{
	public Guid FileId { get; init; }
	public string ChunkId { get; init; } = default!;
	public string WorkspaceId { get; init; } = default!;
	public float Score { get; init; }
	public string Text { get; init; } = default!;
	public string? Classification { get; init; }
	public string? MediaType { get; init; }
}

