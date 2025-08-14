using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Plugin.SDK;

public static class SecureHttpClientExtensions
{
    // Bridges your existing ISecureHttpClient to a familiar PostAsync() shape.
  public static Task<HttpResponseMessage> PostAsync(
    this ISecureHttpClient http,
    string url,
    HttpContent? content = null,
    CancellationToken ct = default)
        => http.SendAsync(HttpMethod.Post, url, content, null, ct);
}
