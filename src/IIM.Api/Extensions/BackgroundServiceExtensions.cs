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
            // Only add background services if not in client mode
            if (deployment.Mode == DeploymentMode.Client)
            {
                return services;
            }

      
            // Note: EvidenceIntegrityMonitor is already defined in Program.cs
            // We'll move it here later, for now comment it out
           // services.AddHostedService<EvidenceIntegrityMonitor>();

            // WSL Service Orchestration (Windows only)
            if (OperatingSystem.IsWindows())
            {
                //services.AddHostedService<WslServiceOrchestrator>();
            }

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