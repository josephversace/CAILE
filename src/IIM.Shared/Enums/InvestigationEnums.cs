namespace IIM.Shared.Enums;

public enum InvestigationType
{
    GeneralInquiry,
    EvidenceAnalysis,
    OSINTResearch,
    ForensicAnalysis,
    Cybercrime,
    Financial,
    ThreatAssessment,
    IncidentResponse,
    TimelineConstruction,
    NetworkAnalysis,
    PatternRecognition
}

public enum InvestigationStatus
{
    Active,
    Paused,
    Completed,
    Archived
}

public enum MessageRole
{
    User,
    Assistant,
    System,
    Tool
}

public enum AttachmentType
{
    Image,
    Document,
    Audio,
    Video,
    Data,
    Archive,
    Other
}

public enum FindingType
{
    Evidence,
    Connection,
    Timeline,
    Pattern,
    Anomaly,
    Identification,
    Location,
    Technical,
    Witness,
    Contradiction
}

public enum FindingSeverity
{
    Low,
    Medium,
    High,
    Critical
}
