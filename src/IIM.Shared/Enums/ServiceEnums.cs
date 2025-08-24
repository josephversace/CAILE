namespace IIM.Shared.Enums;




public enum ServiceState
{
    Unknown,
    NotFound,
    Stopped,
    Starting,
    Running,
    Stopping,
    Error,
    Degraded
}

public enum ServiceType
{
    Docker,
    Python,
    Binary,
    SystemService
}

public enum ServicePriority
{
    Critical,
    High,
    Normal,
    Low
}
