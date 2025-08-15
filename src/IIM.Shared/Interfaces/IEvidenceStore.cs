using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

public interface IEvidenceStore
{
    Task StoreAsync(string key, object data, CancellationToken cancellationToken = default);
    Task<T?> RetrieveAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
