using System;
using System.Collections.Generic;
using System.Linq;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Analysis result from evidence processing
    /// </summary>
    public class AnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EvidenceId { get; set; } = string.Empty;
        public AnalysisType Type { get; set; }
        public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
        public string PerformedBy { get; set; } = string.Empty;
        public TimeSpan AnalysisTime { get; set; }
        public float ConfidenceScore { get; set; }

        // Results
        public List<Finding> Findings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, object> Results { get; set; } = new();
        public List<string> Tags { get; set; } = new();

        // Metadata
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool IsHighConfidence()
        {
            return ConfidenceScore >= 0.8f;
        }

        public bool HasFindings()
        {
            return Findings.Any();
        }
    }

    /// <summary>
    /// Image analysis result
    /// </summary>
    public class ImageAnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EvidenceId { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public List<DetectedObject> Objects { get; set; } = new();
        public List<DetectedFace> Faces { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<SimilarImage> SimilarImages { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool HasFaces()
        {
            return Faces.Any();
        }

        public bool HasObjects()
        {
            return Objects.Any();
        }

        public int GetObjectCount(string label)
        {
            return Objects.Count(o => o.Label.Equals(label, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Detected object in image
    /// </summary>
    public class DetectedObject
    {
        public string Label { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public BoundingBox BoundingBox { get; set; } = new();
        public Dictionary<string, object> Attributes { get; set; } = new();

        public bool IsHighConfidence()
        {
            return Confidence >= 0.8;
        }
    }

    /// <summary>
    /// Detected face in image
    /// </summary>
    public class DetectedFace
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public BoundingBox BoundingBox { get; set; } = new();
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public Dictionary<string, double> Emotions { get; set; } = new();
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public double? Confidence { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public string GetDominantEmotion()
        {
            return Emotions.Any()
                ? Emotions.OrderByDescending(e => e.Value).First().Key
                : "neutral";
        }
    }

    /// <summary>
    /// Bounding box for object detection
    /// </summary>
    public class BoundingBox
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double GetArea()
        {
            return Width * Height;
        }

        public bool Contains(double x, double y)
        {
            return x >= X && x <= X + Width &&
                   y >= Y && y <= Y + Height;
        }

        public bool Intersects(BoundingBox other)
        {
            return !(other.X > X + Width ||
                    other.X + other.Width < X ||
                    other.Y > Y + Height ||
                    other.Y + other.Height < Y);
        }
    }

    /// <summary>
    /// Similar image found in analysis
    /// </summary>
    public class SimilarImage
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public double Similarity { get; set; }
        public string? ThumbnailPath { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool IsHighSimilarity()
        {
            return Similarity >= 0.9;
        }
    }

    /// <summary>
    /// Transcription result from audio/video
    /// </summary>
    public class TranscriptionResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EvidenceId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public double Confidence { get; set; }
        public TimeSpan Duration { get; set; }
        public List<TranscriptionSegment> Segments { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        public string GetFullTranscript()
        {
            return string.Join(" ", Segments.Select(s => s.Text));
        }

        public bool IsHighConfidence()
        {
            return Confidence >= 0.8;
        }
    }

    /// <summary>
    /// Segment of a transcription
    /// </summary>
    public class TranscriptionSegment
    {
        public int Index { get; set; }
        public string Text { get; set; } = string.Empty;
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public double Confidence { get; set; }
        public string? Speaker { get; set; }

        public TimeSpan Duration => End - Start;
    }

    /// <summary>
    /// RAG search result
    /// </summary>
    public class RAGSearchResult
    {
        public string Query { get; set; } = string.Empty;
        public List<RAGDocument> Documents { get; set; } = new();
        public double TotalRelevance { get; set; }
        public TimeSpan SearchTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public RAGDocument? GetTopDocument()
        {
            return Documents.OrderByDescending(d => d.Relevance).FirstOrDefault();
        }

        public bool HasHighRelevanceResults()
        {
            return Documents.Any(d => d.Relevance >= 0.8);
        }
    }

    /// <summary>
    /// RAG document result
    /// </summary>
    public class RAGDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Relevance { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public List<float> Embedding { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        public bool IsHighRelevance()
        {
            return Relevance >= 0.8;
        }

        public string GetSnippet(int maxLength = 200)
        {
            return Content.Length <= maxLength
                ? Content
                : Content.Substring(0, maxLength) + "...";
        }
    }
}