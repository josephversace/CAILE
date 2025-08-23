using System;
using System.Collections.Generic;
using System.Linq;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Domain model representing a report
    /// </summary>
    public class Report
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Draft;
        public string Content { get; set; } = string.Empty;

        // Structure
        public List<ReportSection> Sections { get; set; } = new();
        public List<string> EvidenceIds { get; set; } = new();
        public List<Finding> Findings { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();

        // Metadata
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset? SubmittedAt { get; set; }
        public string? SubmittedTo { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        // Business Methods
        public void AddSection(ReportSection section)
        {
            section.Order = Sections.Count + 1;
            Sections.Add(section);
        }

        public void Submit(string submittedTo)
        {
            if (Status != ReportStatus.Approved)
                throw new InvalidOperationException("Only approved reports can be submitted");

            SubmittedAt = DateTimeOffset.UtcNow;
            SubmittedTo = submittedTo;
            Status = ReportStatus.Submitted;
        }

        public bool IsComplete()
        {
            return !string.IsNullOrWhiteSpace(Content) &&
                   Sections.Any() &&
                   Status != ReportStatus.Draft;
        }
    }

    /// <summary>
    /// Report section
    /// </summary>
    public class ReportSection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<string> EvidenceReferences { get; set; } = new();
        public List<Visualization> Visualizations { get; set; } = new();
    }

    /// <summary>
    /// Report recommendation
    /// </summary>
    public class Recommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RecommendationPriority Priority { get; set; }
        public string Rationale { get; set; } = string.Empty;
        public List<string> RelatedFindingIds { get; set; } = new();
    }

    /// <summary>
    /// Domain model representing a timeline
    /// </summary>
    public class Timeline
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CaseId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // Timeline Components
        public List<TimelineEvent> Events { get; set; } = new();
        public List<TimelinePattern> Patterns { get; set; } = new();
        public List<TimelineAnomaly> Anomalies { get; set; } = new();
        public List<CriticalPeriod> CriticalPeriods { get; set; } = new();

        // Metadata
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        // Business Methods
        public void AddEvent(TimelineEvent timelineEvent)
        {
            Events.Add(timelineEvent);
            Events = Events.OrderBy(e => e.Timestamp).ToList();
        }

        public TimelineEvent? GetEventAt(DateTimeOffset timestamp)
        {
            return Events.FirstOrDefault(e => e.Timestamp == timestamp);
        }

        public List<TimelineEvent> GetEventsInRange(DateTimeOffset start, DateTimeOffset end)
        {
            return Events.Where(e => e.Timestamp >= start && e.Timestamp <= end).ToList();
        }

        public void IdentifyPattern(TimelinePattern pattern)
        {
            Patterns.Add(pattern);
        }

        public void MarkAnomaly(TimelineAnomaly anomaly)
        {
            Anomalies.Add(anomaly);
        }
    }

    /// <summary>
    /// Timeline event
    /// </summary>
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

    /// <summary>
    /// Timeline pattern
    /// </summary>
    public class TimelinePattern
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public PatternType Type { get; set; }
        public List<string> EventIds { get; set; } = new();
        public double Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Timeline anomaly
    /// </summary>
    public class TimelineAnomaly
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset Timestamp { get; set; }
        public AnomalyType Type { get; set; }
        public double Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> AffectedEventIds { get; set; } = new();
    }

    /// <summary>
    /// Critical period in timeline
    /// </summary>
    public class CriticalPeriod
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string Description { get; set; } = string.Empty;
        public CriticalityLevel Level { get; set; }
        public List<string> EventIds { get; set; } = new();

        public TimeSpan Duration => End - Start;

        public bool Contains(DateTimeOffset timestamp)
        {
            return timestamp >= Start && timestamp <= End;
        }
    }

    /// <summary>
    /// Geographic location
    /// </summary>
    public class GeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        public double DistanceTo(GeoLocation other)
        {
            // Haversine formula for distance calculation
            var R = 6371; // Earth's radius in kilometers
            var dLat = ToRad(other.Latitude - Latitude);
            var dLon = ToRad(other.Longitude - Longitude);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(Latitude)) * Math.Cos(ToRad(other.Latitude)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRad(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}