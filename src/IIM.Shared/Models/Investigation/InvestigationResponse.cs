using System;
using System.Collections.Generic;
using System.Linq;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Domain model representing a response to an investigation query
    /// </summary>
    public class InvestigationResponse
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string QueryId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;

        // Response Content
        public string Content { get; set; } = string.Empty;
        public ResponseDisplayType DisplayType { get; set; } = ResponseDisplayType.Auto;
        public double Confidence { get; set; } = 0.0;

        // Results and Evidence
        public List<ToolResult> ToolResults { get; set; } = new();
        public List<Citation> Citations { get; set; } = new();
        public List<Evidence> RelatedEvidence { get; set; } = new();
        public List<Visualization> Visualizations { get; set; } = new();

        // Analysis Results
        public RAGSearchResult? RAGResults { get; set; }
        public List<TranscriptionResult> Transcriptions { get; set; } = new();
        public List<ImageAnalysisResult> ImageAnalyses { get; set; } = new();

        // Timestamps
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public TimeSpan ProcessingTime { get; set; }

        // Metadata
        public Dictionary<string, object> Metadata { get; set; } = new();

        // Business Methods

        /// <summary>
        /// Adds a tool result to the response
        /// </summary>
        public void AddToolResult(ToolResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            ToolResults.Add(result);

            // Update confidence based on tool results
            RecalculateConfidence();
        }

        /// <summary>
        /// Adds a citation to the response
        /// </summary>
        public void AddCitation(Citation citation)
        {
            if (citation == null)
                throw new ArgumentNullException(nameof(citation));

            Citations.Add(citation);
        }

        /// <summary>
        /// Links evidence to the response
        /// </summary>
        public void LinkEvidence(Evidence evidence)
        {
            if (evidence == null)
                throw new ArgumentNullException(nameof(evidence));

            if (!RelatedEvidence.Any(e => e.Id == evidence.Id))
                RelatedEvidence.Add(evidence);
        }

        /// <summary>
        /// Adds a visualization to the response
        /// </summary>
        public void AddVisualization(Visualization visualization)
        {
            if (visualization == null)
                throw new ArgumentNullException(nameof(visualization));

            Visualizations.Add(visualization);
        }

        /// <summary>
        /// Sets the RAG search results
        /// </summary>
        public void SetRAGResults(RAGSearchResult ragResults)
        {
            RAGResults = ragResults;

            // Add citations from RAG results
            if (ragResults?.Documents != null)
            {
                foreach (var doc in ragResults.Documents)
                {
                    AddCitation(new Citation
                    {
                        SourceId = doc.Id,
                        SourceType = "RAG",
                        Text = doc.Content,
                        Relevance = doc.Relevance
                    });
                }
            }

            RecalculateConfidence();
        }

        /// <summary>
        /// Adds a transcription result
        /// </summary>
        public void AddTranscription(TranscriptionResult transcription)
        {
            if (transcription == null)
                throw new ArgumentNullException(nameof(transcription));

            Transcriptions.Add(transcription);
        }

        /// <summary>
        /// Adds an image analysis result
        /// </summary>
        public void AddImageAnalysis(ImageAnalysisResult analysis)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));

            ImageAnalyses.Add(analysis);
        }

        /// <summary>
        /// Recalculates the confidence score based on results
        /// </summary>
        private void RecalculateConfidence()
        {
            var confidenceScores = new List<double>();

            // Add tool result confidences
            if (ToolResults.Any())
            {
                var toolConfidence = ToolResults
                    .Where(r => r.Confidence.HasValue)
                    .Select(r => r.Confidence!.Value)
                    .DefaultIfEmpty(0.5)
                    .Average();
                confidenceScores.Add(toolConfidence);
            }

            // Add citation relevance as confidence
            if (Citations.Any())
            {
                var citationConfidence = Citations
                    .Select(c => c.Relevance)
                    .DefaultIfEmpty(0.5)
                    .Average();
                confidenceScores.Add(citationConfidence);
            }

            // Add RAG confidence
            if (RAGResults != null)
            {
                confidenceScores.Add(RAGResults.TotalRelevance);
            }

            // Calculate overall confidence
            Confidence = confidenceScores.Any()
                ? confidenceScores.Average()
                : 0.5;
        }

        /// <summary>
        /// Determines if the response has high confidence
        /// </summary>
        public bool IsHighConfidence()
        {
            return Confidence >= 0.8;
        }

        /// <summary>
        /// Determines if the response needs review
        /// </summary>
        public bool NeedsReview()
        {
            return Confidence < 0.6 ||
                   ToolResults.Any(r => !r.Success) ||
                   !Citations.Any();
        }

        /// <summary>
        /// Gets a summary of the response
        /// </summary>
        public ResponseSummary GetSummary()
        {
            return new ResponseSummary
            {
                ResponseId = Id,
                ContentPreview = Content.Length > 200 ? Content.Substring(0, 200) + "..." : Content,
                ToolResultsCount = ToolResults.Count,
                CitationsCount = Citations.Count,
                EvidenceCount = RelatedEvidence.Count,
                VisualizationsCount = Visualizations.Count,
                Confidence = Confidence,
                ProcessingTime = ProcessingTime,
                HasErrors = ToolResults.Any(r => !r.Success)
            };
        }

        /// <summary>
        /// Validates the response completeness
        /// </summary>
        public bool IsComplete()
        {
            return !string.IsNullOrWhiteSpace(Content) &&
                   (ToolResults.All(r => r.Status == "Completed") || !ToolResults.Any());
        }
    }

    /// <summary>
    /// Response summary for quick overview
    /// </summary>
    public class ResponseSummary
    {
        public string ResponseId { get; set; } = string.Empty;
        public string ContentPreview { get; set; } = string.Empty;
        public int ToolResultsCount { get; set; }
        public int CitationsCount { get; set; }
        public int EvidenceCount { get; set; }
        public int VisualizationsCount { get; set; }
        public double Confidence { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public bool HasErrors { get; set; }
    }
}