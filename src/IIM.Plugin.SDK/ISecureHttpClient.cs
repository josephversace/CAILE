using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Plugin.SDK;

/// <summary>
/// Brokered HTTP client that enforces egress policy (allowlists, headers, logging).
/// Implementations should apply domain restrictions and redact sensitive data.
/// </summary>
public interface ISecureHttpClient
{
    Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        HttpContent? content = null,
        IDictionary<string, string>? headers = null,
        CancellationToken ct = default);
}
