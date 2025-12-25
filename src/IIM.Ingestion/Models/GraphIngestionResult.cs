using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Ingestion.Models
{
	public record VectorIndexResult
	{
		public int ChunkCount { get; init; }
		public int VectorCount { get; init; }
	}

	public record GraphExtractionResult
	{
		public int EntityCount { get; init; }
		public int RelationshipCount { get; init; }
	}
}
