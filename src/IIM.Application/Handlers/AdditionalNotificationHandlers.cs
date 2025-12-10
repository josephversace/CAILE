using System.Threading;
using System.Threading.Tasks;
using IIM.Application.Investigation;
using IIM.Application.Models;
using IIM.Shared.Mediator;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Handlers
{
	
	// ------------------------------------------------------------
	// Model Load Failed
	// ------------------------------------------------------------
	public class ModelLoadFailedHandler : INotificationHandler<ModelLoadFailedNotification>
	{
		private readonly ILogger<ModelLoadFailedHandler> _logger;

		public ModelLoadFailedHandler(ILogger<ModelLoadFailedHandler> logger)
		{
			_logger = logger;
		}

		public Task Handle(ModelLoadFailedNotification notification, CancellationToken cancellationToken)
		{
			_logger.LogError(
				"Model load failed: ModelId={ModelId}, Error={Error}",
				notification.ModelId,
				notification.Error
			);

			return Task.CompletedTask;
		}
	}

	// ------------------------------------------------------------
	// Model Unloaded
	// ------------------------------------------------------------
	public class ModelUnloadedHandler : INotificationHandler<ModelUnloadedNotification>
	{
		private readonly ILogger<ModelUnloadedHandler> _logger;

		public ModelUnloadedHandler(ILogger<ModelUnloadedHandler> logger)
		{
			_logger = logger;
		}

		public Task Handle(ModelUnloadedNotification notification, CancellationToken cancellationToken)
		{
			_logger.LogInformation(
				"Model {ModelId} unloaded at {Timestamp}",
				notification.ModelId,
				notification.Timestamp
			);

			return Task.CompletedTask;
		}
	}
}
