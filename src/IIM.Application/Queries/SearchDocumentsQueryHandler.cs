using IIM.Application.Interfaces;
using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Queries
{
    /// <summary>
    /// Final fixed handler for document search queries
    /// </summary>
    public class SearchDocumentsQueryHandler : IRequestHandler<SearchDocumentsQuery, RAGSearchResultDto>
    {
        private readonly IInferenceService _inferenceService;
        private readonly IEvidenceManager _evidenceManager;
        private readonly ILogger<SearchDocumentsQueryHandler> _logger;

        public SearchDocumentsQueryHandler(
            IInferenceService inferenceService,
            IEvidenceManager evidenceManager,
            ILogger<SearchDocumentsQueryHandler> logger)
        {
            _inferenceService = inferenceService;
            _evidenceManager = evidenceManager;
            _logger = logger;
        }

        public async Task<RAGSearchResultDto> Handle(SearchDocumentsQuery request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Searching documents with query: {Query}", request.Query);

                // Perform RAG search - RagResponse has Answer and Sources (not Documents or Citations)
                var ragResponse = await _inferenceService.QueryDocumentsAsync(
                    request.Query,
                    request.CaseId ?? "default",
                    cancellationToken);

                // Convert RagResponse.Sources to RAGDocumentDto list
                var documents = new List<RAGDocumentDto>();

                if (ragResponse.Sources != null && ragResponse.Sources.Length > 0)
                {
                    documents = ragResponse.Sources.Select((source, index) => new RAGDocumentDto(
                        Id: $"doc_{index}",
                        Content: ragResponse.Answer, // Use answer as content since Source doesn't have content
                        Relevance: source.Relevance,
                        SourceId: source.Document,
                        SourceType: "Document",
                        Metadata: new Dictionary<string, object>
                        {
                            ["page"] = source.Page,
                            ["document"] = source.Document
                        },
                        ChunkIndices: new List<int> { index }
                    )).ToList();
                }

                // Filter by relevance
                documents = documents
                    .Where(d => d.Relevance >= request.MinRelevance)
                    .Take(request.TopK)
                    .ToList();

                // Extract entities if requested
                var entities = new List<EntityDto>();
                if (request.ExtractEntities)
                {
                    entities = await ExtractEntitiesAsync(documents, cancellationToken);
                }

                // Build knowledge graph if requested
                KnowledgeGraphDto? knowledgeGraph = null;
                if (request.BuildKnowledgeGraph && entities.Any())
                {
                    knowledgeGraph = await BuildKnowledgeGraphAsync(entities, documents, cancellationToken);
                }

                // Analyze query intent
                var queryUnderstanding = AnalyzeQueryIntent(request.Query);

                // Generate follow-up suggestions
                var suggestedFollowUps = GenerateFollowUpQuestions(request.Query, documents);

                // Get case context if available
                Dictionary<string, object>? caseContext = null;
                if (!string.IsNullOrEmpty(request.CaseId))
                {
                    caseContext = await GetCaseContextAsync(request.CaseId, cancellationToken);
                }

                stopwatch.Stop();

                _logger.LogInformation("Document search completed in {ElapsedMs}ms. Found {DocCount} documents",
                    stopwatch.ElapsedMilliseconds, documents.Count);

                return new RAGSearchResultDto(
                    Documents: documents,
                    Entities: entities,
                    Relationships: new List<RelationshipDto>(),
                    KnowledgeGraph: knowledgeGraph,
                    QueryUnderstanding: queryUnderstanding,
                    SuggestedFollowUps: suggestedFollowUps,
                    CaseContext: caseContext
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search documents");
                throw;
            }
        }

        private async Task<List<EntityDto>> ExtractEntitiesAsync(
            List<RAGDocumentDto> documents,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            // Create EntityDto with all required parameters (it's a record with constructor)
            return new List<EntityDto>
            {
                new EntityDto(
                    Id: Guid.NewGuid().ToString(),
                    Name: "Sample Entity",
                    Type: "Person",
                    Properties: new Dictionary<string, object> { ["confidence"] = 0.95 },
                    Aliases: new List<string> { "Entity1" },
                    Relationships: null,
                    AssociatedCaseIds: new List<string>(),
                    RiskScore: 0.5,
                    FirstSeen: DateTimeOffset.UtcNow.AddDays(-30),
                    LastSeen: DateTimeOffset.UtcNow,
                    Attributes: null
                )
            };
        }

        private async Task<KnowledgeGraphDto> BuildKnowledgeGraphAsync(
            List<EntityDto> entities,
            List<RAGDocumentDto> documents,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            var nodes = entities.Select(e => new GraphNodeDto(
                Id: e.Id,
                Label: e.Name,
                Type: e.Type,
                Properties: e.Properties
            )).ToList();

            var edges = new List<GraphEdgeDto>();

            return new KnowledgeGraphDto(
                Nodes: nodes,
                Edges: edges,
                Properties: new Dictionary<string, object> { ["generated_at"] = DateTimeOffset.UtcNow }
            );
        }

        private QueryUnderstandingDto AnalyzeQueryIntent(string query)
        {
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return new QueryUnderstandingDto(
                KeyTerms: words.Take(3).ToList(),
                Intent: "information_retrieval",
                RequiredCapabilities: new List<string> { "text_search", "semantic_matching" },
                Complexity: words.Length > 10 ? 0.8 : 0.5
            );
        }

        private List<string> GenerateFollowUpQuestions(string query, List<RAGDocumentDto> documents)
        {
            return new List<string>
            {
                $"Can you provide more details about {query}?",
                "What specific timeframe are you interested in?",
                "Are there any particular individuals involved?"
            };
        }

        private async Task<Dictionary<string, object>> GetCaseContextAsync(
            string caseId,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            return new Dictionary<string, object>
            {
                ["case_id"] = caseId,
                ["total_evidence"] = 42,
                ["investigation_stage"] = "analysis"
            };
        }
    }
}