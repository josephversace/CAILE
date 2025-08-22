using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

/// <summary>
/// Interface for audit logging - available to all layers
/// </summary>
public interface IAuditLogger
{
    // Synchronous logging (fire and forget)
    void LogAudit(AuditEvent auditEvent);
    void LogAudit(string eventType, string? entityId = null, Dictionary<string, object>? details = null);

    // Asynchronous logging
    Task LogAuditAsync(AuditEvent auditEvent, CancellationToken ct = default);
    Task LogAuditAsync(string eventType, string? entityId = null, Dictionary<string, object>? details = null, CancellationToken ct = default);

    // Query audit logs
    Task<List<AuditEvent>> GetAuditLogsAsync(AuditLogFilter? filter = null, CancellationToken ct = default);
    Task<List<AuditEvent>> GetAuditLogAsync(long id, CancellationToken ct = default);

    // Cleanup
    Task<int> PurgeOldLogsAsync(DateTimeOffset olderThan, CancellationToken ct = default);

    /// <summary>
    /// Logs an audit entry using your existing audit system
    /// </summary>
    void LogAuditEvent(AuditEvent auditEvent);
}