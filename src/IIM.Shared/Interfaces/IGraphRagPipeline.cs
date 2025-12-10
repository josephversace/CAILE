using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphRag.Config;
using IIM.Shared.Models;
using GraphRagConfig = GraphRag.Config.GraphRagConfig;

namespace IIM.Shared.Interfaces
{
	public interface IGraphRagPipeline
	{
		/// <summary>
		/// Runs the full GraphRAG indexing pipeline on a parsed document.
		/// Handles chunking, embeddings, graph extraction, community detection,
		/// summaries, and graph store updates.
		/// </summary>
		Task<GraphRagResult> ProcessAsync(IEnumerable<DocumentInput> documents, GraphRagConfig? config = null, CancellationToken ct = default);
	}
}
