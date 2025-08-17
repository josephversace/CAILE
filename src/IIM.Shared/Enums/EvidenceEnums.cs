namespace IIM.Shared.Enums;

public enum EvidenceType
{
    Document,
    Image,
    Video,
    Audio,
    Email,
    Database,
    DiskImage,
    MemoryDump,
    NetworkCapture,
    LogFile,
    Archive,
    Other
}

public enum EvidenceStatus
{
    Pending,
    Ingested,
    Processing,
    Analyzed,
    Verified,
    Compromised,
    Archived
}
