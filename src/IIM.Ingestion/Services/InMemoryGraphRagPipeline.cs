using System.Text;
using GraphRag.Config;
using GraphRag.Community;
using GraphRag.Data;
using GraphRag.Entities;
using GraphRag.Graphs;
using GraphRag.Indexing.Runtime;
using GraphRag.Relationships;
using GraphRag.Storage;
using IIM.Ingestion.Extensions;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GraphRagConfig = GraphRag.Config.GraphRagConfig;
using IIM.Ingestion.Models;

namespace IIM.Ingestion.Services;

public class InMemoryGraphRagPipeline : IGraphRagPipeline
{
	private readonly IServiceProvider _services;
	private readonly IPipelineFactory _pipelineFactory;
	private readonly PipelineExecutor _executor;
	private readonly IGraphStore? _graphStore;
	private readonly ILogger<InMemoryGraphRagPipeline> _logger;
	private readonly CaileConfig _caileConfig;

	public InMemoryGraphRagPipeline(
		IServiceProvider services,
		IGraphStore? graphStore,
		CaileConfig caileConfig,
		ILogger<InMemoryGraphRagPipeline> logger)
	{
		_services = services;
		_pipelineFactory = services.GetRequiredService<IPipelineFactory>();
		_executor = services.GetRequiredService<PipelineExecutor>();
		_graphStore = graphStore;
		_caileConfig = caileConfig;
		_logger = logger;
	}

	public async Task<GraphRagResult> ProcessAsync(
		IEnumerable<DocumentInput> documents,
		GraphRagConfig? config = null,
		CancellationToken ct = default)
	{

		config = _caileConfig.GraphRag.ToGraphRagConfig();
		// Call the full overload with null document context
		return await ProcessAsync(
			documents,
			documentId: null,
			workspaceId: null,
			virtualFileId: null,
			fileName: null,
			config,
			ct);
	}

	public async Task<GraphRagResult> ProcessAsync(
		IEnumerable<DocumentInput> documents,
		string? documentId,
		Guid? workspaceId,
		Guid? virtualFileId,
		string? fileName,
		GraphRagConfig? config = null,
		CancellationToken ct = default)
	{
		var inputStorage = new MemoryPipelineStorage();
		var outputStorage = new MemoryPipelineStorage();

		// Load documents into memory storage with .txt extension
		var fileNames = new List<string>();

		foreach (var doc in documents)
		{
			var bytes = doc.Content switch
			{
				byte[] b => b,
				Stream s => await ReadStreamAsync(s),
				string text => Encoding.UTF8.GetBytes(text),
				_ => throw new ArgumentException($"Unsupported content type for {doc.FileName}")
			};

			var normalizedName = NormalizeFileName(doc.FileName);
			fileNames.Add(normalizedName);

			_logger.LogDebug(
				"Storing document {Original} as {Normalized} ({Bytes} bytes)",
				doc.FileName, normalizedName, bytes.Length);

			await inputStorage.SetAsync(normalizedName, new MemoryStream(bytes), cancellationToken: ct);
		}

		config ??= CreateDefaultConfig();

		var pipeline = _pipelineFactory.BuildIndexingPipeline(IndexingPipelineDefinitions.Standard);
		var context = PipelineContextFactory.Create(
			inputStorage: inputStorage,
			outputStorage: outputStorage,
			services: _services
		);

		var errors = new List<Exception>();
		await foreach (var result in _executor.ExecuteAsync(pipeline, config, context, ct))
		{
			if (result.Errors is { Count: > 0 })
			{
				errors.AddRange(result.Errors);
				foreach (var error in result.Errors)
				{
					_logger.LogWarning(error, "GraphRAG workflow error in {Workflow}", result.Workflow);
				}
			}
		}

		// Load results from output storage
		var textUnits = await LoadTableSafeAsync<TextUnitRecord>(outputStorage, "text_units", ct);
		var docs = await LoadTableSafeAsync<DocumentRecord>(outputStorage, "documents", ct);
		var entities = await LoadTableSafeAsync<EntityRecord>(outputStorage, "entities", ct);
		var relationships = await LoadTableSafeAsync<RelationshipRecord>(outputStorage, "relationships", ct);

		_logger.LogWarning(
  "GraphRAG tables: TextUnits={TextUnits}, Entities={Entities}, Relationships={Relationships}",
  textUnits.Count,
  entities.Count,
  relationships.Count);

		if (entities.Count > 0)
		{
			_logger.LogWarning(
			  "First entities: {Entities}",
			  string.Join(", ", entities.Take(10).Select(e => $"{e.Title} ({e.Type})")));
		}


		var communities = await LoadTableSafeAsync<CommunityRecord>(outputStorage, "communities", ct);
		var communityReports = await LoadTableSafeAsync<CommunityReportRecord>(outputStorage, "community_reports", ct);

		// Persist to Neo4j if we have entities and a graph store
		int neo4jNodeCount = 0;
		int neo4jRelCount = 0;

		if (_graphStore != null && entities.Count > 0)
		{
			try
			{
				(neo4jNodeCount, neo4jRelCount) = await PersistToNeo4jAsync(
					documentId,
					workspaceId,
					virtualFileId,
					fileName,
					entities,
					relationships,
					communities,
					ct);

				_logger.LogInformation(
					"Persisted {Nodes} nodes and {Rels} relationships to Neo4j for {DocumentId}",
					neo4jNodeCount,
					neo4jRelCount,
					documentId?[..Math.Min(12, documentId.Length)] ?? "unknown");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to persist graph to Neo4j");
				errors.Add(ex);
			}
		}
		else if (_graphStore == null && entities.Count > 0)
		{
			_logger.LogWarning(
				"GraphStore not configured. {EntityCount} entities extracted but not persisted to Neo4j.",
				entities.Count);
		}

		var graphResult = new GraphRagResult
		{
			TextUnits = textUnits,
			Documents = docs,
			Entities = entities,
			Relationships = relationships,
			Communities = communities,
			CommunityReports = communityReports,
			Neo4jNodeCount = neo4jNodeCount,
			Neo4jRelationshipCount = neo4jRelCount,
			Errors = errors
		};

		_logger.LogInformation(
			"GraphRAG completed: {TextUnits} text units, {Entities} entities, {Relationships} relationships, {Communities} communities, {Errors} errors",
			graphResult.TextUnits.Count,
			graphResult.Entities.Count,
			graphResult.Relationships.Count,
			graphResult.Communities.Count,
			errors.Count);

		return graphResult;
	}

