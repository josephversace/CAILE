using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models.Core
{
    public class RoutingDecision
    {
        public string StorageTier { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public bool EnableDeduplication { get; set; } = true;
        public bool RequiresApproval { get; set; } = false;
        public bool RequiresEncryption { get; set; } = false;
        public int RetentionDays { get; set; } = 365;
        public List<string> ApplicableRules { get; set; } = new();
        public string Reasoning { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string TargetBucket { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public bool RequiresQuarantine { get; set; }
        public bool AllowDeduplication { get; set; }
        public List<string> AppliedPolicies { get; set; } = new();
        public float ConfidenceScore { get; set; }
        public DateTime DecisionTime { get; set; } = DateTime.UtcNow;

    }

    public class ComplianceValidationResult
    {
        public bool IsCompliant { get; set; } = true;
        public List<ComplianceViolation> Violations { get; set; } = new();
        public List<string> AppliedRules { get; set; } = new();
        public string OverallRiskLevel { get; set; } = "Low";
        public List<string> Recommendations { get; set; } = new();
    }

    public class ComplianceViolation
    {
        public string RuleName { get; set; } = string.Empty;
        public string ViolationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string RecommendedAction { get; set; } = string.Empty;
    }
}
