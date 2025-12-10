using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace IIM.Shared.Services;

public class QdrantService : IQdrantService
{
	private readonly QdrantClient _client;
	private readonly ILogger<QdrantService> _logger;
	private readonly string _collectionName;
	private readonly uint _vectorSize;

	public QdrantService(QdrantConfig config, ILogger<QdrantService>? logger = null)
	{
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

		await _client.CreatePayloadIndexAsync(
			_collectionName,
			"case_id",
			PayloadSchemaType.Keyword,
			cancellationToken: ct);

		await _client.CreatePayloadIndexAsync(
			_collectionName,
			"file_id",
			PayloadSchemaType.Keyword,
			cancellationToken: ct);

		_logger.LogInformation("Created collection {Collection} with vector size {Size}.",
			_collectionName, _vectorSize);
	}

	public async Task StoreEmbeddingAsync(
		Guid fileId,
		string caseId,
		string chunkId,
		float[] embedding,
		string text,
		string? classification = null,
		string? mediaType = null,
		CancellationToken ct = default)
	{
		var pointId = CreatePointId(chunkId);

		var payload = new Dictionary<string, Value>
		{
			["file_id"] = fileId.ToString(),
			["case_id"] = caseId,
			["chunk_id"] = chunkId,
			["text"] = text
		};

		if (!string.IsNullOrEmpty(classification))
			payload["classification"] = classification;

		if (!string.IsNullOrEmpty(mediaType))
			payload["media_type"] = mediaType;

		var point = new PointStruct
		{
			Id = pointId,
			Vectors = embedding,
			Payload = { payload }
		};

		await _client.UpsertAsync(_collectionName, [point], cancellationToken: ct);

		_logger.LogDebug("Stored embedding for chunk {ChunkId} in case {CaseId}.", chunkId, caseId);
	}

	public async Task<List<ChunkHit>> SearchAsync(
		float[] embedding,
		int limit = 10,
		string? caseId = null,
		CancellationToken ct = default)
	{
		Filter? filter = null;

		if (!string.IsNullOrEmpty(caseId))
		{
			filter = new Filter
			{
				Must =
				{
					new Condition
					{
						Field = new FieldCondition
						{
							Key = "case_id",
							Match = new Match { Keyword = caseId }
						}
					}
				}
			};
		}

		var results = await _client.SearchAsync(
			_collectionName,
			embedding,
			filter: filter,
			limit: (ulong)limit,
			payloadSelector: true,
			cancellationToken: ct);

		return results.Select(r => new ChunkHit
		{
			ChunkId = GetPayloadString(r.Payload, "chunk_id"),
			FileId = Guid.TryParse(GetPayloadString(r.Payload, "file_id"), out var fid) ? fid : Guid.Empty,
			WorkspaceId = GetPayloadString(r.Payload, "workspace_id"),
			Text = GetPayloadString(r.Payload, "text"),
			Score = r.Score,
			Classification = GetPayloadString(r.Payload, "classification"),
			MediaType = GetPayloadString(r.Payload, "media_type")
		}).ToList();
	}

	public async Task DeleteEmbeddingsForFileAsync(Guid fileId, CancellationToken ct = default)
	{
		var filter = new Filter
		{
			Must =
			{
				new Condition
				{
					Field = new FieldCondition
					{
						Key = "file_id",
						Match = new Match { Keyword = fileId.ToString() }
					}
				}
			}
		};

		await _client.DeleteAsync(_collectionName, filter, cancellationToken: ct);

		_logger.LogInformation("Deleted embeddings for file {FileId}.", fileId);
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

	public async Task<long> CountForCaseAsync(string caseId, CancellationToken ct = default)
	{
		var filter = new Filter
		{
			Must =
			{
				new Condition
				{
					Field = new FieldCondition
					{
						Key = "case_id",
						Match = new Match { Keyword = caseId }
					}
				}
			}
		};

		var result = await _client.CountAsync(_collectionName, filter, exact: true, cancellationToken: ct);
		return (long)result;
	}



	private static PointId CreatePointId(string chunkId)
	{
		return Guid.TryParse(chunkId, out var guid)
			? guid
			: new Guid(System.Security.Cryptography.MD5.HashData(
				System.Text.Encoding.UTF8.GetBytes(chunkId)));
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
}