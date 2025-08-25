// File: IIM.Infrastructure.Data/EfAuditRepository.cs
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data
{
    public class EfAuditRepository : IAuditRepository
    {
        private readonly AuditDbContext _db;

        public EfAuditRepository(AuditDbContext db)
        {
            _db = db;
        }

        public async Task AddEventAsync(AuditEvent evt, CancellationToken cancellationToken = default)
        {
            _db.AuditLogs.Add(evt);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<AuditEvent>> GetEventsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.AuditLogs.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<AuditEvent?> GetEventByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.AuditLogs.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }
    }
}
