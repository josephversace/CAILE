using System;
using System.Collections.Generic;


namespace IIM.Core.Models;

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

// AnalysisResult stays in Core - it has business logic
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

// GeoLocation and TimeRange are now in IIM.Shared.Models
