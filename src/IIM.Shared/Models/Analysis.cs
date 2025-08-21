using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class AnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string AnalysisType { get; set; } = string.Empty;
        public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
        public string PerformedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Results { get; set; } = new();
        public double Confidence { get; set; }
        public List<string> Tags { get; set; } = new();
    }


    // Citation stays in Core because it has ID generation logic
    public class Citation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Source { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public string? Location { get; set; }
        public double Relevance { get; set; }
    }


    public class TranscriptionResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string EvidenceId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<TranscriptionSegment> Segments { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class TranscriptionSegment
    {
        public int Start { get; set; }
        public int End { get; set; }
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string? Speaker { get; set; }
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

}
