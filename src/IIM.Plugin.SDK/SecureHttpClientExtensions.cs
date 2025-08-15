using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;

namespace IIM.Plugin.SDK;

public static class SecureHttpClientExtensions
{
    /// <summary>
    /// Send HTTP request with retry logic
    /// </summary>
    public static async Task<HttpResponseMessage> SendWithRetryAsync(
        this ISecureHttpClient client,
        HttpRequestMessage request,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                // Simple call without the extra parameters
                return await client.SendAsync(request, cancellationToken);
            }
            catch (Exception) when (attempt < maxRetries - 1)
            {
                attempt++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
        }
        
        // Last attempt
        return await client.SendAsync(request, cancellationToken);
    }
}