using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Infrastructure.Data.Entities;

namespace IIM.Infrastructure.Data.Services
{
    /// <summary>
    /// SQLite-based audit logger implementation
    /// </summary>
    public class SqliteAuditLogger : IAuditLogger
    {
        private readonly IIMDbContext _context;
        private readonly ILogger<SqliteAuditLogger> _logger;

        public SqliteAuditLogger(IIMDbContext context, ILogger<SqliteAuditLogger> logger)
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

        public async Task LogAuditAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            try
            {
                var entity = AuditLogEntity.FromAuditEvent(auditEvent);
                _context.AuditLogs.Add(entity);
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

        public async Task<List<AuditLog>> GetAuditLogsAsync(AuditLogFilter? filter = null, CancellationToken ct = default)
        {
            var query = _context.AuditLogs.AsNoTracking();

            if (filter != null)
            {
                if (filter.StartDate.HasValue)
                    query = query.Where(a => a.Timestamp >= filter.StartDate.Value.UtcDateTime);

                if (filter.EndDate.HasValue)
                    query = query.Where(a => a.Timestamp <= filter.EndDate.Value.UtcDateTime);

                if (!string.IsNullOrEmpty(filter.EventType))
                    query = query.Where(a => a.EventType == filter.EventType);

                if (!string.IsNullOrEmpty(filter.UserId))
                    query = query.Where(a => a.UserId == filter.UserId);

                if (!string.IsNullOrEmpty(filter.EntityId))
                    query = query.Where(a => a.EntityId == filter.EntityId);

                if (!string.IsNullOrEmpty(filter.EntityType))
                    query = query.Where(a => a.EntityType == filter.EntityType);

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
            return entities.Select(e => e.ToDomainModel()).ToList();
        }

        public async Task<AuditLog?> GetAuditLogAsync(long id, CancellationToken ct = default)
        {
            var entity = await _context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            return entity?.ToDomainModel();
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
    }
}