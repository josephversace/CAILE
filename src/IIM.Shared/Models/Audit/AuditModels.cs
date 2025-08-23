using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models.Audit
{
    // <summary>
    /// Audit event for tracking system activities
    /// </summary>
    public class AuditEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public Dictionary<string, object>? AdditionalData { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
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
        public int Limit { get; set; } = 100;
        public int Offset { get; set; } = 0;
    }

    // ===== Process Related =====

    /// <summary>
    /// Result from process execution
    /// </summary>
    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public bool Success => ExitCode == 0;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

}
