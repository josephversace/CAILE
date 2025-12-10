using System;
using System.Collections.Generic;

namespace IIM.Shared.Models;

public class EnrichmentState
{
	public Guid VirtualFileId { get; }
	public string? StoredFileHash { get; set; }

	// Aggregated metadata produced by steps
	public Dictionary<string, object?> Metadata { get; } = new();

	// Derived file outputs: key = stepName → list of file paths
	public Dictionary<string, List<string>> DerivedFiles { get; } = new();

	// AI-proposed classification
	public string? ProposedLabel { get; set; }

	// Embeddings that QdrantIndexStep will publish
	public float[]? Embeddings { get; set; }

	// Any step errors
	public List<string> Errors { get; } = new();

	public EnrichmentState(Guid virtualFileId)
	{
		VirtualFileId = virtualFileId;
	}
}
