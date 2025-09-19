using Hangfire;
using Hangfire.PostgreSql;
using IIM.Api.Configuration;
using IIM.Api.Services;
using IIM.Infrastructure.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Api.Extensions
{
    public static class BackgroundServiceExtensions
    {
        public static IServiceCollection AddBackgroundServices(
            this IServiceCollection services,
            IConfiguration configuration,
            DeploymentConfiguration deployment)
        {
            // Add Hangfire
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    // Use your existing PostgreSQL connection
                    options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
                }));

            // Add Hangfire server
            services.AddHangfireServer(options =>
            {
                options.ServerName = $"DataRouter-{Environment.MachineName}";
                options.WorkerCount = Environment.ProcessorCount * 2;
                options.Queues = new[]
                {
                "critical",      // High priority processing
                "enrichment",    // AI analysis
                "routing",       // File movement
                "background"     // Low priority
            };
            });


            // Infrastructure Health Monitoring
            services.AddHostedService<InfrastructureMonitor>();

            // Model Preload Service - comment out for now (doesn't exist yet)
            // if (deployment.Mode == DeploymentMode.Server)
            // {
            //     services.AddHostedService<ModelPreloadService>();
            // }

            return services;
        }
    }
}