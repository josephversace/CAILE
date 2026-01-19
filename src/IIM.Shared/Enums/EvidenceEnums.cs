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

public enum FileUploadStatus
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
    Skipped,
    Pending,
    Running,
    Completed,
    Failed
}

public enum Classification { 

    Unclassified,
    ProtectedA,
    ProtectedB,
    ProtectedC

}