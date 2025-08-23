using IIM.Core.Mediator;
using IIM.Shared.Interfaces;
using IIM.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that audits command execution to both logs and database
    /// </summary>
    [PipelineOrder(7)]
    public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;
        private readonly IAuditLogger _auditLogger;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        /// <summary>
        /// Initializes the audit behavior with database support
        /// </summary>
        public AuditBehavior(
            ILogger<AuditBehavior<TRequest, TResponse>> logger,
            IAuditLogger auditLogger,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Audits command execution for compliance with database persistence
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Only audit commands, not queries (unless sensitive)
            if (!ShouldAudit(request))
            {
                return await next();
            }

            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString();
            var userId = GetUserId();
            var userIp = GetUserIp();
            var timestamp = DateTimeOffset.UtcNow;
            var requestData = SerializeRequest(request);
            var stopwatch = Stopwatch.StartNew();

            // Log to console/file (existing)
            _logger.LogInformation(
                "[AUDIT] User {UserId} executing {RequestName} at {Timestamp}. Request: {RequestData}",
                userId, requestName, timestamp, requestData);

            // Create audit event for database - using correct property types
            var startEvent = new AuditEvent
            {
                // Id is auto-generated if using EF Core identity
                EventType = $"{requestName}_STARTED",
                EntityId = ExtractEntityId(request),
                EntityType = ExtractEntityType(requestName),
                UserId = userId,
                IpAddress = userIp,
                Timestamp = timestamp.UtcDateTime, // Convert to DateTime if needed
                Action = "COMMAND_START",
                Details = requestData,
                AdditionalData = new Dictionary<string, object>
                {
                    ["RequestId"] = requestId,
                    ["RequestType"] = requestName,
                    ["UserName"] = GetUserName(), // Store username in AdditionalData
                    ["HttpMethod"] = _httpContextAccessor?.HttpContext?.Request.Method ?? "N/A",
                    ["Path"] = _httpContextAccessor?.HttpContext?.Request.Path.ToString() ?? "N/A"
                }
            };

            // Persist to database
            await _auditLogger.LogAuditAsync(startEvent, cancellationToken);

            try
            {
                var response = await next();
                stopwatch.Stop();

                // Log success to console/file
                _logger.LogInformation(
                    "[AUDIT] User {UserId} successfully executed {RequestName} in {ElapsedMs}ms",
                    userId, requestName, stopwatch.ElapsedMilliseconds);

                // Create completion event for database
                var completionEvent = new AuditEvent
                {
                    EventType = $"{requestName}_COMPLETED",
                    EntityId = ExtractEntityId(request),
                    EntityType = ExtractEntityType(requestName),
                    UserId = userId,
                    IpAddress = userIp,
                    Timestamp = DateTimeOffset.UtcNow.UtcDateTime,
                    Action = "COMMAND_SUCCESS",
                    Details = $"Completed in {stopwatch.ElapsedMilliseconds}ms",
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["RequestId"] = requestId,
                        ["RequestType"] = requestName,
                        ["UserName"] = GetUserName(),
                        ["ElapsedMs"] = stopwatch.ElapsedMilliseconds,
                        ["Success"] = true,
                        ["ResponseType"] = response?.GetType().Name ?? "null"
                    }
                };

                // Extract changes for update commands (if you define IUpdateCommand interface)
                if (IsUpdateCommand(request) && response != null)
                {
                    completionEvent.AdditionalData["Changes"] = ExtractChanges(request, response);
                }

                // Persist to database
                await _auditLogger.LogAuditAsync(completionEvent, cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log failure to console/file
                _logger.LogError(ex,
                    "[AUDIT] User {UserId} failed to execute {RequestName} after {ElapsedMs}ms. Error: {Error}",
                    userId, requestName, stopwatch.ElapsedMilliseconds, ex.Message);

                // Create failure event for database
                var failureEvent = new AuditEvent
                {
                    EventType = $"{requestName}_FAILED",
                    EntityId = ExtractEntityId(request),
                    EntityType = ExtractEntityType(requestName),
                    UserId = userId,
                    IpAddress = userIp,
                    Timestamp = DateTimeOffset.UtcNow.UtcDateTime,
                    Action = "COMMAND_FAILURE",
                    Details = ex.Message,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["RequestId"] = requestId,
                        ["RequestType"] = requestName,
                        ["UserName"] = GetUserName(),
                        ["ElapsedMs"] = stopwatch.ElapsedMilliseconds,
                        ["Success"] = false,
                        ["ExceptionType"] = ex.GetType().Name,
                        ["StackTrace"] = ex.StackTrace ?? string.Empty
                    }
                };

                // Persist to database
                await _auditLogger.LogAuditAsync(failureEvent, cancellationToken);

                throw;
            }
        }

        /// <summary>
        /// Determines if the request should be audited
        /// </summary>
        private bool ShouldAudit(TRequest request)
        {
            // Always audit commands
            if (request is ICommand)
                return true;

            // Skip most queries
            if (request is IQuery<TResponse>)
            {
                var queryName = typeof(TRequest).Name;

                // But audit sensitive queries
                var sensitiveQueries = new[]
                {
                    "GetAuditLogsQuery",
                    "GetEvidenceQuery",
                    "GetInvestigationQuery",
                    "GetUserPermissionsQuery"
                };

                return Array.Exists(sensitiveQueries, q => queryName.Contains(q));
            }

            return true; // Default to auditing
        }

        /// <summary>
        /// Check if request is an update command
        /// </summary>
        private bool IsUpdateCommand(TRequest request)
        {
            var requestName = typeof(TRequest).Name;
            return requestName.Contains("Update") || requestName.Contains("Modify") || requestName.Contains("Change");
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
                    ?? user.FindFirst("sub")?.Value
                    ?? user.Identity.Name
                    ?? "Unknown";
            }

            return "System";
        }

        /// <summary>
        /// Gets the current user's display name
        /// </summary>
        private string GetUserName()
        {
            var user = _httpContextAccessor?.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.FindFirst(ClaimTypes.Name)?.Value
                    ?? user.Identity.Name
                    ?? "Unknown User";
            }

            return "System";
        }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        private string? GetUserIp()
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null) return null;

            // Check for forwarded IP (behind proxy/load balancer)
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
            {
                return forwarded.Split(',').First().Trim();
            }

            // Check real IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to connection IP
            return context.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Extracts entity ID from the request
        /// </summary>
        private string? ExtractEntityId(TRequest request)
        {
            var type = request.GetType();

            // Try common property names
            var properties = new[] { "Id", "EntityId", "EvidenceId", "SessionId", "ModelId", "CaseId" };

            foreach (var propName in properties)
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(request);
                    if (value != null)
                        return value.ToString();
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts entity type from request name
        /// </summary>
        private string ExtractEntityType(string requestName)
        {
            // Remove common suffixes
            var entityType = requestName
                .Replace("Command", "")
                .Replace("Query", "")
                .Replace("Create", "")
                .Replace("Update", "")
                .Replace("Delete", "")
                .Replace("Process", "")
                .Replace("Get", "");

            // Map to known entity types
            return entityType switch
            {
                var s when s.Contains("Investigation") => "Investigation",
                var s when s.Contains("Evidence") => "Evidence",
                var s when s.Contains("Model") => "Model",
                var s when s.Contains("Session") => "Session",
                var s when s.Contains("User") => "User",
                _ => entityType
            };
        }

        /// <summary>
        /// Extracts changes for update commands
        /// </summary>
        private object ExtractChanges(TRequest request, TResponse response)
        {
            // This would need reflection to compare before/after states
            // For now, just return the request data
            return new
            {
                RequestData = SerializeRequest(request),
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        /// <summary>
        /// Serializes the request for auditing (with sensitive data filtering)
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

                var json = JsonSerializer.Serialize(request, options);

                // Filter sensitive data
                var sensitiveFields = new[] { "password", "token", "secret", "apikey", "connectionstring" };
                foreach (var field in sensitiveFields)
                {
                    json = System.Text.RegularExpressions.Regex.Replace(
                        json,
                        $"\"{field}\"\\s*:\\s*\"[^\"]*\"",
                        $"\"{field}\":\"[REDACTED]\"",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }

                // Truncate if too long for database
                if (json.Length > 4000)
                {
                    json = json.Substring(0, 3997) + "...";
                }

                return json;
            }
            catch
            {
                return $"{{\"type\":\"{typeof(TRequest).Name}\"}}";
            }
        }
    }
}