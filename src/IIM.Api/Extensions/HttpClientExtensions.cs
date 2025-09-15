using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using IIM.Api.Configuration;

namespace IIM.Api.Extensions
{
    /// <summary>
    /// HTTP client registration for external service communication
    /// </summary>
    public static class HttpClientExtensions
    {
        public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            // Qdrant vector database client
            services.AddHttpClient("qdrant", client =>
            {
                var baseUrl = configuration["Qdrant:BaseUrl"] ?? "http://localhost:6333";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            
            // Embedding service client
            services.AddHttpClient("embed", client =>
            {
                var baseUrl = configuration["EmbedService:BaseUrl"] ?? "http://localhost:8081";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
         
            // WSL management client
            services.AddHttpClient("wsl", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            
            // WSL services client
            services.AddHttpClient("wsl-services", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            
            return services;
        }
    }
}
