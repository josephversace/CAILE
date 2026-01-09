using System;
using System.Collections.Generic;

namespace IIM.Shared.Models;

/// <summary>
/// Entity categories aligned to knowledge graph node types.
/// These represent the primary entities that can be committed to the graph.
/// </summary>
public enum GraphEntityCategory
{
    /// <summary>
    /// Human individuals - identified by name, DOB, personal contact info.
    /// Indicators: FullName, DateOfBirth, PhoneNumber (personal), EmailAddress (personal)
    /// </summary>
    Person,

    /// <summary>
    /// Digital accounts - usernames, emails, crypto wallets, social handles.
    /// Indicators: Username, EmailAddress, CryptoAddress, SocialHandle, PgpKeyId
    /// </summary>
    Account,

    /// <summary>
    /// Physical or virtual devices - phones, computers, network endpoints.
    /// Indicators: MacAddress, Imei, IpAddress (standalone), OnionAddress
    /// </summary>
    Device,

    /// <summary>
    /// Companies, groups, or institutions.
    /// Indicators: Domain (corporate), URL patterns, ASN
    /// </summary>
    Organization,

    /// <summary>
    /// Files with associated metadata - clustered filename, hash, path.
    /// Indicators: FileName, FileHash, FilePath (grouped by proximity)
    /// </summary>
    File,

    /// <summary>
    /// Temporal events - timestamp bound to another entity or action.
    /// Built from: Timestamp + Who/What/Where indicators
    /// </summary>
    Event,

    /// <summary>
    /// Raw indicators not yet assigned to an entity.
    /// Can be added individually with REFERENCED_IN relationship only.
    /// </summary>
    Ungrouped
}

/// <summary>
/// Represents a proposed graph entity ready for user review before committing to Neo4j.
/// </summary>
public class GraphEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The type of graph node this will become
    /// </summary>
    public GraphEntityCategory Category { get; set; }
    
    /// <summary>
    /// Display label for the entity (e.g., person name, filename, account username)
    /// </summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional sublabel for additional context
    /// </summary>
    public string? Sublabel { get; set; }
    
    /// <summary>
    /// Confidence score for the entity grouping (0.0 - 1.0)
    /// </summary>
    public float Confidence { get; set; } = 1.0f;
    
    /// <summary>
    /// Indicators that belong to this entity (will become properties or related nodes)
    /// </summary>
    public List<EntityIndicator> Indicators { get; set; } = new();
    
    /// <summary>
    /// For Events: the timestamp value
    /// </summary>
    public string? Timestamp { get; set; }
    
    /// <summary>
    /// For Events: the event type (Login, Upload, etc.)
    /// </summary>
    public string? EventType { get; set; }
    
    /// <summary>
    /// Source context for provenance
    /// </summary>
    public string? SourceContext { get; set; }
}

/// <summary>
/// An indicator belonging to a graph entity
/// </summary>
public class EntityIndicator
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The indicator type from extraction
    /// </summary>
    public IndicatorType Type { get; set; }
    
    /// <summary>
    /// Subtype for additional classification (e.g., "MD5", "FullName", "Mobile")
    /// </summary>
    public string? Subtype { get; set; }
    
    /// <summary>
    /// The extracted value
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence score for this indicator
    /// </summary>
    public float Confidence { get; set; } = 1.0f;
    
    /// <summary>
    /// Number of occurrences in the source document
    /// </summary>
    public int OccurrenceCount { get; set; } = 1;
    
    /// <summary>
    /// Role within the entity (e.g., "Who", "What", "Where" for events)
    /// </summary>
    public string? Role { get; set; }
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
