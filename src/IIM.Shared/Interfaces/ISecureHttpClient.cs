using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface ISecureHttpClient
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
        Task<string> GetStringAsync(string requestUri);
        Task<byte[]> GetByteArrayAsync(string requestUri);
    }
}
