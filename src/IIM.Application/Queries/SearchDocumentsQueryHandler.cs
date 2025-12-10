using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Mediator;
using IIM.Shared.Models;

namespace IIM.Application.Queries
{
	/// <summary>
	/// Temporary stub implementation of document search.
	/// 
	/// This exists to remove the legacy IInferenceService dependency and
	/// let the system compile and run with the new architecture.
	/// 
	/// Later we will:
	/// - Use Docling to parse documents
	/// - Use the embedding service + Qdrant for semantic search
	/// - Use IInferencePipeline for answer synthesis
	/// </summary>
	public class SearchDocumentsQueryHandler
		: IRequestHandler<SearchDocumentsQuery, RAGSearchResult>
	{
		public Task<RAGSearchResult> Handle(
			SearchDocumentsQuery request,
			CancellationToken cancellationToken)
		{
			// TODO: Re-implement using:
			// - Docling -> parsed docs
			// - EmbedService + Qdrant -> nearest neighbors
			// - IInferencePipeline -> final answer synthesis

			// For now, return an empty result so the app can run.
			var result = new RAGSearchResult();
			return Task.FromResult(result);
		}
	}
}
