using IIM.Core.Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that logs all requests and responses
    /// </summary>
    [PipelineOrder(1)]
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes the logging behavior
        /// </summary>
        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                MaxDepth = 3
            };
        }

        /// <summary>
        /// Handles the request and logs information
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var stopwatch = Stopwatch.StartNew();

            // Log request
            try
            {
                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                _logger.LogInformation(
                    "[{RequestId}] Processing {RequestName}: {RequestData}",
                    requestId, requestName, requestJson);
            }
            catch
            {
                _logger.LogInformation(
                    "[{RequestId}] Processing {RequestName}",
                    requestId, requestName);
            }

            try
            {
                // Execute the request
                var response = await next();

                stopwatch.Stop();

                // Log successful response
                _logger.LogInformation(
                    "[{RequestId}] Completed {RequestName} in {ElapsedMs}ms",
                    requestId, requestName, stopwatch.ElapsedMilliseconds);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    try
                    {
                        var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                        _logger.LogDebug(
                            "[{RequestId}] Response: {ResponseData}",
                            requestId, responseJson);
                    }
                    catch
                    {
                        // Ignore serialization errors for response
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log error
                _logger.LogError(ex,
                    "[{RequestId}] Failed {RequestName} after {ElapsedMs}ms",
                    requestId, requestName, stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}