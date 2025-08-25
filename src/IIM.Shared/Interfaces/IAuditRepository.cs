// File: IIM.Shared.Interfaces/IAuditRepository.cs
using IIM.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IAuditRepository
    {
        Task AddEventAsync(AuditEvent evt, CancellationToken cancellationToken = default);
        Task<List<AuditEvent>> GetEventsAsync(CancellationToken cancellationToken = default);
        Task<AuditEvent?> GetEventByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
