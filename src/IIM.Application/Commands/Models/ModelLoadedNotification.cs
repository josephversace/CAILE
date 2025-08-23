using IIM.Core.Mediator;
using IIM.Shared.Enums;
using IIM.Shared.Models;
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
        public ModelType Type { get; set; }
        public long MemoryUsage { get; set; }
        public long LoadTimeMs { get; set; }
        public DateTimeOffset Timestamp { get; set; }
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
    /// Notification when a model is unloaded
    /// </summary>
    public class ModelUnloadedNotification : INotification
    {
        public string ModelId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}