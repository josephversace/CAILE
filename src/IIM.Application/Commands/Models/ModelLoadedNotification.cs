using IIM.Core.Mediator;
using IIM.Shared.Enums;
using IIM.Shared.DTOs;
using System;

namespace IIM.Application.Commands.Models
{
    /// <summary>
    /// Notification when a model is successfully loaded
    /// </summary>
    public class ModelLoadedNotification : INotification
    {
        public string ModelId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public ModelType ModelType { get; set; }
        public long MemoryUsage { get; set; }
        public long LoadTimeMs { get; set; }
        public DateTimeOffset Timestamp { get; set; }

     
        public string SessionId { get; set; } = string.Empty;
  
        public DateTimeOffset LoadedAt { get; set; }
    
    }

    /// <summary>
    /// Notification when a model fails to load
    /// </summary>
    public class ModelLoadFailedNotification : INotification
    {
        public string ModelId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }




    /// <summary>
    /// Notification published when model loading starts.
    /// Used for UI progress indicators and audit logging.
    /// </summary>
    public class ModelLoadingStartedNotification : INotification
    {
        public string ModelId { get; set; } = string.Empty;
        public ModelType ModelType { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification published during model loading to report progress.
    /// Used for UI progress bars and monitoring.
    /// </summary>
    public class ModelLoadingProgressNotification : INotification
    {
        public string ModelId { get; set; } = string.Empty;
        public float Progress { get; set; } // 0.0 to 1.0
        public DateTimeOffset Timestamp { get; set; }
    }

 

    /// <summary>
    /// Notification published when model loading fails.
    /// Used for error tracking and user notification.
    /// </summary>
    public class ModelLoadingFailedNotification : INotification
    {
        public string ModelId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification published when resource thresholds are exceeded.
    /// Used for system monitoring and alerting.
    /// </summary>
    public class ResourceThresholdExceededNotification : INotification
    {
        public string ResourceType { get; set; } = string.Empty; // "Memory", "GPU", etc.
        public long CurrentValue { get; set; }
        public long ThresholdValue { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}