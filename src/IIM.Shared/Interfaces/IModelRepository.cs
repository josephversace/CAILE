using IIM.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IModelRepository
    {
        Task<List<ModelConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ModelConfiguration?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task AddAsync(ModelConfiguration config, CancellationToken cancellationToken = default);
        Task UpdateAsync(ModelConfiguration config, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
