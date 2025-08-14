
using System;
using System.Collections.Generic;

namespace IIM.Core.Models;

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
