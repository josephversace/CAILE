using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Marker interface for notifications (one-to-many)
    /// </summary>
    public interface INotification
    {
    }

    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public NotificationCategory Category { get; set; } = NotificationCategory.System;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ReadAt { get; set; }
        public string? SourceId { get; set; }
        public string? SourceType { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public NotificationAction? PrimaryAction { get; set; }
        public List<NotificationAction>? SecondaryActions { get; set; }
        public string? ImageUrl { get; set; }
        public string? Topic { get; set; }
    }

    public class CreateNotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public NotificationCategory Category { get; set; } = NotificationCategory.System;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public string? SourceId { get; set; }
        public string? SourceType { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public NotificationAction? PrimaryAction { get; set; }
        public List<NotificationAction>? SecondaryActions { get; set; }
        public string? ImageUrl { get; set; }
        public string? Topic { get; set; }
    
    }

    public class NotificationFilter
    {
        public bool? IsRead { get; set; }
        public NotificationType? Type { get; set; }
        public NotificationCategory? Category { get; set; }
        public NotificationPriority? Priority { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? Topic { get; set; }
        public string? SourceType { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
    }

    public class NotificationAction
    {
        public string Label { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // "navigate", "execute", "dismiss"
        public string? Target { get; set; } // URL or command
        public Dictionary<string, object>? Parameters { get; set; }
    }


    public class NotificationReceivedEventArgs : EventArgs
    {
        public Notification Notification { get; set; } = null!;
    }

    public class NotificationReadEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
        public DateTimeOffset ReadAt { get; set; }
    }

    public class NotificationDeletedEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
    }
}
