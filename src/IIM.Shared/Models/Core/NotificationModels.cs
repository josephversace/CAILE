using System;

namespace IIM.Shared.Models.Core
{
    public class Notification
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? UserId { get; set; }
    }

    public class UserNotification
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Guid NotificationId { get; set; }
        public bool IsRead { get; set; }
    }
}
