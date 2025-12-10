using System;

namespace IIM.Shared.Models;

public record EnrichmentTask(Guid VirtualFileId, DateTimeOffset EnqueuedAt)
{
	public string? MessageId { get; set; }
}
