namespace IIM.Shared.Enums;

public enum FileType
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

public enum FileProcessingStatus
{
    Pending,
    Ingested,
    Processing,
    Processed,
    Analyzed,
    Verified,
    Compromised,
    Active,   
    Failed,
    Deleted,
    Archived
}

public enum EvidenceUploadStatus
{
    Pending,
    Approved,
    Uploading,
    Processing,
    Duplicate,
    Completed,
    Failed,
    Rejected
}




public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum Classification { 

    Unclassified,
    ProtectedA,
    ProtectedB,
    ProtectedC

}