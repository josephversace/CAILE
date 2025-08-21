using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{

    /// <summary>
    /// Response from RAG (Retrieval Augmented Generation) query
    /// </summary>
    public class RagResponse
    {
        public string Answer { get; set; } = string.Empty;
        public Source[] Sources { get; set; } = Array.Empty<Source>();
        public float Confidence { get; set; }
        public int TokensUsed { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }


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


    public class Entity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public EntityType Type { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> Aliases { get; set; } = new();
        public List<Relationship> Relationships { get; set; } = new();
        public List<string> AssociatedCaseIds { get; set; } = new();
        public double RiskScore { get; set; }
        public DateTimeOffset FirstSeen { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class Relationship
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SourceEntityId { get; set; } = string.Empty;
        public string TargetEntityId { get; set; } = string.Empty;
        public RelationshipType Type { get; set; }
        public double Strength { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
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

    /// <summary>
    /// Result from audio transcription using Whisper model
    /// </summary>
    public class MockTranscriptionResult
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public float Confidence { get; set; }
        public TimeSpan Duration { get; set; }
        public Word[] Words { get; set; } = Array.Empty<Word>();
        public TimeSpan ProcessingTime { get; set; }
        public string DeviceUsed { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bounding box for detected objects
    /// </summary>
    public class MockBoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }


    /// <summary>
    /// Individual word with timing information
    /// </summary>
    public class Word
    {
        public string Text { get; set; } = string.Empty;
        public float Start { get; set; }
        public float End { get; set; }
    }

    /// <summary>
    /// Results from CLIP image search
    /// </summary>
    public class ImageSearchResults
    {
        public List<ImageMatch> Matches { get; set; } = new();
        public TimeSpan QueryProcessingTime { get; set; }
        public int TotalImagesSearched { get; set; }
    }

    /// <summary>
    /// Individual image match result
    /// </summary>
    public class ImageMatch
    {
        public string ImagePath { get; set; } = string.Empty;
        public float Score { get; set; }
        public MockBoundingBox? BoundingBox { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }




    /// <summary>
    /// Document source for RAG response
    /// </summary>
    public class Source
    {
        public string Document { get; set; } = string.Empty;
        public int Page { get; set; }
        public float Relevance { get; set; }
    }
}
