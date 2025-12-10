using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;

public interface IQdrantService
{
	/// <summary>Ensures that the global CAILE vector collection exists.</summary>
	Task EnsureCollectionAsync(CancellationToken ct = default);

	/// <summary>Upserts a vector chunk + metadata.</summary>
	Task StoreEmbeddingAsync(
		Guid fileId,
		string caseId,
		string chunkId,
		float[] embedding,
		string text,
		string? classification = null,
		string? mediaType = null,
		CancellationToken ct = default);

	/// <summary>Semantic search with optional case isolation.</summary>
	Task<List<ChunkHit>> SearchAsync(
		float[] embedding,
		int limit = 10,
		string? caseId = null,
		CancellationToken ct = default);

	/// <summary>Delete embeddings tied to a specific file.</summary>
	Task DeleteEmbeddingsForFileAsync(
		Guid fileId,
		CancellationToken ct = default);

	/// <summary>Lightweight health check.</summary>
	Task<bool> IsHealthyAsync(CancellationToken ct = default);

	/// <summary>Debug: count points for a case.</summary>
	Task<long> CountForCaseAsync(string caseId, CancellationToken ct = default);
}
