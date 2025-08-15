using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Core.Models;


public class Case
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CaseNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CaseType Type { get; set; }
    public CaseStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string LeadInvestigator { get; set; } = string.Empty;
    public List<string> TeamMembers { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public CasePriority Priority { get; set; } = CasePriority.Medium;


    // Relationships
    public List<Evidence> Evidence { get; set; } = new();
    public List<InvestigationSession> Sessions { get; set; } = new();
    public List<Timeline> Timelines { get; set; } = new();
    public List<Report> Reports { get; set; } = new();

    // Security
    public string Classification { get; set; } = "UNCLASSIFIED";
    public List<string> AccessControlList { get; set; } = new();
}

public enum CasePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
    Emergency = 4
}

public enum CaseType
{
    DigitalForensics,
    FinancialCrime,
    Cybercrime,
    OSINT,
    CounterIntelligence,
    MissingPerson,
    Homicide,
    Fraud,
    Narcotics,
    OrganizedCrime,
    Terrorism,
    Other
}

public enum CaseStatus
{
    Active,
    Open,
    InProgress,
    AssignedTo,
    Pending,
    UnderReview,
    Closed,
    Cold,
    Archived
}

