using IIM.Api.Configuration;
using IIM.Core.Configuration;
using IIM.Core.RAG;
using IIM.Core.Services;
using IIM.Core.Storage;
using IIM.Infrastructure.Embeddings;
using IIM.Infrastructure.Platform;
using IIM.Infrastructure.Storage;
using IIM.Infrastructure.VectorStore;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Options;

namespace IIM.Api.Extensions
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration,
            DeploymentConfiguration deployment)
        {
            // ========================================
            // Platform Services (Singleton)
            // ========================================

            // WSL Management (Windows only, Singleton)
            if (deployment.Mode != DeploymentMode.Client && OperatingSystem.IsWindows())
            {
                services.AddSingleton<IWslManager, WslManager>();
                services.AddSingleton<IWslServiceOrchestrator, WslServiceOrchestrator>();
            }

            // Storage Configuration (Singleton - doesn't change)
            services.AddSingleton<StorageConfiguration>(sp =>
            {
                var basePath = configuration["Storage:BasePath"] ??
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIM");

                var config = new StorageConfiguration { BasePath = basePath };

                if (deployment.Mode != DeploymentMode.Client)
                {
                    config.EnsureDirectoriesExist();
                }

                return config;
            });

            // ========================================
            // Storage Services (Scoped for transaction support)
            // ========================================

            // Deduplication Service (Scoped - uses DB context)
            services.AddScoped<IDeduplicationService, DeduplicationService>();

            // MinIO Object Storage (Scoped - transaction boundary)
            services.AddScoped<IMinIOStorageService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<MinIOStorageService>>();
                var config = sp.GetRequiredService<IOptions<MinIOConfiguration>>();
                var deduplicationService = sp.GetRequiredService<IDeduplicationService>();
                return new MinIOStorageService(logger, config, deduplicationService);
            });

            // ========================================
            // AI Infrastructure (Mixed lifetimes)
            // ========================================

            // Vector Store (Singleton for connection pooling)
            services.AddSingleton<IQdrantService>(sp =>
            {
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetRequiredService<ILogger<QdrantService>>();
                var useInMemory = configuration.GetValue<bool>("Development:UseInMemoryQdrant", false);

                if (useInMemory && deployment.IsStandalone)
                {
                    var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                    var inMemoryLogger = sp.GetRequiredService<ILogger<InMemoryQdrantService>>();
                    return new InMemoryQdrantService(inMemoryLogger, storageConfig);
                }

                return new QdrantService(httpFactory, logger);
            });

            // Embedding Service (Singleton for performance)
            services.AddSingleton<IEmbeddingService>(sp =>
            {
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetRequiredService<ILogger<RemoteEmbeddingService>>();
                return new RemoteEmbeddingService(httpFactory, logger);
            });

            return services;
        }
    }
}