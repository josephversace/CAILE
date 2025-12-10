using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
    public interface IConfigRepository
    {
        Task<List<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
        Task AddAsync(Setting setting, CancellationToken cancellationToken = default);
        Task UpdateAsync(Setting setting, CancellationToken cancellationToken = default);
        Task DeleteAsync(string key, CancellationToken cancellationToken = default);

        Task SetJsonAsync<T>(string key, T value, string category, CancellationToken ct = default);

        Task<T?> GetJsonAsync<T>(string key, CancellationToken ct = default);


	}
}
