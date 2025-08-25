using IIM.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IConfigRepository
    {
        Task<List<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
        Task AddAsync(Setting setting, CancellationToken cancellationToken = default);
        Task UpdateAsync(Setting setting, CancellationToken cancellationToken = default);
        Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    }
}
