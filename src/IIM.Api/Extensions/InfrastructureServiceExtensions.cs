using IIM.Application.Services;
using IIM.Infrastructure.Data;
using IIM.Infrastructure.Platform;
using IIM.Infrastructure.Services;
using IIM.Infrastructure.Storage;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Api.Extensions
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
           

            // Register Core Infrastructure Services
            services.AddScoped<IDeduplicationService, DeduplicationService>();

            // Register Storage Providers
            services.AddSingleton(sp =>
                configuration.GetSection("Storage:S3Storage").Get<S3StorageConfiguration>() ?? new S3StorageConfiguration());

            services.AddSingleton<IObjectStorageProvider, SeaweedFSStorageProvider>();

            // Register High-Level Managers/Services
            services.AddScoped<IManagedFileManager, FileManager>();

            // Register Application Services
            services.AddScoped<QuarantineService>();
            services.AddScoped<VirtualFileSystemService>();

            // Add missing services
            services.AddScoped<IAuditService, AuditService>();

            // Register IWslManager based on deployment mode
            var deploymentMode = configuration.GetValue<string>("Deployment:Mode", "Standalone");
            if (deploymentMode == "Docker" || Environment.OSVersion.Platform == PlatformID.Unix)
            {
                services.AddSingleton<IWslManager, DockerManager>();
            }
            else
            {
                // Keep existing WSL implementation for Windows
                services.AddSingleton<IWslManager, WslManager>();
            }

            return services;
        }
    }
}