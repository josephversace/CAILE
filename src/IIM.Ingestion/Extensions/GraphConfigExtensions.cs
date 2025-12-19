using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GraphRag.Config;
using IIM.Shared.Models;

namespace IIM.Ingestion.Extensions;

public static class GraphRagConfigMapper
{
	public static GraphRag.Config.GraphRagConfig ToGraphRagConfig(this IIM.Shared.Models.GraphRagConfig iimConfig)
	{
		var config = new GraphRag.Config.GraphRagConfig
		{
			RootDir = Directory.GetCurrentDirectory(),
			Models = new HashSet<string>(iimConfig.Models, StringComparer.OrdinalIgnoreCase),

			Input = new InputConfig
			{
				Storage = new GraphRag.Config.StorageConfig { BaseDir = "input", Type = StorageType.File }
			},
			Output = new GraphRag.Config.StorageConfig { BaseDir = "output", Type = StorageType.File },
			UpdateIndexOutput = new GraphRag.Config.StorageConfig { BaseDir = "output/update", Type = StorageType.File },

			//Cache = iimConfig.Cache.ToCacheConfig(),
			Chunks = iimConfig.Heuristics.ToChunkingConfig(),
			Heuristics = iimConfig.Heuristics.ToHeuristicMaintenanceConfig(),

			//ClusterGraph = iimConfig.ClusterGraph.ToClusterGraphConfig(),
			ExtractGraph = iimConfig.ExtractGraph.ToExtractGraphConfig(),
			ExtractClaims = iimConfig.ExtractClaims.ToClaimExtractionConfig(),

			// leave other sections at defaults unless you have inputs for them
			Reporting = new ReportingConfig(),
			VectorStore = new Dictionary<string, VectorStoreConfig> { ["default_vector_store"] = new() },
			Workflows = new List<string>(),
			EmbedText = new TextEmbeddingConfig(),
			EmbedGraph = new EmbedGraphConfig(),
			ExtractGraphNlp = new ExtractGraphNlpConfig(),
			SummarizeDescriptions = new SummarizeDescriptionsConfig(),
			PruneGraph = new PruneGraphConfig(),
			CommunityReports = new CommunityReportsConfig(),
			PromptTuning = new PromptTuningConfig(),
			Snapshots = new SnapshotsConfig(),
			Umap = new UmapConfig(),
			LocalSearch = new LocalSearchConfig(),
			GlobalSearch = new GlobalSearchConfig(),
			DriftSearch = new DriftSearchConfig(),
			BasicSearch = new BasicSearchConfig(),
			Extensions = new Dictionary<string, object?>()
		};

		return config;
	}

	public static ChunkingConfig ToChunkingConfig(this GraphRagHeuristics h) => new()
	{
		Size = h.MaxTokensPerTextUnit,
		Overlap = h.MinimumChunkOverlap
	};

	public static HeuristicMaintenanceConfig ToHeuristicMaintenanceConfig(this GraphRagHeuristics h) => new()
	{
		EnableSemanticDeduplication = h.EnableSemanticDeduplication,
		SemanticDeduplicationThreshold = h.SemanticDeduplicationThreshold,
		MaxTokensPerTextUnit = h.MaxTokensPerTextUnit,
		MaxDocumentTokenBudget = h.MaxDocumentTokenBudget,
		MaxTextUnitsPerRelationship = h.MaxTextUnitsPerRelationship,
		OrphanLinkMinimumOverlap = h.OrphanLinkMinimumOverlap,
		OrphanLinkWeight = h.OrphanLinkWeight,
		EnhanceRelationships = h.EnhanceRelationships,
		RelationshipConfidenceFloor = h.RelationshipConfidenceFloor,
		MinimumChunkOverlap = h.MinimumChunkOverlap,

		//EmbeddingModelId = h.EmbeddingModelId
		LinkOrphanEntities = h.LinkOrphanEntities
	};

	public static ExtractGraphConfig ToExtractGraphConfig(this GraphRagExtractGraph e) => new()
	{
		ModelId = e.ModelId,
		//SystemPrompt = e.SystemPrompt,
		//Prompt = e.UserPrompt,
		EntityTypes = e.EntityTypes?.ToList() ?? new(),
		MaxGleanings = e.MaxGleanings,
		//Strategy = e.Strategy
	};

	public static ClaimExtractionConfig ToClaimExtractionConfig(this GraphRagExtractClaims c) => new()
	{
		Enabled = c.Enabled,
		ModelId = c.ModelId
		//Prompt = c.Prompt,
		//Description = c.Description,
		//MaxGleanings = c.MaxGleanings,
		//Strategy = c.Strategy
	};

}
