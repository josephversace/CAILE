using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using static Qdrant.Client.Grpc.PointsUpdateOperation.Types;

namespace IIM.Ingestion.Services;

public class QdrantService : IQdrantService
{
	private readonly QdrantClient _client;
	private readonly ILogger<QdrantService> _logger;
	private readonly string _collectionName;
	private readonly uint _vectorSize;

	public QdrantService(CaileConfig caileConfig, ILogger<QdrantService>? logger = null)
	{
		ArgumentNullException.ThrowIfNull(caileConfig);

		QdrantConfig? config = caileConfig.Qdrant;
		ArgumentNullException.ThrowIfNull(config);

		_client = new QdrantClient(
			host: config.Host,
			port: config.GrpcPort,
			https: config.UseTls,
			apiKey: string.IsNullOrEmpty(config.ApiKey) ? null : config.ApiKey);

		_collectionName = config.DefaultCollection;
		_vectorSize = config.VectorSize;
		_logger = logger ?? NullLogger<QdrantService>.Instance;
	}

	public async Task EnsureCollectionAsync(CancellationToken ct = default)
	{
		var collections = await _client.ListCollectionsAsync(ct);

		if (collections.Any(c => c == _collectionName))
		{
			_logger.LogDebug("Collection {Collection} already exists.", _collectionName);
			return;
		}

		await _client.CreateCollectionAsync(
			_collectionName,
			new VectorParams
			{
				Size = _vectorSize,
				Distance = Distance.Cosine
			},
			cancellationToken: ct);

		// Index on blake3_hash for filtering by file
		await _client.CreatePayloadIndexAsync(
			_collectionName,
			"blake3_hash",
			PayloadSchemaType.Keyword,
			cancellationToken: ct);

		// Index on mime_type for filtering by type
		await _client.CreatePayloadIndexAsync(
			_collectionName,
			"mime_type",
			PayloadSchemaType.Keyword,
			cancellationToken: ct);

		// Index on classification for filtering
		await _client.CreatePayloadIndexAsync(
			_collectionName,
			"classification",
			PayloadSchemaType.Keyword,
			cancellationToken: ct);

		_logger.LogInformation(
			"Created collection {Collection} with vector size {Size}.",
			_collectionName,
			_vectorSize);
	}

	public async Task StoreChunkAsync(
		string blake3Hash,
		int chunkIndex,
		float[] embedding,
		string text,
		ChunkMetadata? metadata = null,
		CancellationToken ct = default)
	{
		var pointId = CreatePointId(blake3Hash, chunkIndex);

		var payload = new Dictionary<string, Value>
		{
			["blake3_hash"] = blake3Hash,
			["chunk_index"] = chunkIndex,
			["text"] = text,
			["indexed_at"] = DateTimeOffset.UtcNow.ToString("O")
		};

		if (metadata.Entities?.Count > 0)
		{
			var list = new ListValue();
			foreach (var e in metadata.Entities)
			{
				list.Values.Add(new Value { StringValue = e });
			}

			payload["entities"] = new Value { ListValue = list };
		}


		var point = new PointStruct
		{
			Id = pointId,
			Vectors = embedding,
			Payload = { payload }
		};

		await _client.UpsertAsync(_collectionName, [point], cancellationToken: ct);

		_logger.LogDebug(
			"Stored chunk {ChunkIndex} for hash {Hash}.",
			chunkIndex,
			blake3Hash[..12]);
	}

	public async Task AttachFileToExistingChunksAsync(
		string blake3Hash,
		Guid workspaceId,
		Guid virtualFileId,
		CancellationToken ct = default)
	{
		// 1. Define the filter to find all chunks for this file hash
		var filter = new Filter
		{
			Must = {
			new Condition {
				Field = new FieldCondition {
					Key = "blake3_hash",
					Match = new Match { Keyword = blake3Hash }
				}
			}
		}
		};

		// 2. Prepare the partial update payload
		// We use SetPayload because it merges with existing keys
		var updatePayload = new Dictionary<string, Value>
		{
			["workspace_ids"] = new Value
			{
				ListValue = new ListValue { Values = { new Value { StringValue = workspaceId.ToString() } } }
			},
			["virtual_file_ids"] = new Value
			{
				ListValue = new ListValue { Values = { new Value { StringValue = virtualFileId.ToString() } } }
			}
		};

		// 3. Apply the update to all points matching the hash filter
		await _client.SetPayloadAsync(_collectionName, updatePayload, filter, cancellationToken: ct);

		_logger.LogInformation(
			"Attached hash {Hash} to workspace {WorkspaceId} (Atomic Update)",
			blake3Hash[..Math.Min(12, blake3Hash.Length)],
			workspaceId);
	}

