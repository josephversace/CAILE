using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{




    /// <summary>
    /// Audit event for logging
    /// </summary>
    public class AuditEvent
    {
        public long Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? RequestId { get; set; }
        public string? UserId { get; set; }
        public string? ModelId { get; set; }
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public string? Result { get; set; }
        public string? Details { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Priority { get; set; }
        public long? DurationMs { get; set; }
        public int? TokensGenerated { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Filter for querying audit logs
    /// </summary>
    public class AuditLogFilter
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? EventType { get; set; }
        public string? UserId { get; set; }
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public int? Limit { get; set; } = 100;
        public int? Offset { get; set; }
    }
}