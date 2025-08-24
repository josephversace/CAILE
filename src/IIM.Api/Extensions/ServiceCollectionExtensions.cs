using IIM.Api.Configuration;
using IIM.Core.Configuration;
using IIM.Core.Inference;
using IIM.Core.Services;
using IIM.Infrastructure.Storage;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Load deployment configuration first
            var deploymentConfig = new DeploymentConfiguration();
            configuration.GetSection("Deployment").Bind(deploymentConfig);
            services.AddSingleton(deploymentConfig);

            // Add databases (Scoped by default with EF Core)
            services.AddIIMDatabases(configuration);

            // Add memory cache (Singleton)
            services.AddMemoryCache();

            // Add configuration objects
            services.AddConfiguration(configuration);

            // Add HTTP context accessor (Singleton) - only add once
            services.AddHttpContextAccessor();

            // Add SignalR for real-time updates
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = deploymentConfig.IsDevelopment;
            });

            // Add services in dependency order
            services.AddHttpClients(configuration);
            services.AddInfrastructureServices(configuration, deploymentConfig);
            services.AddCoreServices(configuration, deploymentConfig);
            services.AddApplicationServices(configuration, deploymentConfig);
            services.AddBackgroundServices(configuration, deploymentConfig);

            // Add authentication based on deployment mode
            if (deploymentConfig.Mode != DeploymentMode.Standalone || deploymentConfig.RequireAuth)
            {
                services.AddAuthenticationServices(configuration, deploymentConfig);
            }

            // Add admin pages for server mode
            if (deploymentConfig.Mode == DeploymentMode.Server)
            {
                services.AddRazorPages()
                    .AddRazorPagesOptions(options =>
                    {
                        options.RootDirectory = "/Areas/Admin/Pages";
                    });
            }

            return services;
        }

        private static IServiceCollection AddConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // All configuration objects as Singleton (they don't change)
            services.Configure<QdrantConfiguration>(configuration.GetSection("Qdrant"));
            services.Configure<InferenceConfiguration>(configuration.GetSection("Inference"));
            services.Configure<WslConfiguration>(configuration.GetSection("Wsl"));
            services.Configure<EvidenceConfiguration>(configuration.GetSection("Evidence"));
            services.Configure<AuditConfiguration>(configuration.GetSection("Audit"));
            services.Configure<ModelTemplateConfiguration>(configuration.GetSection("ModelTemplates"));
            services.Configure<InferencePipelineConfiguration>(configuration.GetSection("InferencePipeline"));
            services.Configure<ModelMetadataConfiguration>(configuration.GetSection("ModelMetadata"));
            services.Configure<MinIOConfiguration>(configuration.GetSection("Storage:MinIO"));

            return services;
        }
    }
}
