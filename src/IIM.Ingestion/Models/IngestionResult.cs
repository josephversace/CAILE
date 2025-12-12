namespace IIM.Ingestion.Models
{
	public class IngestionResult
	{
		public string StoredId { get; set; }
		public int ChunkCount { get; set; }
		public int EntityCount { get; set; }

		public int VectorCount { get; set; }
		public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;

		public Dictionary<string, string>? Metadata { get; set; }
	}
}
