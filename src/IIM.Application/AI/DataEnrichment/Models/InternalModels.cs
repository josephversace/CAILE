using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;

namespace IIM.Application.AI.DataEnrichment.Models
{
    internal class QueryIntent
    {
        public string PrimaryIntent { get; set; } = string.Empty;
        public Dictionary<string, float> IntentScores { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
        public List<string> Entities { get; set; } = new();
        public QueryType QueryType { get; set; }
        public float Confidence { get; set; }
    }

    internal enum QueryType
    {
        Search,
        Analysis,
        Classification,
        Similarity,
        Metadata,
        Compliance
    }

    internal class ProcessingContext
    {
        public string RequestId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> ProcessingSteps { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, TimeSpan> StepTimings { get; set; } = new();
    }

    internal class TextAnalysisResult
    {
        public string ExtractedText { get; set; } = string.Empty;
        public List<string> KeyPhrases { get; set; } = new();
        public Dictionary<string, float> LanguageConfidence { get; set; } = new();
        public SentimentResult? Sentiment { get; set; }
        public List<EntityMatch> ExtractedEntities { get; set; } = new();
        public float ContentQuality { get; set; }
    }

    internal class SentimentResult
    {
        public string OverallSentiment { get; set; } = string.Empty;
        public float PositiveScore { get; set; }
        public float NegativeScore { get; set; }
        public float NeutralScore { get; set; }
        public float Confidence { get; set; }
    }

    internal class EntityMatch
    {
        public string Text { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }
}