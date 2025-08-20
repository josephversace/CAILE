using IIM.Core.Inference;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Services; // ADD for IAuditLogger
using IIM.Shared.Enums; // ADD for NotificationType

namespace IIM.Application.Handlers
{
    /// <summary>
    /// Handles inference queued notifications for UI updates
    /// </summary>
    public class InferenceQueuedHandler : INotificationHandler<InferenceQueuedNotification>
    {
        private readonly ILogger<InferenceQueuedHandler> _logger;
        private readonly INotificationService _notificationService;

        public InferenceQueuedHandler(
            ILogger<InferenceQueuedHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task Handle(InferenceQueuedNotification notification, CancellationToken ct)
        {
            _logger.LogInformation("Inference request {RequestId} queued with {Priority} priority",
                notification.RequestId, notification.Priority);

            // Send UI notification if queue is getting full
            if (notification.QueueDepth > 50)
            {
                await _notificationService.SendNotificationAsync(
                    "High Queue Depth",
                    $"Inference queue depth is {notification.QueueDepth}. Consider scaling resources.",
                    NotificationType.Warning,
                    ct);
            }
        }
    }

    /// <summary>
    /// Handles inference started notifications for progress tracking
    /// </summary>
    public class InferenceStartedHandler : INotificationHandler<InferenceStartedNotification>
    {
        private readonly ILogger<InferenceStartedHandler> _logger;
        private readonly IProgressTracker _progressTracker;

        public InferenceStartedHandler(
            ILogger<InferenceStartedHandler> logger,
            IProgressTracker progressTracker)
        {
            _logger = logger;
            _progressTracker = progressTracker;
        }

        public Task Handle(InferenceStartedNotification notification, CancellationToken ct)
        {
            _logger.LogDebug("Inference started for request {RequestId} after {QueueTime}ms in queue",
                notification.RequestId, notification.QueueTimeMs);

            // Update progress tracker
            _progressTracker.UpdateProgress(notification.RequestId, new InferenceProgressUpdate
            {
                Status = "Processing",
                Message = $"Processing with model {notification.ModelId}",
                PercentComplete = 50,
                QueueTimeMs = notification.QueueTimeMs
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles inference completion for result caching and metrics
    /// </summary>
    public class InferenceCompletedHandler : INotificationHandler<InferenceCompletedNotification>
    {
        private readonly ILogger<InferenceCompletedHandler> _logger;
        private readonly IMetricsCollector _metrics;
        private readonly IProgressTracker _progressTracker;

        public InferenceCompletedHandler(
            ILogger<InferenceCompletedHandler> logger,
            IMetricsCollector metrics,
            IProgressTracker progressTracker)
        {
            _logger = logger;
            _metrics = metrics;
            _progressTracker = progressTracker;
        }

        public Task Handle(InferenceCompletedNotification notification, CancellationToken ct)
        {
            _logger.LogInformation("Inference completed for {RequestId}: Queue={QueueMs}ms, Inference={InferenceMs}ms, Tokens={Tokens}",
                notification.RequestId, notification.QueueTimeMs, notification.InferenceTimeMs, notification.TokensGenerated);

            // Record metrics
            _metrics.RecordInferenceMetrics(new InferenceMetrics
            {
                ModelId = notification.ModelId,
                QueueTimeMs = notification.QueueTimeMs,
                InferenceTimeMs = notification.InferenceTimeMs,
                TotalTimeMs = notification.QueueTimeMs + notification.InferenceTimeMs,
                TokensGenerated = notification.TokensGenerated,
                TokensPerSecond = notification.TokensGenerated / (notification.InferenceTimeMs / 1000.0)
            });

            // Update progress
            _progressTracker.UpdateProgress(notification.RequestId, new InferenceProgressUpdate
            {
                Status = "Completed",
                Message = $"Generated {notification.TokensGenerated} tokens",
                PercentComplete = 100,
                QueueTimeMs = notification.QueueTimeMs,
                ProcessingTimeMs = notification.InferenceTimeMs
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles inference failures for error tracking and recovery
    /// </summary>
    public class InferenceFailedHandler : INotificationHandler<InferenceFailedNotification>
    {
        private readonly ILogger<InferenceFailedHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IProgressTracker _progressTracker;
        private readonly IErrorTracker _errorTracker;

        public InferenceFailedHandler(
            ILogger<InferenceFailedHandler> logger,
            INotificationService notificationService,
            IProgressTracker progressTracker,
            IErrorTracker errorTracker)
        {
            _logger = logger;
            _notificationService = notificationService;
            _progressTracker = progressTracker;
            _errorTracker = errorTracker;
        }

        public async Task Handle(InferenceFailedNotification notification, CancellationToken ct)
        {
            _logger.LogError("Inference failed for {RequestId}: {ErrorType} - {Error}",
                notification.RequestId, notification.ErrorType, notification.Error);

            // Track error for patterns
            _errorTracker.TrackError(new ErrorEntry
            {
                RequestId = notification.RequestId,
                ModelId = notification.ModelId,
                ErrorType = notification.ErrorType,
                ErrorMessage = notification.Error,
                Timestamp = DateTimeOffset.UtcNow
            });

            // Update progress
            _progressTracker.UpdateProgress(notification.RequestId, new InferenceProgressUpdate
            {
                Status = "Failed",
                Message = notification.Error,
                PercentComplete = 0,
                IsError = true
            });

            // Send notification for critical errors
            if (notification.ErrorType == "InsufficientMemoryException")
            {
                await _notificationService.SendNotificationAsync(
                    "Inference Failed - Memory Issue",
                    $"Model {notification.ModelId} failed due to insufficient memory. Consider unloading unused models.",
                    NotificationType.Error,
                    ct);
            }
        }
    }

    /// <summary>
    /// Audit handler for compliance logging
    /// </summary>
    public class InferenceAuditHandler :
        INotificationHandler<InferenceQueuedNotification>,
        INotificationHandler<InferenceCompletedNotification>,
        INotificationHandler<InferenceFailedNotification>
    {
        private readonly IAuditLogger _auditLogger;

        public InferenceAuditHandler(IAuditLogger auditLogger)
        {
            _auditLogger = auditLogger;
        }

        public Task Handle(InferenceQueuedNotification notification, CancellationToken ct)
        {
            _auditLogger.LogAuditEvent(new AuditEvent
            {
                EventType = "InferenceQueued",
                RequestId = notification.RequestId,
                ModelId = notification.ModelId,
                Priority = notification.Priority.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task Handle(InferenceCompletedNotification notification, CancellationToken ct)
        {
            _auditLogger.LogAuditEvent(new AuditEvent
            {
                EventType = "InferenceCompleted",
                RequestId = notification.RequestId,
                ModelId = notification.ModelId,
                DurationMs = notification.QueueTimeMs + notification.InferenceTimeMs,
                TokensGenerated = notification.TokensGenerated,
                Timestamp = DateTimeOffset.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task Handle(InferenceFailedNotification notification, CancellationToken ct)
        {
            _auditLogger.LogAuditEvent(new AuditEvent
            {
                EventType = "InferenceFailed",
                RequestId = notification.RequestId,
                ModelId = notification.ModelId,
                ErrorType = notification.ErrorType,
                ErrorMessage = notification.Error,
                Timestamp = DateTimeOffset.UtcNow
            });

            return Task.CompletedTask;
        }
    }
}
