using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Application.AI.DataEnrichment.Helpers
{
    /// <summary>
    /// Helper class for calculating confidence scores for various analysis results
    /// </summary>
    public class ConfidenceCalculator
    {
        private readonly ILogger<ConfidenceCalculator> _logger;

        public ConfidenceCalculator(ILogger<ConfidenceCalculator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Calculates overall confidence for content analysis
        /// </summary>
        public float CalculateAnalysisConfidence(ContentAnalysis analysis)
        {
            var factors = new List<float>();

            // Text extraction quality
            if (!string.IsNullOrEmpty(analysis.Summary))
            {
                factors.Add(0.9f); // High confidence if we got meaningful text
            }
            else
            {
                factors.Add(0.3f); // Low confidence without text content
            }

            // Metadata extraction success
            if (analysis.ExtractedMetadata?.Any() == true)
            {
                factors.Add(0.8f);
            }

            // Key phrases quality
            if (analysis.KeyPhrases?.Any() == true)
            {
                var phraseQuality = Math.Min(analysis.KeyPhrases.Count / 10.0f, 1.0f);
                factors.Add(phraseQuality);
            }

            // Language detection confidence
            if (analysis.DetectedLanguages?.Any() == true)
            {
                var maxLangConfidence = analysis.DetectedLanguages.Values.Max();
                factors.Add(maxLangConfidence);
            }

            return factors.Any() ? factors.Average() : 0.0f;
        }

        /// <summary>
        /// Calculates confidence for classification suggestions
        /// </summary>
        public float CalculateClassificationConfidence(ClassificationSuggestion suggestion)
        {
            if (!suggestion.SuggestedTags.Any())
                return 0.0f;

            var avgTagConfidence = suggestion.SuggestedTags.Average(t => t.Confidence);
            var tagCountFactor = Math.Min(suggestion.SuggestedTags.Count / 3.0f, 1.0f);

            return (avgTagConfidence + tagCountFactor) / 2.0f;
        }

        /// <summary>
        /// Calculates confidence for query results
        /// </summary>
        public float CalculateQueryConfidence(QueryResult result)
        {
            var factors = new List<float>();

            // Result count factor
            if (result.TotalResults > 0)
            {
                var resultCountFactor = Math.Min(result.TotalResults / 10.0f, 1.0f);
                factors.Add(resultCountFactor);
            }

            // Response quality
            if (!string.IsNullOrEmpty(result.GeneratedResponse))
            {
                var responseLength = result.GeneratedResponse.Length;
                var lengthFactor = Math.Min(responseLength / 500.0f, 1.0f);
                factors.Add(lengthFactor);
            }

            // Query time factor (faster = more confident)
            if (result.QueryTime.HasValue)
            {
                var timeFactor = result.QueryTime.Value.TotalSeconds < 5 ? 0.9f : 0.7f;
                factors.Add(timeFactor);
            }

            return factors.Any() ? factors.Average() : 0.5f;
        }

        /// <summary>
        /// Calculates confidence for risk assessment
        /// </summary>
        public float CalculateRiskConfidence(RiskAssessment assessment)
        {
            var factors = new List<float>();

            // Data completeness
            if (assessment.DataVolumeAnalyzed > 0)
            {
                factors.Add(0.8f);
            }

            // Risk factor identification
            if (assessment.IdentifiedRisks?.Any() == true)
            {
                var riskFactor = Math.Min(assessment.IdentifiedRisks.Count / 5.0f, 1.0f);
                factors.Add(riskFactor);
            }

            // Recommendations quality
            if (assessment.Recommendations?.Any() == true)
            {
                factors.Add(0.7f);
            }

            return factors.Any() ? factors.Average() : 0.0f;
        }

        /// <summary>
        /// Calculates confidence for policy suggestions
        /// </summary>
        public float CalculatePolicySuggestionConfidence(PolicySuggestion suggestion)
        {
            var factors = new List<float>();

            // Rule count factor
            if (suggestion.SuggestedRules.Any())
            {
                var ruleCountFactor = Math.Min(suggestion.SuggestedRules.Count / 5.0f, 1.0f);
                factors.Add(ruleCountFactor);
            }

            // Classification count factor
            if (suggestion.SuggestedClassifications.Any())
            {
                var classificationFactor = Math.Min(suggestion.SuggestedClassifications.Count / 3.0f, 1.0f);
                factors.Add(classificationFactor);
            }

            // Reasoning quality
            if (!string.IsNullOrEmpty(suggestion.Reasoning))
            {
                var reasoningFactor = Math.Min(suggestion.Reasoning.Length / 200.0f, 1.0f);
                factors.Add(reasoningFactor);
            }

            return factors.Any() ? factors.Average() : 0.5f;
        }
    }
}