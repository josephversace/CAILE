using IIM.Core.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that audits command execution
    /// </summary>
    [PipelineOrder(7)]
    public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        /// <summary>
        /// Initializes the audit behavior
        /// </summary>
        public AuditBehavior(
            ILogger<AuditBehavior<TRequest, TResponse>> logger,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Audits command execution for compliance
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Only audit commands, not queries
            if (request is IQuery<TResponse>)
            {
                return await next();
            }

            var requestName = typeof(TRequest).Name;
            var userId = GetUserId();
            var timestamp = DateTimeOffset.UtcNow;
            var requestData = SerializeRequest(request);

            _logger.LogInformation(
                "[AUDIT] User {UserId} executing {RequestName} at {Timestamp}. Request: {RequestData}",
                userId, requestName, timestamp, requestData);

            try
            {
                var response = await next();

                _logger.LogInformation(
                    "[AUDIT] User {UserId} successfully executed {RequestName} at {Timestamp}",
                    userId, requestName, DateTimeOffset.UtcNow);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "[AUDIT] User {UserId} failed to execute {RequestName} at {Timestamp}. Error: {Error}",
                    userId, requestName, DateTimeOffset.UtcNow, ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Gets the current user ID
        /// </summary>
        private string GetUserId()
        {
            var user = _httpContextAccessor?.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.Identity.Name
                    ?? "Unknown";
            }

            return "System";
        }

        /// <summary>
        /// Serializes the request for auditing
        /// </summary>
        private string SerializeRequest(TRequest request)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    MaxDepth = 2
                };

                return JsonSerializer.Serialize(request, options);
            }
            catch
            {
                return $"{{\"type\":\"{typeof(TRequest).Name}\"}}";
            }
        }
    }
}