using System;
using System.Collections.Generic;
using IIM.Shared.Dtos;

namespace IIM.Shared.Dtos;

/// <summary>
/// Request to add a graph entity (Person, Account, Device, Organization, File, Event)
/// to the Neo4j knowledge graph.
/// </summary>
public class AddGraphEntityRequest
{
    /// <summary>
    /// Target workspace ID
    /// </summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// Source file that this entity was extracted from (for REFERENCED_IN relationship)
    /// </summary>
    public Guid? SourceFileId { get; set; }

    /// <summary>
    /// Source file name for display/provenance
    /// </summary>
    public string? SourceFileName { get; set; }

    /// <summary>
    /// The entity to add to the graph
    /// </summary>
    public GraphEntityDto Entity { get; set; } = null!;
}

/// <summary>
/// Result of adding a graph entity
/// </summary>
public class AddGraphEntityResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The Neo4j node ID of the created entity
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// IDs of any related nodes created (e.g., indicator nodes linked to the entity)
    /// </summary>
    public List<string> RelatedNodeIds { get; set; } = new();

    /// <summary>
    /// Number of relationships created
    /// </summary>
    public int RelationshipsCreated { get; set; }
}
