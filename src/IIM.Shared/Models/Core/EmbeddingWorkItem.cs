using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public sealed record EmbeddingWorkItem
	{
		public required string Blake3Hash { get; init; }
		public required int ChunkIndex { get; init; }
		public required string Text { get; init; }
		public required int MaxTokens { get; init; }
		public required string SemanticType { get; init; }

		public IReadOnlyDictionary<string, string> Metadata { get; init; }
			= Empty;

		private static readonly IReadOnlyDictionary<string, string> Empty
			= new Dictionary<string, string>();
	}


}
