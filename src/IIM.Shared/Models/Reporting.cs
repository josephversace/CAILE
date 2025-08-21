using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class Report
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public ReportStatus Status { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<ReportSection> Sections { get; set; } = new();
        public List<string> EvidenceIds { get; set; } = new();
        public List<Finding> Findings { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset? SubmittedAt { get; set; }
        public string? SubmittedTo { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ReportSection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<string> EvidenceReferences { get; set; } = new();
        public List<Visualization> Visualizations { get; set; } = new();
    }

    public class Finding
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FindingSeverity Severity { get; set; }
        public double Confidence { get; set; }
        public List<string> SupportingEvidenceIds { get; set; } = new();
        public List<string> RelatedEntityIds { get; set; } = new();
        public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class Recommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RecommendationPriority Priority { get; set; }
        public string Rationale { get; set; } = string.Empty;
        public List<string> RelatedFindingIds { get; set; } = new();
    }

    #region Timeline


    public class Timeline
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CaseId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<TimelineEvent> Events { get; set; } = new();
        public List<TimelinePattern> Patterns { get; set; } = new();
        public List<TimelineAnomaly> Anomalies { get; set; } = new();
        public List<CriticalPeriod> CriticalPeriods { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class TimelineEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public EventType Type { get; set; }
        public EventImportance Importance { get; set; }
        public string? EvidenceId { get; set; }
        public List<string> RelatedEntityIds { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public GeoLocation? Location { get; set; }
    }

    public class TimelinePattern
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public PatternType Type { get; set; }
        public List<string> EventIds { get; set; } = new();
        public double Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class TimelineAnomaly
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset Timestamp { get; set; }
        public AnomalyType Type { get; set; }
        public double Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> AffectedEventIds { get; set; } = new();
    }

    public class CriticalPeriod
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string Description { get; set; } = string.Empty;
        public CriticalityLevel Level { get; set; }
        public List<string> EventIds { get; set; } = new();
    }


    #endregion
}
