using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<AuditService> _logger;

        public AuditService(IAuditRepository auditRepository, ILogger<AuditService> logger)
        {
            _auditRepository = auditRepository;
            _logger = logger;
        }

        public async Task LogAuditAsync(AuditEvent auditEvent)
        {
            try
            {
                await _auditRepository.AddAuditLogAsync(auditEvent);
                _logger.LogDebug("Audit event logged: {EventType} by {UserId}",
                    auditEvent.EventType, auditEvent.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit event: {EventType}", auditEvent.EventType);
            }
        }

        public void LogAudit(AuditEvent auditEvent)
        {
            Task.Run(async () => await LogAuditAsync(auditEvent));
        }

        public void LogAudit(string eventType, string? entityId = null, Dictionary<string, object>? details = null)
        {
            throw new NotImplementedException();
        }

        public Task LogAuditAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task LogAuditAsync(string eventType, string? entityId = null, Dictionary<string, object>? details = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<AuditEvent>> GetAuditLogsAsync(AuditLogFilter? filter = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<AuditEvent>> GetAuditLogAsync(long id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> PurgeOldLogsAsync(DateTimeOffset olderThan, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public void LogAuditEvent(AuditEvent auditEvent)
        {
            throw new NotImplementedException();
        }
    }
}
