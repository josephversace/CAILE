using IIM.Application.Interfaces;
using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Core.Services;

using IIM.Shared.Enums;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Application.Queries
{
    /// <summary>
    /// Final fixed handler for document search queries
    /// </summary>
    public class SearchDocumentsQueryHandler : IRequestHandler<SearchDocumentsQuery, RAGSearchResult>
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

        public async Task<RAGSearchResult> Handle(SearchDocumentsQuery request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Searching documents with query: {Query}", request.Query);

                // Perform RAG search
                var ragResponse = await _inferenceService.QueryDocumentsAsync(
                    request.Query,
                    request.CaseId ?? "default",
                    cancellationToken);

                // Map ragResponse.Sources (List<string>) to RAGDocument list
                var documents = new List<RAGDocument>();

                if (ragResponse.Sources != null && ragResponse.Sources.Any())
                {
                    documents = ragResponse.Sources.Select((source, index) => new RAGDocument
                    {
                        Id = source,
                        Content = ragResponse.Answer,        // Only field you have; replace if you want different content
                        Relevance = 1.0,                     // No relevance value; set default
                        SourceId = source,
                        SourceType = "Document",
                        Metadata = new Dictionary<string, object> { ["index"] = index },
                        ChunkIndices = new List<int> { index }
                    }).ToList();
                }

                // Filter by relevance
                documents = documents
                    .Where(d => d.Relevance >= request.MinRelevance)
                    .Take(request.TopK)
                    .ToList();

                // Extract entities if requested (use your real logic here)
                var entities = new List<Entity>();
                if (request.ExtractEntities)
                {
                    entities = await ExtractEntitiesAsync(documents, cancellationToken);
                }

                // Build knowledge graph if requested
                KnowledgeGraph? knowledgeGraph = null;
                if (request.BuildKnowledgeGraph && entities.Any())
                {
                    knowledgeGraph = await BuildKnowledgeGraphAsync(entities, documents, cancellationToken);
                }

                // Analyze query intent
                var queryUnderstanding = AnalyzeQueryIntent(request.Query);

                // Generate follow-up suggestions
                var suggestedFollowUps = GenerateFollowUpQuestions(request.Query, documents);

                // Get case context if available
                Dictionary<string, object> caseContext = new();
                if (!string.IsNullOrEmpty(request.CaseId))
                {
                    caseContext = await GetCaseContextAsync(request.CaseId, cancellationToken);
                }

                stopwatch.Stop();

                _logger.LogInformation("Document search completed in {ElapsedMs}ms. Found {DocCount} documents",
                    stopwatch.ElapsedMilliseconds, documents.Count);

                // Return result using property initializers
                return new RAGSearchResult
                {
                    Documents = documents,
                    Entities = entities,
                    Relationships = new List<Relationship>(),
                    KnowledgeGraph = knowledgeGraph,
                    QueryUnderstanding = queryUnderstanding,
                    SuggestedFollowUps = suggestedFollowUps,
                    CaseContext = caseContext
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search documents");
                throw;
            }
        }

        // Update your supporting methods to return the correct types (Entity, KnowledgeGraph, etc.)
        private async Task<List<Entity>> ExtractEntitiesAsync(
            List<RAGDocument> documents,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            return new List<Entity>
    {
        new Entity
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Sample Entity",
            Type = EntityType.Person,
            Properties = new Dictionary<string, object> { ["confidence"] = 0.95 },
            Aliases = new List<string> { "Entity1" },
            Relationships = new List<Relationship>(),
            AssociatedCaseIds = new List<string>(),
            RiskScore = 0.5,
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-30),
            LastSeen = DateTimeOffset.UtcNow,
            Attributes = new Dictionary<string, object>()
        }
    };
        }

        private async Task<KnowledgeGraph> BuildKnowledgeGraphAsync(
            List<Entity> entities,
            List<RAGDocument> documents,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            var nodes = entities.Select(e => new GraphNode
            {
                Id = e.Id,
                Label = e.Name,
                Type = e.Type.ToString(),
                Properties = e.Properties
            }).ToList();

            var edges = new List<GraphEdge>();

            return new KnowledgeGraph
            {
                Nodes = nodes,
                Edges = edges,
                Properties = new Dictionary<string, object> { ["generated_at"] = DateTimeOffset.UtcNow }
            };
        }

        private QueryUnderstanding AnalyzeQueryIntent(string query)
        {
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return new QueryUnderstanding
            {
                KeyTerms = words.Take(3).ToList(),
                Intent = "information_retrieval",
                RequiredCapabilities = new List<string> { "text_search", "semantic_matching" },
                Complexity = words.Length > 10 ? 0.8 : 0.5
            };
        }

        private List<string> GenerateFollowUpQuestions(string query, List<RAGDocument> documents)
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