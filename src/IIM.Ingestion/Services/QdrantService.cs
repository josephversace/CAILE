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
			limit: 1024,
			cancellationToken: ct);

		var updates = new List<PointStruct>();

		foreach (var p in scrollResponse.Result)  // Note: .Result to get the points
		{
			var payload = p.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

			AddToList(payload, "workspace_ids", workspaceId.ToString());
			AddToList(payload, "virtual_file_ids", virtualFileId.ToString());

			updates.Add(new PointStruct
			{
				Id = p.Id,
				Payload = { payload }
			});
		}

		if (updates.Count > 0)
			await _client.UpsertAsync(_collectionName, updates, cancellationToken: ct);

		_logger.LogInformation(
			"Attached existing chunks for hash {Hash} to workspace {WorkspaceId}",
			blake3Hash[..12],
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
		return results.Select(r => new ChunkHit
		{
			Blake3Hash = GetPayloadString(r.Payload, "blake3_hash") ?? "",
			ChunkIndex = GetPayloadInt(r.Payload, "chunk_index"),
			Text = GetPayloadString(r.Payload, "text") ?? "",
			Score = r.Score,
			FileName = GetPayloadString(r.Payload, "file_name"),
			MimeType = GetPayloadString(r.Payload, "mime_type"),
			Classification = GetPayloadString(r.Payload, "classification"),
			Entities = GetPayloadStringList(r.Payload, "entities")
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
}
