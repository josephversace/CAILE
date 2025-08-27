using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IModelParameterSetRepository
    {
        Task<List<ModelParameterSet>> GetByModelIdAsync(string modelId, CancellationToken cancellationToken = default);
        Task<ModelParameterSet?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task AddAsync(ModelParameterSet parameterSet, CancellationToken cancellationToken = default);
        Task UpdateAsync(ModelParameterSet parameterSet, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }

}
