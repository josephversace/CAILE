using System;

namespace IIM.Shared.Events;

/// <summary>
/// SignalR event broadcast when file ingestion completes.
/// </summary>
public sealed class FileIngestedEvent
{
    public required Guid VirtualFileId { get; init; }
    public required Guid WorkspaceId { get; init; }
    public required bool Success { get; init; }
    
    /// <summary>
    /// The content-addressable hash. Null if ingestion failed.
    /// </summary>
    public string? Blake3Hash { get; init; }
    
    public string? FileName { get; init; }
    public string? MimeType { get; init; }
    
    /// <summary>
    /// Number of text chunks extracted.
    /// </summary>
    public int ChunkCount { get; init; }
    
    /// <summary>
    /// Number of entities extracted to knowledge graph.
    /// </summary>
    public int EntityCount { get; init; }
    
    /// <summary>
    /// Error message if Success is false.
    /// </summary>
    public string? Error { get; init; }
}