	private static void AddToList(
	IDictionary<string, Value> payload,
	string key,
	string value)
	{
		if (!payload.TryGetValue(key, out var existing) || existing.ListValue == null)
		{
			payload[key] = new Value
			{
				ListValue = new ListValue
				{
					Values = { new Value { StringValue = value } }
				}
			};
			return;
		}

		if (!existing.ListValue.Values.Any(v => v.StringValue == value))
			existing.ListValue.Values.Add(new Value { StringValue = value });
	}


	public async Task StoreChunksAsync(
		string blake3Hash,
		List<ChunkData> chunks,
		CancellationToken ct = default)
	{
		if (chunks.Count == 0)
			return;

		var points = new List<PointStruct>(chunks.Count);

		foreach (var chunk in chunks)
		{
			var pointId = CreatePointId(blake3Hash, chunk.ChunkIndex);

			var payload = new Dictionary<string, Value>
			{
				["blake3_hash"] = blake3Hash,
				["chunk_index"] = chunk.ChunkIndex,
				["indexed_at"] = DateTimeOffset.UtcNow.ToString("O")
			};

			if (!string.IsNullOrEmpty(chunk.Text))
				payload["text"] = chunk.Text;

			if (chunk.Metadata != null)
			{
				payload["workspace_ids"] = new Value
				{
					ListValue = new ListValue
					{
						Values =
			{
				new Value { StringValue = chunk.Metadata.WorkspaceId.ToString() }
			}
					}
				};

				payload["virtual_file_ids"] = new Value
				{
					ListValue = new ListValue
					{
						Values =
			{
				new Value { StringValue = chunk.Metadata.VirtualFileId.ToString() }
			}
					}
				};

				if (!string.IsNullOrEmpty(chunk.Metadata.Classification))
					payload["classification"] = chunk.Metadata.Classification;

				if (chunk.Metadata.Entities?.Count > 0)
				{
					var entities = new ListValue();
					foreach (var e in chunk.Metadata.Entities)
						entities.Values.Add(new Value { StringValue = e });

					payload["entities"] = new Value { ListValue = entities };
				}
			}

			points.Add(new PointStruct
			{
				Id = pointId,
				Vectors = chunk.Embedding,
				Payload = { payload }
			});
		}

		await _client.UpsertAsync(
		collectionName: _collectionName,
		points: points,
		cancellationToken: ct);

	}


	public async Task<List<ChunkHit>> SearchByHashesAsync(
		float[] embedding,
		List<string> blake3Hashes,
		int limit = 10,
		CancellationToken ct = default)
	{
		if (blake3Hashes.Count == 0)
			return [];

		// Filter: blake3_hash IN [hash1, hash2, ...]
		var filter = new Filter
		{
			Should =
			{
				blake3Hashes.Select(hash => new Condition
				{
					Field = new FieldCondition
					{
						Key = "blake3_hash",
						Match = new Match { Keyword = hash }
					}
				})
			}
		};

		var results = await _client.SearchAsync(
			_collectionName,
			embedding,
			filter: filter,
			limit: (ulong)limit,
			payloadSelector: true,
			cancellationToken: ct);

		return MapResults(results);
	}
	//RoundRobin
	public async Task<List<ChunkHit>> SearchByHashesBalancedAsync(
	float[] embedding,
	List<string> blake3Hashes,
	int totalLimit = 12,
	int minPerFile = 2,
	CancellationToken ct = default)
	{
		if (blake3Hashes.Count == 0) return [];

		// 1. Fetch more than we need from each file in parallel
		// This ensures we have enough 'candidates' to pick from.
		var tasks = blake3Hashes.Select(hash =>
			SearchSingleHashAsync(embedding, hash, minPerFile + 2, ct));

		var resultsPerFile = await Task.WhenAll(tasks);

		// 2. Interleave the results (Round-Robin)
		// This gives every file a "fair shot" at the top spots.
		var interleaved = new List<ChunkHit>();
		int depth = 0;
		bool addedAny;

		do
		{
			addedAny = false;
			foreach (var fileResults in resultsPerFile)
			{
				if (depth < fileResults.Count)
				{
					interleaved.Add(fileResults[depth]);
					addedAny = true;
				}
				if (interleaved.Count >= totalLimit) break;
			}
			depth++;
		} while (addedAny && interleaved.Count < totalLimit);

		return interleaved;
	}

