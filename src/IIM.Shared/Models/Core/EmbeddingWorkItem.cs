using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public sealed record EmbeddingWorkItem
	{
		public required string Blake3Hash { get; init; }
		public required int ChunkIndex { get; init; }

		public required string Text { get; init; } // bounded by construction

		public required int MaxTokens { get; init; }

		public required string SemanticType { get; init; } // paragraph, table, caption
		public required IReadOnlyDictionary<string, string> Metadata { get; init; }
	}

}
