using IIM.Core.Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace IIM.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that wraps commands in transactions
    /// </summary>
    [PipelineOrder(6)]
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

        /// <summary>
        /// Initializes the transaction behavior
        /// </summary>
        public TransactionBehavior(ILogger<TransactionBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Wraps command execution in a transaction
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Only apply transactions to commands, not queries
            if (request is not ICommand)
            {
                return await next();
            }

            var requestName = typeof(TRequest).Name;

            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromSeconds(30)
                },
                TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                _logger.LogDebug("Beginning transaction for {RequestName}", requestName);

                var response = await next();

                scope.Complete();

                _logger.LogDebug("Transaction completed for {RequestName}", requestName);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed for {RequestName}, rolling back", requestName);
                throw;
            }
        }
    }
}