	//public async Task<List<ChunkHit>> SearchByHashesBalancedAsync(
	//float[] embedding,
	//List<string> blake3Hashes,
	//int totalLimit = 12,
	//int minPerFile = 2,
	//CancellationToken ct = default)
	//{
	//	if (blake3Hashes.Count == 0)
	//		return [];

	//	// Single file - just use normal search
	//	if (blake3Hashes.Count == 1)
	//	{
	//		return await SearchByHashesAsync(embedding, blake3Hashes, totalLimit, ct);
	//	}

	//	var fileCount = blake3Hashes.Count;

	//	// ════════════════════════════════════════════════════════════
	//	// Tier 1: Small sets (2-10 files) - parallel per-file search
	//	// ════════════════════════════════════════════════════════════
	//	if (fileCount <= 10)
	//	{
	//		var perFileK = Math.Max(minPerFile, (totalLimit / fileCount) + 1);

	//		var tasks = blake3Hashes.Select(hash =>
	//			SearchSingleHashAsync(embedding, hash, perFileK, ct));

	//		var results = await Task.WhenAll(tasks);

	//		return results
	//			.SelectMany(r => r)
	//			.GroupBy(h => $"{h.Blake3Hash}:{h.ChunkIndex}")
	//			.Select(g => g.First())
	//			.OrderByDescending(h => h.Score)
	//			.Take(totalLimit)
	//			.ToList();
	//	}

	//	// ════════════════════════════════════════════════════════════
	//	// Tier 2: Medium sets (11-100 files) - global search + backfill
	//	// ════════════════════════════════════════════════════════════
	//	if (fileCount <= 100)
	//	{
	//		// Phase 1: Global search with over-fetch
	//		var globalHits = await SearchByHashesAsync(
	//			embedding, blake3Hashes, totalLimit * 2, ct);

	//		// Check coverage
	//		var coveredFiles = globalHits
	//			.Select(h => h.Blake3Hash)
	//			.ToHashSet();

	//		var uncoveredFiles = blake3Hashes
	//			.Where(h => !coveredFiles.Contains(h))
	//			.ToList();

	//		// Phase 2: Backfill if coverage is poor (< 50% of files)
	//		if (uncoveredFiles.Count > fileCount / 2)
	//		{
	//			// Sample up to 10 uncovered files for backfill
	//			var toBackfill = uncoveredFiles.Take(10).ToList();

	//			var backfillTasks = toBackfill.Select(hash =>
	//				SearchSingleHashAsync(embedding, hash, 1, ct));

	//			var backfillResults = await Task.WhenAll(backfillTasks);

	//			globalHits.AddRange(backfillResults.SelectMany(r => r));

	//			_logger.LogDebug(
	//				"Backfilled {Count} uncovered files out of {Total}",
	//				toBackfill.Count, uncoveredFiles.Count);
	//		}

	//		return globalHits
	//			.GroupBy(h => $"{h.Blake3Hash}:{h.ChunkIndex}")
	//			.Select(g => g.First())
	//			.OrderByDescending(h => h.Score)
	//			.Take(totalLimit)
	//			.ToList();
	//	}

	//	// ════════════════════════════════════════════════════════════
	//	// Tier 3: Large sets (100+ files) - global search, trust vectors
	//	// ════════════════════════════════════════════════════════════
	//	_logger.LogDebug(
	//		"Large file set ({Count} files) - using global semantic ranking",
	//		fileCount);

	//	var hits = await SearchByHashesAsync(embedding, blake3Hashes, totalLimit, ct);

