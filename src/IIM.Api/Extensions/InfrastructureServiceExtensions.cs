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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;  // Add this
using System;
using System.IO;

namespace IIM.Api.Extensions
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration,
            DeploymentConfiguration deployment)
        {
            // WSL Management (not needed for client mode)
            if (deployment.Mode != DeploymentMode.Client)
            {
                services.AddSingleton<IWslManager, WslManager>();
                services.AddSingleton<IWslServiceOrchestrator, WslServiceOrchestrator>();
            }

            // Storage Configuration
            services.AddScoped<StorageConfiguration>(sp =>
            {
                var basePath = configuration["Storage:BasePath"] ??
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIM");

                var config = new StorageConfiguration
                {
                    BasePath = basePath
                };

                // Only create directories if not in client mode
                if (deployment.Mode != DeploymentMode.Client)
                {
                    config.EnsureDirectoriesExist();
                }

                return config;
            });

            // Configure MinIO settings
            services.Configure<MinIOConfiguration>(configuration.GetSection("Storage:MinIO"));

            // Deduplication Service (register before MinIO since it's a dependency)
            services.AddScoped<IDeduplicationService, DeduplicationService>();

            // MinIO Object Storage - Fix: provide all required parameters
            services.AddScoped<IMinIOStorageService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<MinIOStorageService>>();
                var config = sp.GetRequiredService<IOptions<MinIOConfiguration>>();
                var deduplicationService = sp.GetRequiredService<IDeduplicationService>();

                return new MinIOStorageService(logger, config, deduplicationService);
            });

            // Vector Store
            services.AddScoped<IQdrantService>(sp =>
            {
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetRequiredService<ILogger<QdrantService>>();

                // Use in-memory for development/testing
                var useInMemory = configuration.GetValue<bool>("Development:UseInMemoryQdrant", false);

                if (useInMemory && deployment.IsStandalone)
                {
                    var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                    var inMemoryLogger = sp.GetRequiredService<ILogger<InMemoryQdrantService>>();
                    return new InMemoryQdrantService(inMemoryLogger, storageConfig);
                }

                return new QdrantService(httpFactory, logger);
            });

            // Embedding Service
            services.AddScoped<IEmbeddingService>(sp =>
            {
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetRequiredService<ILogger<RemoteEmbeddingService>>();
                return new RemoteEmbeddingService(httpFactory, logger);
            });

            return services;
        }
    }
}