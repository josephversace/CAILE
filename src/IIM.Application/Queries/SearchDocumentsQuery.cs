using IIM.Core.Mediator;
using IIM.Shared.DTOs;
using IIM.Shared.DTOs;
using System.Collections.Generic;

namespace IIM.Application.Queries
{
    /// <summary>
    /// Query to search documents using RAG pipeline
    /// </summary>
    public class SearchDocumentsQuery : IQuery<RAGSearchResult>
    {
        /// <summary>
        /// Search query text
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Case ID to search within (optional)
        /// </summary>
        public string? CaseId { get; set; }

        /// <summary>
        /// Number of top results to return
        /// </summary>
        public int TopK { get; set; } = 5;

        /// <summary>
        /// Minimum relevance score (0.0 - 1.0)
        /// </summary>
        public double MinRelevance { get; set; } = 0.5;

        /// <summary>
        /// Time range filter
        /// </summary>
        public TimeRangeDto? TimeRange { get; set; }

        /// <summary>
        /// Tags to filter by
        /// </summary>
        public List<string>? FilterTags { get; set; }

        /// <summary>
        /// Document types to include
        /// </summary>
        public List<string>? DocumentTypes { get; set; }

        /// <summary>
        /// Whether to include entity extraction
        /// </summary>
        public bool ExtractEntities { get; set; } = true;

        /// <summary>
        /// Whether to build knowledge graph
        /// </summary>
        public bool BuildKnowledgeGraph { get; set; } = false;

        /// <summary>
        /// Whether to rerank results
        /// </summary>
        public bool RerankResults { get; set; } = true;
    }
}