	// ────────────────────────────────────────────────────────────────
	// NEO4J PERSISTENCE
	// ────────────────────────────────────────────────────────────────

	private async Task<(int NodeCount, int RelCount)> PersistToNeo4jAsync(
		string? documentId,
		Guid? workspaceId,
		Guid? virtualFileId,
		string? fileName,
		IReadOnlyList<EntityRecord> entities,
		IReadOnlyList<RelationshipRecord> relationships,
		IReadOnlyList<CommunityRecord> communities,
		CancellationToken ct)
	{
		var nodeCount = 0;
		var relCount = 0;

		// 1. Create Document node if we have context
		if (!string.IsNullOrEmpty(documentId))
		{
			await _graphStore!.UpsertNodeAsync(
				id: documentId,
				label: "Document",
				properties: new Dictionary<string, object?>
				{
					["blake3_hash"] = documentId,
					["workspace_id"] = workspaceId?.ToString(),
					["virtual_file_id"] = virtualFileId?.ToString(),
					["file_name"] = fileName,
					["entity_count"] = entities.Count,
					["relationship_count"] = relationships.Count,
					["indexed_at"] = DateTimeOffset.UtcNow.ToString("O")
				},
				ct);
			nodeCount++;
		}

		// 2. Create Entity nodes (batch)
		if (entities.Count > 0)
		{
			var entityNodes = entities.Select(e => new GraphNodeUpsert(
				Id: e.Id,
				Label: SanitizeLabel(e.Type),
				Properties: new Dictionary<string, object?>
				{
					["title"] = e.Title,
					["description"] = e.Description,
					["frequency"] = e.Frequency,
					["degree"] = e.Degree,
					["human_readable_id"] = e.HumanReadableId,
					["x"] = e.X,
					["y"] = e.Y
				}
			)).ToList();

			await _graphStore!.UpsertNodesAsync(entityNodes, ct);
			nodeCount += entityNodes.Count;
		}

		// 3. Create Community nodes
		if (communities.Count > 0)
		{
			var communityNodes = communities.Select(c => new GraphNodeUpsert(
				Id: c.Id,
				Label: "Community",
				Properties: new Dictionary<string, object?>
				{
					["title"] = c.Title,
					["level"] = c.Level,
					["size"] = c.Size,
					["community_id"] = c.CommunityId,
					["period"] = c.Period
				}
			)).ToList();

			await _graphStore!.UpsertNodesAsync(communityNodes, ct);
			nodeCount += communityNodes.Count;
		}

		// 4. Create Entity-to-Entity relationships (batch)
		if (relationships.Count > 0)
		{
			var entityRels = relationships.Select(r => new GraphRelationshipUpsert(
				SourceId: r.Source,
				TargetId: r.Target,
				Type: SanitizeLabel(r.Type),
				Properties: new Dictionary<string, object?>
				{
					["description"] = r.Description,
					["weight"] = r.Weight,
					["combined_degree"] = r.CombinedDegree,
					["human_readable_id"] = r.HumanReadableId
				},
				Bidirectional: r.Bidirectional
			)).ToList();

			await _graphStore!.UpsertRelationshipsAsync(entityRels, ct);
			relCount += entityRels.Count;
		}

		// 5. Create Document -> Entity relationships (MENTIONS)
		if (!string.IsNullOrEmpty(documentId) && entities.Count > 0)
		{
			var mentionsRels = entities.Select(e => new GraphRelationshipUpsert(
				SourceId: documentId,
				TargetId: e.Id,
				Type: "MENTIONS",
				Properties: new Dictionary<string, object?>
				{
					["extracted_at"] = DateTimeOffset.UtcNow.ToString("O")
				},
				Bidirectional: false
			)).ToList();

			await _graphStore!.UpsertRelationshipsAsync(mentionsRels, ct);
			relCount += mentionsRels.Count;
		}

		// 6. Create Community -> Entity relationships (CONTAINS)
		foreach (var community in communities)
		{
			if (community.EntityIds.Length == 0)
				continue;

			var containsRels = community.EntityIds.Select(entityId => new GraphRelationshipUpsert(
				SourceId: community.Id,
				TargetId: entityId,
				Type: "CONTAINS",
				Properties: new Dictionary<string, object?>(),
				Bidirectional: false
			)).ToList();

			await _graphStore!.UpsertRelationshipsAsync(containsRels, ct);
			relCount += containsRels.Count;
		}

		// 7. Create Workspace node and link Document (optional, for multi-tenancy)
		if (workspaceId.HasValue && !string.IsNullOrEmpty(documentId))
		{
			var workspaceIdStr = workspaceId.Value.ToString();

			await _graphStore!.UpsertNodeAsync(
				id: workspaceIdStr,
				label: "Workspace",
				properties: new Dictionary<string, object?>
				{
					["workspace_id"] = workspaceIdStr
				},
				ct);
			nodeCount++;

			await _graphStore!.UpsertRelationshipAsync(
				sourceId: workspaceIdStr,
				targetId: documentId,
				type: "CONTAINS_DOCUMENT",
				properties: new Dictionary<string, object?>
				{
					["added_at"] = DateTimeOffset.UtcNow.ToString("O")
				},
				ct);
			relCount++;
		}

		return (nodeCount, relCount);
	}

