using System;
using System.Collections.Generic;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Notification entity
    /// </summary>
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; }
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
        public bool IsRead { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ReadAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? Category { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public List<NotificationAction>? Actions { get; set; }

        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTimeOffset.UtcNow;
            Status = NotificationStatus.Read;
        }

        public bool IsExpired()
        {
            return ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Notification action that can be taken
    /// </summary>
    public class NotificationAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Label { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }

    /// <summary>
    /// Request to create a notification
    /// </summary>
    public class CreateNotificationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public string? Category { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public TimeSpan? ExpiresIn { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public List<NotificationAction>? Actions { get; set; }
    }

    /// <summary>
    /// Filter for querying notifications
    /// </summary>
    public class NotificationFilter
    {
        public string? UserId { get; set; }
        public NotificationType? Type { get; set; }
        public NotificationPriority? Priority { get; set; }
        public NotificationStatus? Status { get; set; }
        public bool? IsRead { get; set; }
        public string? Category { get; set; }
        public DateTimeOffset? CreatedAfter { get; set; }
        public DateTimeOffset? CreatedBefore { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Event args for notification received
    /// </summary>
    public class NotificationReceivedEventArgs : EventArgs
    {
        public Notification Notification { get; set; } = new();
        public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Event args for notification read
    /// </summary>
    public class NotificationReadEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTimeOffset ReadAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Event args for notification deleted
    /// </summary>
    public class NotificationDeletedEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTimeOffset DeletedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? Reason { get; set; }
    }
}