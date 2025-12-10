using IIM.Shared.Mediator;
using IIM.Application.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Handlers
{

	/// <summary>
	/// Handler for model loaded notifications
	/// </summary>
	public class ModelLoadedNotificationHandler : INotificationHandler<ModelLoadedNotification>
	{
		private readonly ILogger<ModelLoadedNotificationHandler> _logger;

		public ModelLoadedNotificationHandler(ILogger<ModelLoadedNotificationHandler> logger)
		{
			_logger = logger;
		}

		public Task Handle(ModelLoadedNotification notification, CancellationToken cancellationToken)
		{
			_logger.LogInformation(
				"Model loaded: ModelId={ModelId}, LoadTimeMs={LoadMs}, Provider={Provider}, Type={Type}",
				notification.ModelId,
				notification.LoadTimeMs,
				notification.Provider,
				notification.ModelType
			);

			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// Audit-only handler for model loading
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
				"[AUDIT] Model loaded — ID={ModelId} | Provider={Provider} | Type={Type} | MemoryMB={MemoryMB} | Time={Timestamp}",
				notification.ModelId,
				notification.Provider,
				notification.ModelType,
				notification.MemoryUsage / (1024 * 1024),
				notification.Timestamp
			);

			return Task.CompletedTask;
		}
	}
}
