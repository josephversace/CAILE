using IIM.Application.Commands.Investigation;
using IIM.Application.Commands.Models;
using IIM.Application.Commands.Wsl;
using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Handlers
{
    /// <summary>
    /// Handler for WSL feature enabled notification
    /// </summary>
    public class WslFeatureEnabledHandler : INotificationHandler<WslFeatureEnabledNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<WslFeatureEnabledHandler> _logger;

        public WslFeatureEnabledHandler(
            INotificationService notificationService,
            ILogger<WslFeatureEnabledHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(WslFeatureEnabledNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("WSL feature enabled at {Timestamp}", notification.Timestamp);

            var message = notification.RequiresRestart
                ? "WSL feature enabled. Please restart your computer to complete installation."
                : "WSL feature enabled successfully.";

            await _notificationService.ShowToastAsync("WSL Status", message, NotificationType.Info);
        }
    }

    /// <summary>
    /// Handler for WSL distro installed notification
    /// </summary>
    public class WslDistroInstalledHandler : INotificationHandler<WslDistroInstalledNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<WslDistroInstalledHandler> _logger;

        public WslDistroInstalledHandler(
            INotificationService notificationService,
            ILogger<WslDistroInstalledHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(WslDistroInstalledNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("WSL distro {DistroName} installed with state {State}",
                notification.DistroName, notification.State);

            await _notificationService.ShowToastAsync(
                "Distribution Installed",
                $"{notification.DistroName} (v{notification.Version}) is now available",
                NotificationType.Success);
        }
    }

    /// <summary>
    /// Handler for model load failure
    /// </summary>
    public class ModelLoadFailedHandler : INotificationHandler<ModelLoadFailedNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<ModelLoadFailedHandler> _logger;

        public ModelLoadFailedHandler(
            INotificationService notificationService,
            ILogger<ModelLoadFailedHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(ModelLoadFailedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogError("Failed to load model {ModelId}: {Error}",
                notification.ModelId, notification.Error);

            await _notificationService.ShowToastAsync(
                "Model Load Failed",
                $"Failed to load {notification.ModelId}: {notification.Error}",
                NotificationType.Error);
        }
    }

    /// <summary>
    /// Handler for model unloaded
    /// </summary>
    public class ModelUnloadedHandler : INotificationHandler<ModelUnloadedNotification>
    {
        private readonly ILogger<ModelUnloadedHandler> _logger;

        public ModelUnloadedHandler(ILogger<ModelUnloadedHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ModelUnloadedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Model {ModelId} unloaded at {Timestamp}",
                notification.ModelId, notification.Timestamp);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handler for investigation query started
    /// </summary>
    public class InvestigationQueryStartedHandler : INotificationHandler<InvestigationQueryStartedNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<InvestigationQueryStartedHandler> _logger;

        public InvestigationQueryStartedHandler(
            INotificationService notificationService,
            ILogger<InvestigationQueryStartedHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(InvestigationQueryStartedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Investigation query started for session {SessionId}", notification.SessionId);

            await _notificationService.ShowNotificationAsync(
                "Processing query...",
                NotificationType.Info,
                2000);
        }
    }

    /// <summary>
    /// Handler for investigation query completed
    /// </summary>
    public class InvestigationQueryCompletedHandler : INotificationHandler<InvestigationQueryCompletedNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<InvestigationQueryCompletedHandler> _logger;

        public InvestigationQueryCompletedHandler(
            INotificationService notificationService,
            ILogger<InvestigationQueryCompletedHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(InvestigationQueryCompletedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Query completed in {Time}ms using {ToolCount} tools with {CitationCount} citations",
                notification.ProcessingTimeMs, notification.ToolsUsed.Count, notification.CitationCount);

            var message = notification.CitationCount > 0
                ? $"Query completed with {notification.CitationCount} citations"
                : "Query completed successfully";

            await _notificationService.ShowNotificationAsync(message, NotificationType.Success, 3000);
        }
    }

    /// <summary>
    /// Handler for investigation query failed
    /// </summary>
    public class InvestigationQueryFailedHandler : INotificationHandler<InvestigationQueryFailedNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<InvestigationQueryFailedHandler> _logger;

        public InvestigationQueryFailedHandler(
            INotificationService notificationService,
            ILogger<InvestigationQueryFailedHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(InvestigationQueryFailedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogError("Query failed for session {SessionId}: {Error}",
                notification.SessionId, notification.Error);

            await _notificationService.ShowToastAsync(
                "Query Failed",
                notification.Error,
                NotificationType.Error);
        }
    }

    /// <summary>
    /// Handler for session created
    /// </summary>
    public class SessionCreatedHandler : INotificationHandler<SessionCreatedNotification>
    {
        private readonly ILogger<SessionCreatedHandler> _logger;

        public SessionCreatedHandler(ILogger<SessionCreatedHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(SessionCreatedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Session {SessionId} created for case {CaseId} with title '{Title}'",
                notification.SessionId, notification.CaseId, notification.Title);
            return Task.CompletedTask;
        }
    }
}