	//	var representedCount = hits.Select(h => h.Blake3Hash).Distinct().Count();
	//	_logger.LogDebug(
	//		"Global search covered {Represented}/{Total} files",
	//		representedCount, fileCount);

	//	return hits;
	//}

	private async Task<List<ChunkHit>> SearchSingleHashAsync(
		float[] embedding,
		string blake3Hash,
		int limit,
		CancellationToken ct)
	{
		var filter = new Filter
		{
			Must =
		{
			new Condition
			{
				Field = new FieldCondition
				{
					Key = "blake3_hash",
					Match = new Match { Keyword = blake3Hash }
				}
			}
		}
		};

		var results = await _client.SearchAsync(
			_collectionName,
			embedding,
			filter: filter,
			limit: (ulong)limit,
			payloadSelector: true,
			cancellationToken: ct);

		return MapResults(results);
	}

	public async Task<List<ChunkHit>> SearchAllAsync(
		float[] embedding,
		int limit = 10,
		CancellationToken ct = default)
	{
		var results = await _client.SearchAsync(
			_collectionName,
			embedding,
			limit: (ulong)limit,
			payloadSelector: true,
			cancellationToken: ct);

		return MapResults(results);
	}

	public async Task DeleteByHashAsync(string blake3Hash, CancellationToken ct = default)
	{
		var filter = new Filter
		{
			Must =
			{
				new Condition
				{
					Field = new FieldCondition
					{
						Key = "blake3_hash",
						Match = new Match { Keyword = blake3Hash }
					}
				}
			}
		};

		await _client.DeleteAsync(_collectionName, filter, cancellationToken: ct);

		_logger.LogInformation("Deleted chunks for hash {Hash}.", blake3Hash[..12]);
	}

	public async Task<bool> ExistsAsync(string blake3Hash, CancellationToken ct = default)
	{
		try
		{
			var count = await CountByHashAsync(blake3Hash, ct);
			return count > 0;
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
		{
			// Collection doesn't exist yet - nothing indexed
			return false;
		}
	}

	public async Task<long> CountByHashAsync(string blake3Hash, CancellationToken ct = default)
	{
		var filter = new Filter
		{
			Must =
			{
				new Condition
				{
					Field = new FieldCondition
					{
						Key = "blake3_hash",
						Match = new Match { Keyword = blake3Hash }
					}
				}
			}
		};

		var result = await _client.CountAsync(
			_collectionName,
			filter,
			exact: true,
			cancellationToken: ct);

		return (long)result;
	}

	public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
	{
		try
		{
			await _client.ListCollectionsAsync(ct);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Qdrant health check failed.");
			return false;
		}
	}

	// ─────────────────────────────────────────────────────────────
	// HELPERS
	// ─────────────────────────────────────────────────────────────

	/// <summary>
	/// Create deterministic point ID from hash + chunk index.
	/// Same content always gets same ID (idempotent upserts).
	/// </summary>
	private static PointId CreatePointId(string blake3Hash, int chunkIndex)
	{
		// Deterministic UUID from (hash + chunk index)
		// Uses first 16 bytes of SHA256 to form a UUID
		using var sha = System.Security.Cryptography.SHA256.Create();

		var input = $"{blake3Hash}:{chunkIndex:D6}";
		var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));

		var guidBytes = new byte[16];
		Array.Copy(hash, guidBytes, 16);

