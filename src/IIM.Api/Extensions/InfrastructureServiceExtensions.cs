using IIM.Application.Services;
using IIM.Infrastructure.Data;
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
            services.AddSingleton<IDeduplicationService, DeduplicationService>();

            // Register Storage Providers based on the new abstractions
            // This reads the S3Storage section from your appsettings.json
            services.AddSingleton(sp =>
                configuration.GetSection("S3Storage").Get<S3StorageConfiguration>() ?? new S3StorageConfiguration());

            // This is our single, agnostic storage provider
            services.AddSingleton<IObjectStorageProvider, SeaweedFSStorageProvider>();

            // Register Repositories (Data Access Layer)
            services.AddScoped<IAuditRepository, EfAuditRepository>();
            services.AddScoped<IWorkspaceProvider, PostgresWorkspaceProvider>();
            services.AddScoped<IGovernanceRepository, EfGovernanceRepository>();
           /// services.AddScoped<IWorkspaceManager, EfWorkspaceManager>(); // Assuming you have an EF implementation
           // services.AddScoped<ISessionRepository, EfSessionRepository>(); // Assuming you have an EF implementation for sessions

            // Register High-Level Managers/Services
            // IManagedFileManager is the primary orchestrator for file operations.
            services.AddScoped<IManagedFileManager, FileManager>();

            // Register Application Services
            services.AddScoped<QuarantineService>();
            services.AddScoped<VirtualFileSystemService>();

            return services;
        }
    }
}

