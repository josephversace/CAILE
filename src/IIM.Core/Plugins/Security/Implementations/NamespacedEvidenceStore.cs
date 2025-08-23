using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Plugins.Security.Implementations;

public class NamespacedEvidenceStore : IEvidenceStore
{
    private readonly string _namespace;
    private readonly ILogger<NamespacedEvidenceStore> _logger;
    private readonly ConcurrentDictionary<string, object> _store = new();

    public NamespacedEvidenceStore(string ns, ILogger<NamespacedEvidenceStore> logger)
    {
        _namespace = ns;
        _logger = logger;
    }

    public Task StoreAsync(string key, object data, CancellationToken cancellationToken = default)
    {
        var namespacedKey = $"{_namespace}:{key}";
        _store[namespacedKey] = data;
        _logger.LogInformation("Stored evidence at key {Key}", namespacedKey);
        return Task.CompletedTask;
    }

    public Task<T?> RetrieveAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var namespacedKey = $"{_namespace}:{key}";
        if (_store.TryGetValue(namespacedKey, out var value))
        {
            if (value is T typedValue)
                return Task.FromResult<T?>(typedValue);
        }
        return Task.FromResult<T?>(default(T));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var namespacedKey = $"{_namespace}:{key}";
        return Task.FromResult(_store.ContainsKey(namespacedKey));
    }
}