		return new PointId { Uuid = new Guid(guidBytes).ToString() };
	}




	private static List<ChunkHit> MapResults(IReadOnlyList<ScoredPoint> results)
	{
		return results.Select(r =>
		{
			var embeddingRole =
				GetPayloadString(r.Payload, "embedding_role") ?? "authoritative";

			int? sourceChunkIndex = null;
			var sourceIdx = GetPayloadString(r.Payload, "source_chunk_index");
			if (int.TryParse(sourceIdx, out var parsed))
				sourceChunkIndex = parsed;

			return new ChunkHit
			{
				Blake3Hash = GetPayloadString(r.Payload, "blake3_hash") ?? "",
				ChunkIndex = GetPayloadInt(r.Payload, "chunk_index"),
				Text = GetPayloadString(r.Payload, "text") ?? "",
				Score = r.Score,

				FileName = GetPayloadString(r.Payload, "file_name"),
				MimeType = GetPayloadString(r.Payload, "mime_type"),
				Classification = GetPayloadString(r.Payload, "classification"),

				Entities = GetPayloadStringList(r.Payload, "entities"),
				WorkspaceIds = GetPayloadStringList(r.Payload, "workspace_ids"),
				VirtualFileIds = GetPayloadStringList(r.Payload, "virtual_file_ids"),

				// 🔒 FIX #5 FIELDS
				EmbeddingRole = embeddingRole,
				SourceChunkIndex = sourceChunkIndex
			};
		}).ToList();

	}

	private static string? GetPayloadString(
		IDictionary<string, Value>? payload,
		string key)
	{
		if (payload == null)
			return null;

		return payload.TryGetValue(key, out var value)
			? value.StringValue
			: null;
	}

	private static int GetPayloadInt(
		IDictionary<string, Value>? payload,
		string key)
	{
		if (payload == null)
			return 0;

		return payload.TryGetValue(key, out var value)
			? (int)value.IntegerValue
			: 0;
	}

	private static List<string>? GetPayloadStringList(
		IDictionary<string, Value>? payload,
		string key)
	{
		if (payload == null)
			return null;

		if (!payload.TryGetValue(key, out var value))
			return null;

		if (value.ListValue == null)
			return null;

		return value.ListValue.Values
			.Select(v => v.StringValue)
			.Where(s => !string.IsNullOrEmpty(s))
			.ToList();
	}

	// src/IIM.Ingestion/Services/QdrantService.cs
	// ADD these methods to your existing QdrantService class

	public async Task<List<ChunkRecord>> GetChunksByHashAsync(string blake3Hash, CancellationToken ct = default)
	{
		var filter = new Filter
		{
			Must =
		{
			new Condition
			{
				Field = new FieldCondition
				{
					Key = "blake3_hash",
					Match = new Match { Keyword = blake3Hash }
				}
			}
		}
		};

		var scrollResponse = await _client.ScrollAsync(
			collectionName: _collectionName,
			filter: filter,
			payloadSelector: new WithPayloadSelector { Enable = true },
			limit: 1000,
			cancellationToken: ct);

		var results = new List<ChunkRecord>();

		foreach (var point in scrollResponse.Result)
		{
			var chunkIndex = GetPayloadInt(point.Payload, "chunk_index");
			var text = GetPayloadString(point.Payload, "text") ?? "";
			var entityIds = GetPayloadStringList(point.Payload, "entity_ids");

			results.Add(new ChunkRecord(blake3Hash, chunkIndex, text, entityIds));
		}

		return results.OrderBy(c => c.ChunkIndex).ToList();
	}

	public async Task UpdateChunkPayloadAsync(
	string blake3Hash,
	int chunkIndex,
	Dictionary<string, object> payload,
	CancellationToken ct = default)
	{
		var qdrantPayload = new Dictionary<string, Value>();

		foreach (var (key, value) in payload)
		{
			qdrantPayload[key] = value switch
			{
				string s => new Value { StringValue = s },
				int i => new Value { IntegerValue = i },
				long l => new Value { IntegerValue = l },
				double d => new Value { DoubleValue = d },
				bool b => new Value { BoolValue = b },
				IEnumerable<string> list => new Value
				{
					ListValue = new ListValue
					{
						Values = { list.Select(s => new Value { StringValue = s }) }
					}
				},
				_ => new Value { StringValue = value?.ToString() ?? "" }
			};
		}

		// Use filter to target the specific point instead of ID
		var filter = new Filter
		{
			Must =
		{
			new Condition
			{
				Field = new FieldCondition
				{
					Key = "blake3_hash",
					Match = new Match { Keyword = blake3Hash }
				}
			},
			new Condition
			{
				Field = new FieldCondition
				{
					Key = "chunk_index",
					Match = new Match { Integer = chunkIndex }
				}
			}
		}
		};

		await _client.SetPayloadAsync(
			collectionName: _collectionName,
			payload: qdrantPayload,
			filter: filter,
			cancellationToken: ct);

		_logger.LogDebug(
			"Updated payload for chunk {ChunkIndex} of {Hash}",
			chunkIndex,
			blake3Hash[..Math.Min(12, blake3Hash.Length)]);
	}
}
