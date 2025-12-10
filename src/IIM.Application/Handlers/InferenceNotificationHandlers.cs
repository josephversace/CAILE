using IIM.Shared.Mediator;
using IIM.Application.Inference;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Handlers
{
	public class InferenceQueuedHandler : INotificationHandler<InferenceQueuedNotification>
	{
		private readonly ILogger<InferenceQueuedHandler> _logger;

		public InferenceQueuedHandler(ILogger<InferenceQueuedHandler> logger)
		{
			_logger = logger;
		}

		public Task Handle(InferenceQueuedNotification notification, CancellationToken cancellationToken)
		{
			_logger.LogInformation(
				"Inference queued: RequestId={RequestId}, Model={ModelId}, Priority={Priority}",
				notification.RequestId,
				notification.ModelId,
				notification.Priority
			);

			return Task.CompletedTask;
		}
	}
}
