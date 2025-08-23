// ============================================
// File: src/IIM.Core/Services/INotificationService.cs
// Purpose: Core service for managing system notifications
// ============================================

using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Core.Services
{


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
        /// <summary>
        /// Sends a notification with the specified title, message, and type
        /// </summary>
        /// <param name="title">The notification title</param>
        /// <param name="message">The notification message</param>
        /// <param name="type">The type/severity of the notification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes when the notification is sent</returns>
        public async Task SendNotificationAsync(string title, string message, NotificationType type, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(title))
            {
                title = type.ToString(); // Use type as title if not provided
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(message));
            }

            // Determine notification category based on the message content or type
            var category = DetermineCategory(message, type);

            // Determine priority based on type
            var priority = DeterminePriority(type);

            // Create the notification request
            var request = new CreateNotificationRequest
            {
                Title = title,
                Message = message,
                Type = type,
                Category = category,
                Priority = priority
            };

            // If it's a critical notification, add an action
            if (type == NotificationType.Critical || type == NotificationType.Error)
            {
                request.PrimaryAction = new NotificationAction
                {
                    Label = "View Details",
                    ActionType = "navigate",
                    Target = "/notifications"
                };
            }

            // Create the notification
            var notificationId = await CreateNotificationAsync(request, cancellationToken);

            // For critical notifications, also trigger immediate UI update if possible
            if (type == NotificationType.Critical)
            {
                // Log critical notifications for audit trail
               // _logger?.LogWarning("Critical notification sent: {Title} - {Message}", title, message);

                // You could also trigger SignalR or other real-time updates here
                // Example: await _hubContext.Clients.All.SendAsync("CriticalNotification", title, message);
            }

            // For error notifications, log them
            if (type == NotificationType.Error)
            {
               // _logger?.LogError("Error notification sent: {Title} - {Message}", title, message);
            }

            // For info and success, just log at debug level
            if (type == NotificationType.Info || type == NotificationType.Success)
            {
               // _logger?.LogDebug("Notification sent: {Title} - {Message}", title, message);
            }
        }

        /// <summary>
        /// Determines the appropriate category based on message content
        /// </summary>
        private NotificationCategory DetermineCategory(string message, NotificationType type)
        {
            var lowerMessage = message.ToLowerInvariant();

            // Check for specific keywords to determine category
            if (lowerMessage.Contains("model") || lowerMessage.Contains("inference") || lowerMessage.Contains("llm"))
                return NotificationCategory.Model;

            if (lowerMessage.Contains("investigation") || lowerMessage.Contains("query") || lowerMessage.Contains("session"))
                return NotificationCategory.Investigation;

            if (lowerMessage.Contains("case") || lowerMessage.Contains("incident"))
                return NotificationCategory.Case;

            if (lowerMessage.Contains("evidence") || lowerMessage.Contains("document") || lowerMessage.Contains("file"))
                return NotificationCategory.Evidence;

            if (lowerMessage.Contains("training") || lowerMessage.Contains("fine-tun"))
                return NotificationCategory.Training;

            if (lowerMessage.Contains("export") || lowerMessage.Contains("download"))
                return NotificationCategory.Export;

            if (lowerMessage.Contains("import") || lowerMessage.Contains("upload"))
                return NotificationCategory.Import;

            if (lowerMessage.Contains("security") || lowerMessage.Contains("authentication") || lowerMessage.Contains("permission"))
                return NotificationCategory.Security;

            if (lowerMessage.Contains("update") || lowerMessage.Contains("upgrade") || lowerMessage.Contains("version"))
                return NotificationCategory.Update;

            // Default to System category
            return NotificationCategory.System;
        }

        /// <summary>
        /// Determines the priority based on notification type
        /// </summary>
        private NotificationPriority DeterminePriority(NotificationType type)
        {
            return type switch
            {
                NotificationType.Critical => NotificationPriority.Critical,
                NotificationType.Error => NotificationPriority.High,
                NotificationType.Warning => NotificationPriority.Normal,
                NotificationType.Success => NotificationPriority.Normal,
                NotificationType.Info => NotificationPriority.Low,
                _ => NotificationPriority.Normal
            };
        }
    }
}