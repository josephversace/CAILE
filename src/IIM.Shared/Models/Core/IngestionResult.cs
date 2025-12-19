using System;

namespace IIM.Ingestion.Models;

public sealed class IngestionResult
{
    public string? StoredId { get; init; }
    public int ChunkCount { get; init; }
    public int EntityCount { get; init; }
    public int VectorCount { get; init; }
    public bool Deduplicated { get; init; }
    public DateTime CompletedAt { get; init; }
}
