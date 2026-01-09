using System;
using System.Collections.Generic;
using IIM.Shared.Models;

namespace IIM.Shared.Dtos;

/// <summary>
/// DTO for graph entity extraction results - replaces EntityGroupDto
/// Aligned to knowledge graph node types for controlled insertion
/// </summary>
public class GraphEntityDto
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Graph node type: Person, Account, Device, Organization, File, Event
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Primary display label
    /// </summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// Secondary label for context
    /// </summary>
    public string? Sublabel { get; set; }
    
    /// <summary>
    /// Grouping confidence (0.0 - 1.0)
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// Indicators belonging to this entity
    /// </summary>
    public List<GraphEntityIndicatorDto> Indicators { get; set; } = new();
    
    /// <summary>
    /// For Events: timestamp value
    /// </summary>
    public string? Timestamp { get; set; }
    
    /// <summary>
    /// For Events: event type (Login, Upload, etc.)
    /// </summary>
    public string? EventType { get; set; }
    
    /// <summary>
    /// Context snippet for provenance
    /// </summary>
    public string? SourceContext { get; set; }
}

/// <summary>
/// DTO for indicators within a graph entity
/// </summary>
public class GraphEntityIndicatorDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Subtype { get; set; }
    public string Value { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public int OccurrenceCount { get; set; }
    
    /// <summary>
    /// Role within entity: "Primary", "Who", "What", "Where", "Property"
    /// </summary>
    public string? Role { get; set; }
}

/// <summary>
/// Updated extraction result with graph-aligned entities
/// </summary>
public class GraphExtractionResultDto
{
    /// <summary>
    /// Entities grouped and ready for graph insertion
    /// </summary>
    public List<GraphEntityDto> Entities { get; set; } = new();
    
    /// <summary>
    /// Ungrouped indicators that can be added individually
    /// </summary>
    public List<GraphEntityIndicatorDto> UngroupedIndicators { get; set; } = new();
    
    /// <summary>
    /// Extraction statistics
    /// </summary>
    public ExtractionStatsDto Stats { get; set; } = new();
}

public class ExtractionStatsDto
{
    public int TotalIndicators { get; set; }
    public int GroupedCount { get; set; }
    public int UngroupedCount { get; set; }
    public int EntityCount { get; set; }
    public TimeSpan Duration { get; set; }
    public bool TimedOut { get; set; }
    
    public Dictionary<string, int> CountsByCategory { get; set; } = new();
    public Dictionary<string, int> CountsByType { get; set; } = new();
}
