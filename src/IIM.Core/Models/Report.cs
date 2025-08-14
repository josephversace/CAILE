
using System;
using System.Collections.Generic;

namespace IIM.Core.Models;

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

public enum ReportType
{
    Preliminary,
    Progress,
    Final,
    Executive,
    Technical,
    Forensic,
    Intelligence,
    Incident,
    Custom
}

public enum ReportStatus
{
    Draft,
    Review,
    Approved,
    Submitted,
    Archived
}

public enum FindingSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Urgent
}
