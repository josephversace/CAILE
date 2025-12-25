using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;

public interface IQdrantService
{
    /// <summary>
    /// Ensure collection exists with proper indexes.
    /// </summary>
    Task EnsureCollectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Store a chunk embedding keyed by content hash.
    /// </summary>
    Task StoreChunkAsync(
        string blake3Hash,
        int chunkIndex,
        float[] embedding,
        string text,
        ChunkMetadata? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Store multiple chunk embeddings in batch.
    /// </summary>
    Task StoreChunksAsync(
        string blake3Hash,
        List<ChunkData> chunks,
        CancellationToken ct = default);

	/// <summary>
	/// Search for similar chunks filtered by specific file hashes.
	/// </summary>
	/// 
	// Add this method to your interface
	Task<List<ChunkHit>> SearchByHashesBalancedAsync(
		float[] embedding,
		List<string> blake3Hashes,
		int totalLimit = 12,
		int minPerFile = 2,
		CancellationToken ct = default);


	Task<List<ChunkHit>> SearchByHashesAsync(
        float[] embedding,
        List<string> blake3Hashes,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Search across all chunks (no filter).
    /// </summary>
    Task<List<ChunkHit>> SearchAllAsync(
        float[] embedding,
        int limit = 10,
        CancellationToken ct = default);

	Task AttachFileToExistingChunksAsync(
	string blake3Hash,
	Guid workspaceId,
	Guid virtualFileId,
	CancellationToken ct = default);


	/// <summary>
	/// Delete all chunks for a given content hash.
	/// </summary>
	Task DeleteByHashAsync(string blake3Hash, CancellationToken ct = default);

    /// <summary>
    /// Check if chunks exist for a given hash.
    /// </summary>
    Task<bool> ExistsAsync(string blake3Hash, CancellationToken ct = default);

    /// <summary>
    /// Count chunks for a given hash.
    /// </summary>
    Task<long> CountByHashAsync(string blake3Hash, CancellationToken ct = default);

    /// <summary>
    /// Health check.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken ct = default);

	// src/IIM.Shared/Interfaces/IQdrantService.cs
	// ADD these methods to the existing interface

	/// <summary>Get all chunks for a specific document hash.</summary>
	Task<List<ChunkRecord>> GetChunksByHashAsync(string blake3Hash, CancellationToken ct = default);

	/// <summary>Update payload fields on an existing chunk.</summary>
	Task UpdateChunkPayloadAsync(
		string blake3Hash,
		int chunkIndex,
		Dictionary<string, object> payload,
		CancellationToken ct = default);
}
