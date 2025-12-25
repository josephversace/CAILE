using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class ContentAnalysisResult
    {
        public string FileHash { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public List<DetectedEntity> Entities { get; set; } = new();
        public List<string> KeyPhrases { get; set; } = new();
        public SentimentAnalysis? Sentiment { get; set; }
        public List<ContentPattern> DetectedPatterns { get; set; } = new();
        public Dictionary<string, object> TechnicalMetadata { get; set; } = new();
        public float OverallConfidence { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public List<string> WarningsOrErrors { get; set; } = new();
    }

    public class AnalysisOptions
    {
        public bool ExtractText { get; set; } = true;
        public bool DetectEntities { get; set; } = true;
        public bool AnalyzeSentiment { get; set; } = false;
        public bool ExtractKeyPhrases { get; set; } = true;
        public bool DetectPatterns { get; set; } = true;
        public List<string> CustomPatterns { get; set; } = new();
        public Dictionary<string, object> AdditionalOptions { get; set; } = new();
    }

    public class AnalysisCapabilities
    {
        public List<string> SupportedMimeTypes { get; set; } = new();
        public List<string> AvailableModels { get; set; } = new();
        public List<string> SupportedLanguages { get; set; } = new();
        public long MaxFileSizeBytes { get; set; }
        public Dictionary<string, object> ModelCapabilities { get; set; } = new();
    }

    public class AnalysisConfiguration
    {
        public string PreferredModel { get; set; } = string.Empty;
        public Dictionary<string, float> ConfidenceThresholds { get; set; } = new();
        public List<CustomPattern> CustomPatterns { get; set; } = new();
        public Dictionary<string, object> ModelParameters { get; set; } = new();
    }

    public class DetectedEntity
    {
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public int StartPosition { get; set; }
        public int Length { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class ContentPattern
    {
        public string PatternType { get; set; } = string.Empty;
        public string PatternValue { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public int Occurrences { get; set; }
        public List<PatternMatch> Matches { get; set; } = new();
    }

    public class PatternMatch
    {
        public string MatchedText { get; set; } = string.Empty;
        public int Position { get; set; }
        public int Length { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
    }

    public class CustomPattern
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string PatternType { get; set; } = "Regex";
        public float MinConfidence { get; set; } = 0.8f;
    }

    public class SentimentAnalysis
    {
        public float Positive { get; set; }
        public float Negative { get; set; }
        public float Neutral { get; set; }
        public string OverallSentiment { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }
    public class SentimentScore
    {
        public string OverallSentiment { get; set; } = string.Empty;
        public float PositiveScore { get; set; }
        public float NegativeScore { get; set; }
        public float NeutralScore { get; set; }
        public float Confidence { get; set; }
    }

    // Fix for DataElement (was missing)
    public class DataElement
    {
        public string Name { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public string DataType { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    // Fix for DataFacet (was missing)
    public class DataFacet
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, int> Values { get; set; } = new();
        public int TotalCount { get; set; }
    }

    // Fix for InsightMetric (was missing)
    public class InsightMetric
    {
        public string Name { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public string Unit { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }

    // Fix for SuggestedClassificationTag (was missing)
    public class SuggestedClassificationTag
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;

        public int FileCount { get; set; }
    }

    // Fix for SuggestedStorageTier (was missing)
    public class SuggestedStorageTier
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RetentionDays { get; set; }
        public float Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public string Criteria { get; set; } = string.Empty; 
        public int EstimatedFileCount { get; set; } 
    }

    // Fix for SuggestedDataHandlingRule (was missing)
    public class SuggestedDataHandlingRule
    {
        public string RuleType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public float Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
    }



}
