using IIM.Core.Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that monitors performance
    /// </summary>
    [PipelineOrder(3)]
    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
        private const int WarningThresholdMs = 500;
        private const int CriticalThresholdMs = 3000;

        /// <summary>
        /// Initializes the performance behavior
        /// </summary>
        public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Monitors performance of request handling
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var stopwatch = Stopwatch.StartNew();

            // Track memory before execution
            var memoryBefore = GC.GetTotalMemory(false);

            try
            {
                var response = await next();

                stopwatch.Stop();

                // Track memory after execution
                var memoryAfter = GC.GetTotalMemory(false);
                var memoryUsed = memoryAfter - memoryBefore;

                // Log performance metrics
                if (stopwatch.ElapsedMilliseconds > CriticalThresholdMs)
                {
                    _logger.LogError(
                        "Critical performance issue: {RequestName} took {ElapsedMs}ms (threshold: {Threshold}ms). Memory used: {MemoryMB:F2} MB",
                        requestName, stopwatch.ElapsedMilliseconds, CriticalThresholdMs, memoryUsed / (1024.0 * 1024.0));
                }
                else if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
                {
                    _logger.LogWarning(
                        "Performance warning: {RequestName} took {ElapsedMs}ms (threshold: {Threshold}ms). Memory used: {MemoryMB:F2} MB",
                        requestName, stopwatch.ElapsedMilliseconds, WarningThresholdMs, memoryUsed / (1024.0 * 1024.0));
                }
                else if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Performance metrics: {RequestName} completed in {ElapsedMs}ms. Memory used: {MemoryMB:F2} MB",
                        requestName, stopwatch.ElapsedMilliseconds, memoryUsed / (1024.0 * 1024.0));
                }

                return response;
            }
            catch (Exception)
            {
                stopwatch.Stop();

                _logger.LogError(
                    "Request {RequestName} failed after {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}