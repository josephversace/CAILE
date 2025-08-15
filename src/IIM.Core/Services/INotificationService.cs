// ============================================
// File: src/IIM.Core/Services/INotificationService.cs
// Purpose: Core service for managing system notifications
// ============================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.Services
{
    /// <summary>
    /// Service for managing system notifications across the IIM platform.
    /// Handles investigation alerts, model status updates, and system events.
    /// </summary>
    public interface INotificationService
    {
        // Notification Management
        Task<string> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
        Task<Notification?> GetNotificationAsync(string notificationId, CancellationToken cancellationToken = default);
        Task<List<Notification>> GetNotificationsAsync(NotificationFilter? filter = null, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);
        Task<bool> MarkAllAsReadAsync(CancellationToken cancellationToken = default);
        Task<bool> DeleteNotificationAsync(string notificationId, CancellationToken cancellationToken = default);
        Task<bool> ClearAllNotificationsAsync(CancellationToken cancellationToken = default);

        // Real-time Notifications
        Task ShowNotificationAsync(string message, NotificationType type = NotificationType.Info, int durationMs = 5000);
        Task ShowToastAsync(string title, string message, NotificationType type = NotificationType.Info);

        // Subscription Management
        Task SubscribeToTopicAsync(string topic, CancellationToken cancellationToken = default);
        Task UnsubscribeFromTopicAsync(string topic, CancellationToken cancellationToken = default);
        Task<List<string>> GetSubscribedTopicsAsync(CancellationToken cancellationToken = default);

        // Events
        event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;
        event EventHandler<NotificationReadEventArgs>? NotificationRead;
        event EventHandler<NotificationDeletedEventArgs>? NotificationDeleted;
    }

    // DTOs
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

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Critical
    }

    public enum NotificationCategory
    {
        System,
        Investigation,
        Case,
        Evidence,
        Model,
        Training,
        Export,
        Import,
        Security,
        Update
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Urgent,
        Critical
    }

    // Event Args
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

    // Concrete Implementation
    public class NotificationService : INotificationService
    {
        private readonly List<Notification> _notifications = new();
        private readonly HashSet<string> _subscribedTopics = new();
        private readonly object _lock = new();

        public event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;
        public event EventHandler<NotificationReadEventArgs>? NotificationRead;
        public event EventHandler<NotificationDeletedEventArgs>? NotificationDeleted;

        public NotificationService()
        {
            // Initialize with some sample notifications for development
            InitializeSampleNotifications();
        }

        public Task<string> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Category = request.Category,
                Priority = request.Priority,
                SourceId = request.SourceId,
                SourceType = request.SourceType,
                Metadata = request.Metadata,
                PrimaryAction = request.PrimaryAction,
                SecondaryActions = request.SecondaryActions,
                ImageUrl = request.ImageUrl,
                Topic = request.Topic
            };

            lock (_lock)
            {
                _notifications.Insert(0, notification); // Add to beginning for most recent first
            }

            NotificationReceived?.Invoke(this, new NotificationReceivedEventArgs { Notification = notification });

            return Task.FromResult(notification.Id);
        }

        public Task<Notification?> GetNotificationAsync(string notificationId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
                return Task.FromResult(notification);
            }
        }

        public Task<List<Notification>> GetNotificationsAsync(NotificationFilter? filter = null, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                IEnumerable<Notification> query = _notifications;

                if (filter != null)
                {
                    if (filter.IsRead.HasValue)
                        query = query.Where(n => n.IsRead == filter.IsRead.Value);

                    if (filter.Type.HasValue)
                        query = query.Where(n => n.Type == filter.Type.Value);

                    if (filter.Category.HasValue)
                        query = query.Where(n => n.Category == filter.Category.Value);

                    if (filter.Priority.HasValue)
                        query = query.Where(n => n.Priority == filter.Priority.Value);

                    if (filter.StartDate.HasValue)
                        query = query.Where(n => n.CreatedAt >= filter.StartDate.Value);

                    if (filter.EndDate.HasValue)
                        query = query.Where(n => n.CreatedAt <= filter.EndDate.Value);

                    if (!string.IsNullOrEmpty(filter.Topic))
                        query = query.Where(n => n.Topic == filter.Topic);

                    if (!string.IsNullOrEmpty(filter.SourceType))
                        query = query.Where(n => n.SourceType == filter.SourceType);

                    if (filter.Offset.HasValue)
                        query = query.Skip(filter.Offset.Value);

                    if (filter.Limit.HasValue)
                        query = query.Take(filter.Limit.Value);
                }

                return Task.FromResult(query.ToList());
            }
        }

        public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var count = _notifications.Count(n => !n.IsRead);
                return Task.FromResult(count);
            }
        }

        public Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null && !notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTimeOffset.UtcNow;

                    NotificationRead?.Invoke(this, new NotificationReadEventArgs
                    {
                        NotificationId = notificationId,
                        ReadAt = notification.ReadAt.Value
                    });

                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<bool> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;
                var unreadNotifications = _notifications.Where(n => !n.IsRead).ToList();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = now;

                    NotificationRead?.Invoke(this, new NotificationReadEventArgs
                    {
                        NotificationId = notification.Id,
                        ReadAt = now
                    });
                }

                return Task.FromResult(unreadNotifications.Any());
            }
        }

        public Task<bool> DeleteNotificationAsync(string notificationId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var removed = _notifications.RemoveAll(n => n.Id == notificationId) > 0;

                if (removed)
                {
                    NotificationDeleted?.Invoke(this, new NotificationDeletedEventArgs { NotificationId = notificationId });
                }

                return Task.FromResult(removed);
            }
        }

        public Task<bool> ClearAllNotificationsAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var hadNotifications = _notifications.Any();
                _notifications.Clear();
                return Task.FromResult(hadNotifications);
            }
        }

        public Task ShowNotificationAsync(string message, NotificationType type = NotificationType.Info, int durationMs = 5000)
        {
            // This would typically trigger a UI notification
            // For now, we'll create a notification entry
            var request = new CreateNotificationRequest
            {
                Title = type.ToString(),
                Message = message,
                Type = type,
                Category = NotificationCategory.System
            };

            return CreateNotificationAsync(request);
        }

        public Task ShowToastAsync(string title, string message, NotificationType type = NotificationType.Info)
        {
            // This would typically trigger a toast notification in the UI
            var request = new CreateNotificationRequest
            {
                Title = title,
                Message = message,
                Type = type,
                Category = NotificationCategory.System,
                Priority = type == NotificationType.Error || type == NotificationType.Critical
                    ? NotificationPriority.High
                    : NotificationPriority.Normal
            };

            return CreateNotificationAsync(request);
        }

        public Task SubscribeToTopicAsync(string topic, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _subscribedTopics.Add(topic);
            }
            return Task.CompletedTask;
        }

        public Task UnsubscribeFromTopicAsync(string topic, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _subscribedTopics.Remove(topic);
            }
            return Task.CompletedTask;
        }

        public Task<List<string>> GetSubscribedTopicsAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_subscribedTopics.ToList());
            }
        }

        private void InitializeSampleNotifications()
        {
            // Add some sample notifications for development
            _notifications.AddRange(new[]
            {
                new Notification
                {
                    Title = "Model Training Complete",
                    Message = "Fine-tuning of llama3.1:70b completed successfully",
                    Type = NotificationType.Success,
                    Category = NotificationCategory.Training,
                    Priority = NotificationPriority.Normal,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                    PrimaryAction = new NotificationAction
                    {
                        Label = "View Results",
                        ActionType = "navigate",
                        Target = "/models/training/results"
                    }
                },
                new Notification
                {
                    Title = "New Evidence Added",
                    Message = "15 new documents added to Case #2024-CF-1234",
                    Type = NotificationType.Info,
                    Category = NotificationCategory.Evidence,
                    Priority = NotificationPriority.Normal,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
                    SourceId = "case-001",
                    SourceType = "Case"
                },
                new Notification
                {
                    Title = "GPU Memory Warning",
                    Message = "GPU memory usage exceeded 90% threshold",
                    Type = NotificationType.Warning,
                    Category = NotificationCategory.System,
                    Priority = NotificationPriority.High,
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
                }
            });
        }
    }
}