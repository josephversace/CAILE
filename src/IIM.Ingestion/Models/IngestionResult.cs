public record IngestionResult
{
	public required string StoredId { get; init; }
	public bool Deduplicated { get; init; }
	public int ChunkCount { get; init; }
	public int VectorCount { get; init; }
	public int EntityCount { get; init; }
	public int RelationshipCount { get; init; }
	public bool GraphExtractionFailed { get; init; }
	public DateTime CompletedAt { get; init; }
}