	// ────────────────────────────────────────────────────────────────
	// HELPERS
	// ────────────────────────────────────────────────────────────────

	private static string SanitizeLabel(string? label)
	{
		if (string.IsNullOrWhiteSpace(label))
			return "Entity";

		// Neo4j labels: alphanumeric + underscore only, must start with letter
		var sanitized = string.Concat(label
			.Replace(" ", "_")
			.Replace("-", "_")
			.Where(c => char.IsLetterOrDigit(c) || c == '_'));

		// Ensure it starts with a letter
		if (sanitized.Length > 0 && !char.IsLetter(sanitized[0]))
		{
			sanitized = "E_" + sanitized;
		}

		return string.IsNullOrEmpty(sanitized) ? "Entity" : sanitized;
	}

	private static string NormalizeFileName(string fileName)
	{
		var baseName = Path.GetFileNameWithoutExtension(fileName);
		baseName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars()));
		return $"{baseName}.txt";
	}

	private static async Task<byte[]> ReadStreamAsync(Stream stream)
	{
		using var ms = new MemoryStream();
		if (stream.CanSeek)
			stream.Position = 0;
		await stream.CopyToAsync(ms);
		return ms.ToArray();
	}

	private static async Task<IReadOnlyList<T>> LoadTableSafeAsync<T>(
		IPipelineStorage storage,
		string tableName,
		CancellationToken ct)
	{
		try
		{
			return await storage.LoadTableAsync<T>(tableName, ct);
		}
		catch (FileNotFoundException)
		{
			return Array.Empty<T>();
		}
		catch (KeyNotFoundException)
		{
			return Array.Empty<T>();
		}
	}

	private static GraphRagConfig CreateDefaultConfig() => new()
	{
		Input = new InputConfig
		{
			FileType = InputFileType.Text,
			FilePattern = @".*\.txt$"
		},
		Chunks = new ChunkingConfig
		{
			Size = 1200,
			Overlap = 100
		},
		ExtractGraph = new ExtractGraphConfig
		{
			ModelId = "chat_model",
			EntityTypes = ["person", "organization", "location", "event", "concept", "technology", "date"]
		},
		ClusterGraph = new ClusterGraphConfig
		{
			Algorithm = CommunityDetectionAlgorithm.FastLabelPropagation,
			MaxClusterSize = 25,
			MaxIterations = 40
		},
		CommunityReports = new CommunityReportsConfig
		{
			ModelId = "chat_model",
			MaxLength = 2000
		},
		Heuristics = new HeuristicMaintenanceConfig
		{
			EnableSemanticDeduplication = true,
			SemanticDeduplicationThreshold = 0.92,
			MaxTokensPerTextUnit = 1200,
			LinkOrphanEntities = true,
			EnhanceRelationships = true
		}
	};
}