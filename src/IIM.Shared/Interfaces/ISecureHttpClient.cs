using System;
using System.Net.Http;  
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

public interface ISecureHttpClient
{
    Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}
