using IIM.Shared.Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Investigation
{
	/// <summary>
	/// Temporary stub replacing legacy model-orchestration unloading.
	/// Always returns true until ONNX/Foundry orchestration is implemented.
	/// </summary>
	public class UnloadModelCommandHandler
		: IRequestHandler<UnloadModelCommand, bool>
	{
		private readonly IMediator _mediator;
		private readonly ILogger<UnloadModelCommandHandler> _logger;

		public UnloadModelCommandHandler(
			IMediator mediator,
			ILogger<UnloadModelCommandHandler> logger)
		{
			_mediator = mediator;
			_logger = logger;
		}

		public async Task<bool> Handle(
			UnloadModelCommand request,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation("Stub: Unloading model {ModelId}", request.ModelId);

			// Since no model orchestrator exists, do nothing.
			// This will be replaced when the new runtime is integrated.

			// Optionally still publish the unload event.
			try
			{
				await _mediator.Publish(new ModelUnloadedNotification
				{
					ModelId = request.ModelId,
					Timestamp = DateTimeOffset.UtcNow
				}, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Stub unload notification failed.");
			}

			return true;
		}
	}
}
