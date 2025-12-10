using System;
using System.Collections.Generic;
using System.Text;
using GraphRag.Chunking;
using GraphRag.Config;

namespace IIM.Ingestion.Services
{
	public class ChunkingService(IChunkerResolver chunkerResolver)
	{
		public IReadOnlyList<TextChunk> ChunkDocument(string documentId, string text)
		{
			var config = new ChunkingConfig
			{
				Size = 1024,
				Overlap = 128,
				Strategy = ChunkStrategyType.Sentence
			};

			var chunker = chunkerResolver.Resolve(config.Strategy);
			var slices = new[] { new ChunkSlice(documentId, text) };

			return chunker.Chunk(slices, config);
		}
	}
}
