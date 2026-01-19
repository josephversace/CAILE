using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Ingestion.Chunking;
using IIM.Ingestion.Models;
using IIM.Shared.Models;

namespace IIM.Ingestion.Services;

public sealed class EmbedIndexQdrantStep : IIngestionStep
{
	public string Id => IngestionStepIds.EmbedIndexQdrant;
	public string Version => "1.0";
	public IReadOnlyList<string> DependsOn => new[] { IngestionStepIds.ChunkBuild };
	public bool IsFatal => true;

	public bool RequiresBytes => true;

	const int MaxChars = 4000;

	public ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		// Identity: stored file + embedding vector size + pipeline params
		var input = ctx.StoredFile.Blake3Hash;
		var parms = $"vec:{ctx.Embedding.VectorSize}:v1";
		return ValueTask.FromResult((input, parms));
	}

	public async Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct)
	{
		if (!ctx.TryGetExtractedText(out var text))
		{
			const string skip = "{\"skipped\":\"no_text\"}";
			return ("no-text", skip);
		}


		if (!ctx.Embedding.IsReady)
			return (null, "{\"skipped\":true,\"reason\":\"embedding_not_ready\"}");

		var storedHash = ctx.StoredFile.Blake3Hash;

		// Dedup: if already indexed, attach to workspace/vf and record.
		if (await ctx.Qdrant.ExistsAsync(storedHash, ct))
		{
			await ctx.Qdrant.AttachFileToExistingChunksAsync(
				storedHash,
				ctx.VirtualFile.WorkspaceId,
				ctx.VirtualFile.Id,
				ctx.VirtualFile.FileName,
				ct);

			await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
			{
				StoredFileHash = storedHash,
				DerivedHash = null,
				ProcessorName = "QdrantIndex",
				ProcessorKind = "embedding",
				ProcessorVersion = Version,
				ParametersHash = $"vec:{ctx.Embedding.VectorSize}:v1",
				MetadataJson = "{\"skipped\":true,\"reason\":\"already_indexed\"}"
			}, ct);

			return (null, "{\"skipped\":true,\"reason\":\"already_indexed\"}");
		}

		ChunkingResult chunking;
		DocumentShapeResult shape;

		// ─────────────────────────────────────────────────────────────
		// Fast path: same-run reuse
		// ─────────────────────────────────────────────────────────────
		if (ctx.Bag.TryGetValue("chunking", out var cObj) && cObj is ChunkingResult cachedChunking)
		{
			chunking = cachedChunking;

			if (!ctx.Bag.TryGetValue("document_shape", out var sObj) || sObj is not DocumentShapeResult cachedShape)
				throw new InvalidOperationException("Chunking exists without document shape.");

			shape = cachedShape;
		}
		else
		{
			// ─────────────────────────────────────────────────────────
			// Slow path: recompute from durable text
			// ─────────────────────────────────────────────────────────


			if (!ctx.Bag.TryGetValue("document_shape", out var sObj) || sObj is not DocumentShapeResult cachedShape)
			{
				shape = ctx.ShapeDetector.Detect(text);
				ctx.Bag["document_shape"] = shape;
			}
			else
			{
				shape = cachedShape;
			}

			var options = ChunkingStrategyFactory.SelectOptionsForShape(shape) with
			{
				FileName = ctx.VirtualFile.FileName,
				MimeType = ctx.StoredFile.MimeType,
				Blake3Hash = ctx.StoredFile.Blake3Hash
			};

			chunking = ctx.ChunkingFactory.Chunk(text, shape, options);

			// same-run reuse
			ctx.Bag["chunking"] = chunking;
		}


		if (chunking.Chunks.Count == 0)
			return (null, "{\"skipped\":true,\"reason\":\"no_chunks\"}");

		var mime = ctx.StoredFile.MimeType;

		var workItems = chunking.Chunks.Select(chunk =>
		{
			var contentType = chunk.ContentType;

			IReadOnlyDictionary<string, string> metadata =
				new Dictionary<string, string>
				{
					["file_name"] = ctx.VirtualFile.FileName,
					["mime_type"] = mime,
					["content_type"] = contentType.ToString(),
					["section_path"] = chunk.SectionPath ?? "",
					["parent_section"] = chunk.ParentSection ?? ""
				};

			return new EmbeddingWorkItem
			{
				Blake3Hash = storedHash,
				ChunkIndex = chunk.Index,
				Text = BuildSafeChunkText(chunk.OverlapPrefix, chunk.Text),
				MaxTokens = 8192,
				SemanticType = contentType.ToString().ToLowerInvariant(),
				Metadata = metadata
			};
		}).ToList();

		var embeddings = await ctx.Embedding.EmbedAsync(workItems, ct);

		var chunkData = workItems.Zip(embeddings, (work, embedding) => new ChunkData
		{
			ChunkIndex = work.ChunkIndex,
			Embedding = embedding,
			Text = work.Text,
			Metadata = new ChunkMetadata
			{
				FileName = ctx.VirtualFile.FileName,
				MimeType = mime,
				Classification = work.SemanticType,
				IndexedAt = DateTimeOffset.UtcNow,
				WorkspaceId = ctx.VirtualFile.WorkspaceId,
				VirtualFileId = ctx.VirtualFile.Id,
				SectionPath = work.Metadata.GetValueOrDefault("section_path"),
				ParentSection = work.Metadata.GetValueOrDefault("parent_section")
			}
		}).ToList();

		await ctx.Qdrant.StoreChunksAsync(storedHash, chunkData, ct);

		await ctx.Workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = storedHash,
			DerivedHash = null,
			ProcessorName = "QdrantIndex",
			ProcessorKind = "embedding",
			ProcessorVersion = Version,
			ParametersHash = $"vec:{ctx.Embedding.VectorSize}:v1",
			MetadataJson = JsonSerializer.Serialize(new
			{
				chunkCount = chunkData.Count,
				vectorSize = ctx.Embedding.VectorSize
			})
		}, ct);

		return (null, "{\"status\":\"ok\"}");
	}




	static string BuildSafeChunkText(string? prefix, string text)
	{
		if (string.IsNullOrWhiteSpace(prefix))
			return Truncate(text, MaxChars);

		// Cap overlap explicitly (e.g. 20%)
		var maxPrefix = MaxChars / 5;
		if (prefix.Length > maxPrefix)
			prefix = prefix[^maxPrefix..];

		var combined = $"{prefix} {text}";
		return Truncate(combined, MaxChars);
	}

	public Task<bool> VerifyAsync(IngestionStepContext ctx, string? outputHash, CancellationToken ct)
	{
		// StepState verification is basically "embedding exists" but that’s stored in Qdrant.
		// We accept completed step rows as valid.
		return Task.FromResult(true);
	}

	private static string Truncate(string s, int maxChars)
	{
		if (string.IsNullOrEmpty(s) || s.Length <= maxChars) return s;
		return s.Substring(0, maxChars);
	}

}
