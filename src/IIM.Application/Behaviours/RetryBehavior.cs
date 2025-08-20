using IIM.Core.Mediator;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that implements retry logic
    /// </summary>
    [PipelineOrder(5)]
    public class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;
        private const int MaxRetries = 3;

        /// <summary>
        /// Initializes the retry behavior
        /// </summary>
        public RetryBehavior(ILogger<RetryBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Implements retry logic for transient failures
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            // Define retry policy
            var retryPolicy = Policy
                .Handle<TimeoutException>()
                .Or<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount}/{MaxRetries} for {RequestName} after {TimeSpan}s due to {ExceptionType}: {Message}",
                            retryCount, MaxRetries, requestName, timeSpan.TotalSeconds,
                            exception.GetType().Name, exception.Message);
                    });

            try
            {
                return await retryPolicy.ExecuteAsync(async () => await next());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All retries exhausted for {RequestName}", requestName);
                throw;
            }
        }
    }
}