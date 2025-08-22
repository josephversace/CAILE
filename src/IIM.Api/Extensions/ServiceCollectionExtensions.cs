using IIM.Api.Configuration;
using IIM.Core.Configuration;

using IIM.Infrastructure.Storage;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IIM.Api.Extensions
{
    /// <summary>
    /// Main service registration extension methods for the IIM API
    /// Orchestrates all service registration based on deployment mode
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all IIM API services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Load deployment configuration first
            var deploymentConfig = new DeploymentConfiguration();
            configuration.GetSection("Deployment").Bind(deploymentConfig);
            services.AddSingleton(deploymentConfig);

            services.AddIIMDatabases(configuration);
   
            services.AddMemoryCache();

            // Add configuration objects
            services.AddConfiguration(configuration);
            
            // Add MediatR for audit-critical operations
 
            
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
        
        /// <summary>
        /// Adds configuration objects from appsettings
        /// </summary>
        private static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<QdrantConfiguration>(configuration.GetSection("Qdrant"));
            services.Configure<InferenceConfiguration>(configuration.GetSection("Inference"));
            services.Configure<WslConfiguration>(configuration.GetSection("Wsl"));
            services.Configure<EvidenceConfiguration>(configuration.GetSection("Evidence"));
           services.Configure<AuditConfiguration>(configuration.GetSection("Audit"));
            services.Configure<ModelTemplateConfiguration>(configuration.GetSection("ModelTemplates"));
            
            return services;
        }
    }
}
