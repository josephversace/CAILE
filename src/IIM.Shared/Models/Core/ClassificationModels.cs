using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class ClassificationResult
    {
        public List<string> AppliedTags { get; set; } = new();
        public DataSensitivityLevel SensitivityLevel { get; set; }
        public string RecommendedStorageTier { get; set; } = string.Empty;
        public float ConfidenceScore { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public bool RequiresHumanReview { get; set; }
        public Dictionary<string, object> AdditionalMetadata { get; set; } = new();
    }

    public class ClassificationFeedback
    {
        public string FileHash { get; set; } = string.Empty;
        public List<string> CorrectTags { get; set; } = new();
        public List<string> IncorrectTags { get; set; } = new();
        public string UserFeedback { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime FeedbackAt { get; set; } = DateTime.UtcNow;
    }

    // Missing: BulkClassificationResponse
    public class BulkClassificationResponse
    {
        public int TotalFiles { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<ClassificationResult> Results { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }



    // Missing: ClassificationUpdate
    public class ClassificationUpdate
    {
        public string FileId { get; set; }
		public string ClassificationLevel { get; set; } = string.Empty;
		public List<string> Tags { get; set; }
        public string Description { get; set; }
        public string UpdatedBy { get; set; }
        public string UpdateReason { get; set; }
    }
}
