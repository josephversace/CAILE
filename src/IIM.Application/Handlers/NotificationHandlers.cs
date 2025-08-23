using IIM.Application.Commands.Investigation;
using IIM.Application.Commands.Models;
using IIM.Application.Commands.Wsl;
using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Handlers
{
    /// <summary>
    /// Handler for WSL setup completion
    /// </summary>
    public class WslSetupCompletedHandler : INotificationHandler<WslSetupCompletedNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<WslSetupCompletedHandler> _logger;

        public WslSetupCompletedHandler(
            INotificationService notificationService,
            ILogger<WslSetupCompletedHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(WslSetupCompletedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("WSL setup completed successfully");

            await _notificationService.ShowToastAsync(
                "WSL Ready",
                $"WSL2 and all services are now running. Distribution: {notification.DistroName}",
                NotificationType.Success);
        }
    }

    /// <summary>
    /// Handler for WSL setup failure
    /// </summary>
    public class WslSetupFailedHandler : INotificationHandler<WslSetupFailedNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<WslSetupFailedHandler> _logger;

        public WslSetupFailedHandler(
            INotificationService notificationService,
            ILogger<WslSetupFailedHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(WslSetupFailedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogError("WSL setup failed: {Error}", notification.Error);

            await _notificationService.ShowToastAsync(
                "WSL Setup Failed",
                notification.Error,
                NotificationType.Error);
        }
    }

    /// <summary>
    /// Handler for model loaded notifications
    /// </summary>
    public class ModelLoadedNotificationHandler : INotificationHandler<ModelLoadedNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<ModelLoadedNotificationHandler> _logger;

        public ModelLoadedNotificationHandler(
            INotificationService notificationService,
            ILogger<ModelLoadedNotificationHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(ModelLoadedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Model {ModelId} loaded in {LoadTime}ms",
                notification.ModelId, notification.LoadTimeMs);

            await _notificationService.ShowToastAsync(
                "Model Loaded",
                $"{notification.ModelId} is ready for inference ({notification.LoadTimeMs}ms)",
                NotificationType.Success);
        }
    }

    /// <summary>
    /// Audit handler for model loading
    /// </summary>
    public class ModelLoadedAuditHandler : INotificationHandler<ModelLoadedNotification>
    {
        private readonly ILogger<ModelLoadedAuditHandler> _logger;

        public ModelLoadedAuditHandler(ILogger<ModelLoadedAuditHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ModelLoadedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[AUDIT] Model loaded - ID: {ModelId} | Provider: {Provider} | Type: {Type} | Memory: {Memory}MB | Time: {Timestamp}",
                notification.ModelId,
                notification.Provider,
                notification.ModelType,
                notification.MemoryUsage / (1024 * 1024),
                notification.Timestamp);

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handler for investigation completion
    /// </summary>
    public class InvestigationCompletedHandler : INotificationHandler<InvestigationQueryCompletedNotification>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<InvestigationCompletedHandler> _logger;

        public InvestigationCompletedHandler(
            INotificationService notificationService,
            ILogger<InvestigationCompletedHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(InvestigationQueryCompletedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Investigation query completed in {Time}ms with {ToolCount} tools",
                notification.ProcessingTimeMs, notification.ToolsUsed.Count);

            if (notification.CitationCount > 0)
            {
                await _notificationService.ShowNotificationAsync(
                    $"Found {notification.CitationCount} relevant sources",
                    NotificationType.Info,
                    3000);
            }
        }
    }
}