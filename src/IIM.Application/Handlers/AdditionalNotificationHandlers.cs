using IIM.Shared.Mediator;
using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using IIM.Application.Investigation;
using IIM.Application.Models;
using IIM.Application.Wsl;

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


}