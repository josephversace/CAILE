using IIM.Core.Configuration;

using IIM.Shared.Interfaces;
using IIM.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.Services
{
    /// <summary>
    /// SQLite-based audit logger implementation
    /// </summary>
    public class SqliteAuditLogger : IAuditLogger
    {
        private readonly AuditDbContext _context;
        private readonly ILogger<SqliteAuditLogger> _logger;

        public SqliteAuditLogger(AuditDbContext context, ILogger<SqliteAuditLogger> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void LogAudit(AuditEvent auditEvent)
        {
            // Fire and forget
            Task.Run(async () => await LogAuditAsync(auditEvent));
        }

        public void LogAudit(string eventType, string? entityId = null, Dictionary<string, object>? details = null)
        {
            var evt = new AuditEvent
            {
                EventType = eventType,
                EntityId = entityId,
                AdditionalData = details
            };
            LogAudit(evt);
        }

        /// <summary>
        /// Logs an audit entry using your existing audit system
        /// </summary>
        public void LogLogAuditEventAudit(AuditEvent auditEvent)
        {
     
            LogAudit(auditEvent);
        }

        public async Task LogAuditAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            try
            {
                _context.AuditLogs.Add(auditEvent);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Don't throw from audit logger - just log the error
                _logger.LogError(ex, "Failed to write audit log for event {EventType}", auditEvent.EventType);
            }
        }

        public async Task LogAuditAsync(string eventType, string? entityId = null, Dictionary<string, object>? details = null, CancellationToken ct = default)
        {
            var evt = new AuditEvent
            {
                EventType = eventType,
                EntityId = entityId,
                AdditionalData = details
            };
            await LogAuditAsync(evt, ct);
        }

        public async Task<List<AuditEvent>> GetAuditLogsAsync(AuditLogFilter? filter = null, CancellationToken ct = default)
        {
            var query = _context.AuditLogs.AsNoTracking();

            if (filter != null)
            {
                if (filter.StartDate.HasValue)
                    query = query.Where(a => a.Timestamp >= filter.StartDate.Value.UtcDateTime);

                if (filter.EndDate.HasValue)
                    query = query.Where(a => a.Timestamp <= filter.EndDate.Value.UtcDateTime);

                //if (!string.IsNullOrEmpty(filter.EventType))
                //    query = query.Where(a => a.EventType == filter.EventType);

                //if (!string.IsNullOrEmpty(filter.UserId))
                //    query = query.Where(a => a.User == filter.UserId);

                //if (!string.IsNullOrEmpty(filter.EntityId))
                //    query = query.Where(a => a.EntityId == filter.EntityId);

                //if (!string.IsNullOrEmpty(filter.EntityType))
                //    query = query.Where(a => a.EntityType == filter.EntityType);

                query = query.OrderByDescending(a => a.Timestamp);

                if (filter.Offset.HasValue)
                    query = query.Skip(filter.Offset.Value);

                if (filter.Limit.HasValue)
                    query = query.Take(filter.Limit.Value);
            }
            else
            {
                query = query.OrderByDescending(a => a.Timestamp).Take(100);
            }

            var entities = await query.ToListAsync(ct);
            return entities;
        }


        public async Task<List<AuditEvent>?> GetAuditLogAsync(long id, CancellationToken ct = default)
        {
            var entity = await _context.AuditLogs.ToListAsync();
     

            return entity;
        }


        public async Task<int> PurgeOldLogsAsync(DateTimeOffset olderThan, CancellationToken ct = default)
        {
            var cutoff = olderThan.UtcDateTime;
            var toDelete = await _context.AuditLogs
                .Where(a => a.Timestamp < cutoff)
                .ToListAsync(ct);

            if (toDelete.Any())
            {
                _context.AuditLogs.RemoveRange(toDelete);
                await _context.SaveChangesAsync(ct);
            }

            return toDelete.Count;
        }

    

        public void LogAuditEvent(AuditEvent auditEvent)
        {
            LogAudit(auditEvent);
        }
    }
}