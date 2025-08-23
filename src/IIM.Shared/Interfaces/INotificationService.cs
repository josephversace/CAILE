using IIM.Shared.Enums;
using IIM.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
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

        Task SendNotificationAsync(string title, string message, NotificationType type, CancellationToken cancellationToken = default);


        // Events
        event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;
        event EventHandler<NotificationReadEventArgs>? NotificationRead;
        event EventHandler<NotificationDeletedEventArgs>? NotificationDeleted;
    }
}
