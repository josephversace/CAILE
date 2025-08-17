namespace IIM.Shared.Enums;

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

public enum CasePriority
{
    Low,
    Medium,
    High,
    Critical,
    Emergency
}

public enum CaseType
{
    Investigation,
    Intelligence,
    Surveillance,
    Forensics,
    CyberCrime,
    FinancialCrime,
    CounterIntelligence,
    MissingPerson,
    Homicide,
    Fraud,
    Narcotics,
    OrganizedCrime,
    Terrorism,
    Other
}
