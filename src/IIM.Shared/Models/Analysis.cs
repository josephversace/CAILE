using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models;

    public class AnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
  
        public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
        public string PerformedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Results { get; set; } = new();    
        public double Confidence { get; set; }
        public List<string> Tags { get; set; } = new();

    public AnalysisType Type { get; set; }
    public List<Finding> Findings { get; set; } = new();  // Using existing Finding from IIM.Shared.Models
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public TimeSpan AnalysisTime { get; set; }
    public float ConfidenceScore { get; set; }
}


/// <summary>
/// Citation reference with extended properties.
/// </summary>
public class Citation
{
    // Existing core properties
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? Location { get; set; }
    public double Relevance { get; set; }

    // New optional properties
    public int? Index { get; set; }  // Citation number in response
    public string? Source { get; set; }  // Source name/title
    public string? Url { get; set; }  // Link to source
    public DateTimeOffset? AccessedAt { get; set; }  // When cited
    public string? Author { get; set; }  // Source author
    public DateTimeOffset? PublishedAt { get; set; }  // Publication date
    public Dictionary<string, object>? Metadata { get; set; }
}



public class ImageAnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string EvidenceId { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public List<DetectedObject> Objects { get; set; } = new();
        public List<DetectedFace> Faces { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<SimilarImage> SimilarImages { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class DetectedObject
    {
        public string Label { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public BoundingBox BoundingBox { get; set; } = new();
    }

    public class DetectedFace
    {
        public string Id { get; set; } = string.Empty;
        public BoundingBox BoundingBox { get; set; } = new();
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public Dictionary<string, double> Emotions { get; set; } = new();
        public int? Age { get; set; }
        public string? Gender { get; set; }
    }

    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class SimilarImage
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public double Similarity { get; set; }
    }

