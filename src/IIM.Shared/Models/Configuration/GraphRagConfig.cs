namespace IIM.Shared.Models;

public class GraphRagConfig
{
	public string[] Models { get; set; } = new string[0];
	public GraphRagStores GraphStores { get; set; } = new();
	public GraphRagHeuristics Heuristics { get; set; } = new();
	public GraphRagClusterGraph ClusterGraph { get; set; } = new();
	public GraphRagCache Cache { get; set; } = new();
	public GraphRagExtractClaims ExtractClaims { get; set; } = new();
}

public class GraphRagStores
{
	public GraphRagStoreConfig Neo4j { get; set; } = new();
}

public class GraphRagStoreConfig
{
	public string Uri { get; set; } = "";
	public string Username { get; set; } = "";
	public string Password { get; set; } = "";
	public string Database { get; set; } = "neo4j";
}

public class GraphRagHeuristics
{
	public int MinimumChunkOverlap { get; set; }
	public bool EnableSemanticDeduplication { get; set; }
	public double SemanticDeduplicationThreshold { get; set; }
	public int MaxTokensPerTextUnit { get; set; }
	public int MaxDocumentTokenBudget { get; set; }
	public int MaxTextUnitsPerRelationship { get; set; }
	public bool LinkOrphanEntities { get; set; }
	public double OrphanLinkMinimumOverlap { get; set; }
	public double OrphanLinkWeight { get; set; }
	public bool EnhanceRelationships { get; set; }
	public double RelationshipConfidenceFloor { get; set; }
}


public class GraphRagClusterGraph
{
	public string Algorithm { get; set; } = "";
	public int MaxIterations { get; set; }
	public int MaxClusterSize { get; set; }
	public bool UseLargestConnectedComponent { get; set; }
	public uint Seed { get; set; }
}

public class GraphRagCache
{
	public string Type { get; set; } = "Memory";
}

public class GraphRagExtractClaims
{
	public bool Enabled { get; set; }
	public string ModelId { get; set; } = "";
}

