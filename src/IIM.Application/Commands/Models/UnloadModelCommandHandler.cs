using IIM.Core.AI;
using IIM.Core.Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Commands.Models
{
    /// <summary>
    /// Handler for unloading models
    /// </summary>
    public class UnloadModelCommandHandler : IRequestHandler<UnloadModelCommand, bool>
    {
        private readonly IModelOrchestrator _orchestrator;
        private readonly IMediator _mediator;
        private readonly ILogger<UnloadModelCommandHandler> _logger;

        public UnloadModelCommandHandler(
            IModelOrchestrator orchestrator,
            IMediator mediator,
            ILogger<UnloadModelCommandHandler> logger)
        {
            _orchestrator = orchestrator;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(UnloadModelCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Unloading model {ModelId}", request.ModelId);

                var result = await _orchestrator.UnloadModelAsync(request.ModelId, cancellationToken);

                if (result)
                {
                    await _mediator.Publish(new ModelUnloadedNotification
                    {
                        ModelId = request.ModelId,
                        Timestamp = DateTimeOffset.UtcNow
                    }, cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload model {ModelId}", request.ModelId);
                throw;
            }
        }
    }
}
