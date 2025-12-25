using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{
    public class ContentAnalysis
    {
        public string FileHash { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<string> KeyPhrases { get; set; } = new();
        public Dictionary<string, float> DetectedLanguages { get; set; } = new();
        public SentimentScore? Sentiment { get; set; }  // Fixed: Use SentimentScore
        public List<DataElement> StructuredData { get; set; } = new(); // Fixed: Use List<DataElement>
        public Dictionary<string, object> ExtractedMetadata { get; set; } = new();
        public float ConfidenceScore { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    // Updated ClassificationSuggestion to include missing properties
    public class ClassificationSuggestion
    {
        public List<TagSuggestion> SuggestedTags { get; set; } = new();
        public DataSensitivityLevel SuggestedSensitivity { get; set; }
        public DataSensitivityLevel SensitivityLevel { get; set; } 
        public string StorageTier { get; set; } = string.Empty;
        public string RecommendedStorageTier { get; set; } = string.Empty; 
        public int RetentionDays { get; set; } 
        public bool RequiresHumanReview { get; set; } 
        public float ConfidenceScore { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    // Updated DataInsight to include missing properties
    public class DataInsight
    {
        public string Question { get; set; } = string.Empty;
        public string Insight { get; set; } = string.Empty;
        public List<InsightSupport> SupportingEvidence { get; set; } = new();
        public List<InsightMetric> Metrics { get; set; } = new(); 
        public List<string> Recommendations { get; set; } = new(); 
        public float Confidence { get; set; }
        public float ConfidenceScore { get; set; } // (alias for Confidence)
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    // Updated SimilarityResult to include missing properties
    public class SimilarityResult
    {
        public Guid SourceFileId { get; set; }
        public List<SimilarFile> SimilarFiles { get; set; } = new();
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public string SimilarityMethod { get; set; } = string.Empty;
        public TimeSpan SearchTime { get; set; } 
    }

    // Updated PolicySuggestion to include missing properties
    public class PolicySuggestion
    {
        public List<string> SuggestedRules { get; set; } = new();
        public List<string> SuggestedClassifications { get; set; } = new();
        public List<string> SuggestedRetentionPolicies { get; set; } = new();
        public List<SuggestedClassificationTag> SuggestedTags { get; set; } = new(); 
        public List<SuggestedStorageTier> SuggestedTiers { get; set; } = new(); 
        public string Reasoning { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public float ConfidenceScore { get; set; }  //(alias for Confidence)
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    // Updated ComplianceCheck to include missing properties
    public class ComplianceCheck
    {
        public string FileId { get; set; } = string.Empty; 
        public bool IsCompliant { get; set; }
        public List<ComplianceIssue> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<string> AppliedRules { get; set; } = new(); 
        public RiskLevel OverallRisk { get; set; } 
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public string FrameworkVersion { get; set; } = string.Empty;
    }


    public class TagSuggestion
    {
        public string Name { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class EntityExtractionResult
    {
        public List<ExtractedEntity> Entities { get; set; } = new();
        public float OverallConfidence { get; set; }
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
        public string ModelUsed { get; set; } = string.Empty;
    }

    public class ExtractedEntity
    {
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }

    public class QueryResult
    {
        public string Query { get; set; } = string.Empty;
        public List<VirtualFile> MatchingFiles { get; set; } = new();
        public int TotalResults { get; set; }
        public string GeneratedResponse { get; set; } = string.Empty;
        public List<string> SuggestedFollowups { get; set; } = new();
        public Dictionary<string, List<string>> Facets { get; set; } = new();
        public TimeSpan? QueryTime { get; set; }
        public float Confidence { get; set; }
    }

    public class InsightSupport
    {
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public float Relevance { get; set; }
    }

    public class SimilarFile
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public float SimilarityScore { get; set; }
        public string SimilarityReason { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class RiskAssessment
    {
        public Guid WorkspaceId { get; set; }
        public RiskLevel OverallRiskLevel { get; set; }
        public List<RiskFactor> IdentifiedRisks { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public int DataVolumeAnalyzed { get; set; }
        public float ConfidenceScore { get; set; }
        public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    }

    public class RiskFactor
    {
        public string RiskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RiskLevel Impact { get; set; }
        public RiskLevel Likelihood { get; set; }
        public string Mitigation { get; set; } = string.Empty;
    }

    public class ComplianceIssue
    {
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public enum RiskLevel
    {
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum DataSensitivityLevel
    {
        Unknown = -1,
        Public = 0,
        Internal = 1,
        Confidential = 2,
        Restricted = 3,
        TopSecret = 4
    }
}