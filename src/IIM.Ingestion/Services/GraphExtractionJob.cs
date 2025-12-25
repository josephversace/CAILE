using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using IIM.Ingestion.Extensions;
using IIM.Ingestion.Models;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services;

public sealed class GraphExtractionJob
{
	private readonly IGraphRagPipeline _graphRag;
	private readonly IWorkspaceManager _workspace;
	private readonly IFileStore _files;

	private readonly CaileConfig _config;
	private readonly EntityLinkingJob? _entityLinking;
	private readonly ILogger<GraphExtractionJob> _logger;

	private const string DerivedCollection = "derived";

	public GraphExtractionJob(
		IGraphRagPipeline graphRag,
		IWorkspaceManager workspace,
		IFileStore files,
		CaileConfig config,
		ILogger<GraphExtractionJob> logger,
		EntityLinkingJob? entityLinking = null)
	{
		_graphRag = graphRag;
		_workspace = workspace;
		_files = files;
		_config = config;
		_logger = logger;
		_entityLinking = entityLinking;
	}

	/// <summary>
	/// Enqueues graph extraction as a background job.
	/// Arguments MUST be small and serializable.
	/// </summary>
	public Task EnqueueAsync(
		string storedFileHash,
		string extractedTextDerivedHash,
		Guid virtualFileId)
	{
		BackgroundJob.Enqueue(() =>
			ExecuteAsync(
				storedFileHash,
				extractedTextDerivedHash,
				virtualFileId,
				CancellationToken.None));

		return Task.CompletedTask;
	}

	// ------------------------------------------------------------------
	// Background execution
	// ------------------------------------------------------------------

	public async Task ExecuteAsync(
		string storedFileHash,
		string extractedTextDerivedHash,
		Guid virtualFileId,
		CancellationToken ct)
	{
		using Blake3.Blake3HashAlgorithm blake3 = new Blake3.Blake3HashAlgorithm();

		_logger.LogInformation(
			"Starting graph extraction for {Hash}",
			storedFileHash[..12]);

		// 1. Load VirtualFile fresh (retry-safe)
		var vf = await _workspace.GetVirtualFileByIdAsync(virtualFileId, ct)
			?? throw new InvalidOperationException($"VirtualFile {virtualFileId} not found.");

		// Todo 2. Idempotency check
		//if (await _workspace.ProcessedFileExistsAsync(
		//		storedFileHash,
		//		processorKind: "graph",
		//		ct))
		//{
		//	_logger.LogInformation(
		//		"Graph extraction already exists for {Hash}, skipping.",
		//		storedFileHash[..12]);
		//	return;
		//}

		// 3. Load extracted text from derived storage
		var extractedBytes = await _files.ReadAsync(
			DerivedCollection,
			extractedTextDerivedHash,
			ct);

		var text = Encoding.UTF8.GetString(extractedBytes);

		if (string.IsNullOrWhiteSpace(text))
		{
			_logger.LogWarning(
				"Extracted text empty for {Hash}, skipping graph extraction.",
				storedFileHash[..12]);
			return;
		}

		// 4. Run GraphRAG
		var result = await _graphRag.ProcessAsync(
			documents: [new DocumentInput(vf.FileName, text)],
			documentId: storedFileHash,
			workspaceId: vf.WorkspaceId,
			virtualFileId: vf.Id,
			fileName: vf.FileName,
			_config.GraphRag.ToGraphRagConfig(),
			ct);

		if (result.IsEmpty)
		{
			_logger.LogDebug(
				"No entities extracted for {Hash}",
				storedFileHash[..12]);
			return;
		}




		var graphPayload = new
		{
			extraction_type = "graphrag_extraction",
			epistemic_status = "untrusted_extraction",
			source_document = storedFileHash,
			generated_at = DateTimeOffset.UtcNow,

			extracted_entities = result.Entities,
			extracted_relationships = result.Relationships,
			neo4j_node_count = result.Neo4jNodeCount
		};


		var metadata = new
		{
			schema = "graphrag_reported_items_v1",

			counts = new
			{
				entityCount = result.Entities.Count,
				relationshipCount = result.Relationships.Count,
				neo4jNodeCount = result.Neo4jNodeCount

			}

		};




		var graphJson = JsonSerializer.Serialize(
			graphPayload,
			new JsonSerializerOptions { WriteIndented = true });


	

		var graphBytes = Encoding.UTF8.GetBytes(graphJson);

		var hashBytes = blake3.ComputeHash(graphBytes);

		// Convert to canonical lowercase hex
		var derivedHash = Convert
			.ToHexString(hashBytes)
			.ToLowerInvariant(); ;


		await using var graphStream = new MemoryStream(graphBytes);

		if (!await _files.ExistsAsync(DerivedCollection, derivedHash, ct))
		{
			await _files.WriteAsync(
				collection: DerivedCollection,
				key: derivedHash,
				data: graphStream,
				ct);
		}


		//Todo add verion
		await _workspace.AddProcessedFileAsync(
			new ProcessedFile
			{
				StoredFileHash = storedFileHash,
				DerivedHash = extractedTextDerivedHash,
				ProcessorName = "GraphRAG",
				ProcessorVersion = "1.0",
				ProcessorKind = "graph",
				ProcessedAt = DateTimeOffset.UtcNow,
				MetadataJson = JsonSerializer.Serialize(metadata)
			},
			ct);

		_logger.LogInformation(
			"Graph extraction completed for {Hash}: {Entities} entities",
			storedFileHash[..12],
			result.Entities.Count);

		// 6. Queue entity linking (optional, eventual consistency)
		if (_entityLinking != null)
		{
			await _entityLinking.ExecuteAsync(
				storedFileHash,
				vf.WorkspaceId,
				ct);
		}
	}
}
