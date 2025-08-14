using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Core.Models;


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

public enum EventType
{
    Communication,
    Transaction,
    Movement,
    Access,
    Modification,
    Creation,
    Deletion,
    Meeting,
    Observation,
    Other
}

public enum EventImportance
{
    Low,
    Medium,
    High,
    Critical
}

public enum PatternType
{
    Temporal,
    Behavioral,
    Transactional,
    Communication,
    Geographic
}

public enum AnomalyType
{
    TimeGap,
    UnusualActivity,
    PatternBreak,
    Outlier,
    Suspicious
}

public enum CriticalityLevel
{
    Low,
    Medium,
    High,
    Critical
}
