using System.Collections.Generic;

namespace IIM.Shared.Models;

public class EnrichmentConfig
{
	public int Workers { get; set; } = 1;

	/// Backoff sequence (seconds)
	public List<int> BackoffSeconds { get; set; } =
		new() { 5, 30, 300, 3600 }; // 5s → 30s → 5m → 1h

	/// Stream name inside Redis
	public string StreamKey { get; set; } = "caile.enrichment.tasks";
	public string ConsumerGroup { get; set; } = "caile.enrichment.group";
	public string DeadLetterKey { get; set; } = "caile.enrichment.dlq";
}
