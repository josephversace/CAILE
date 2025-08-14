
namespace IIM.Core.Models;

public class RAGSearchResult
{
    public List<RAGDocument> Documents { get; set; } = new();
    public List<Entity> Entities { get; set; } = new();
    public List<Relationship> Relationships { get; set; } = new();
    public KnowledgeGraph? KnowledgeGraph { get; set; }
    public QueryUnderstanding QueryUnderstanding { get; set; } = new();
    public List<string> SuggestedFollowUps { get; set; } = new();
    public Dictionary<string, object> CaseContext { get; set; } = new();
}

public class RAGDocument
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Relevance { get; set; }
    public string? SourceId { get; set; }
    public string? SourceType { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<int> ChunkIndices { get; set; } = new();
}

public class KnowledgeGraph
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class GraphEdge
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Weight { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class QueryUnderstanding
{
    public List<string> KeyTerms { get; set; } = new();
    public string Intent { get; set; } = string.Empty;
    public List<string> RequiredCapabilities { get; set; } = new();
    public double Complexity { get; set; }
}