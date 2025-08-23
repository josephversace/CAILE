using IIM.Application.Interfaces;
using IIM.Application.Services;
using IIM.Core.Services;
using IIM.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IIM.Api.Configuration;
using System;
using System.IO;
// Use fully qualified name to avoid ambiguity
using CoreEvidenceConfiguration = IIM.Core.Configuration.EvidenceConfiguration;
using IIM.Core.Configuration;

namespace IIM.Api.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            DeploymentConfiguration deployment)
        {
            // Investigation Service
     

            // Evidence Management - use Core.Security.EvidenceConfiguration
            services.AddScoped<IEvidenceManager>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<EvidenceManager>>();
                var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                var auditContext = sp.GetRequiredService<AuditDbContext>();

                // Use the Core.Security.EvidenceConfiguration explicitly
                var config = new CoreEvidenceConfiguration
                {
                    StorePath = storageConfig.EvidencePath,
                    EnableEncryption = configuration.GetValue<bool>("Evidence:EnableEncryption", false),
                    RequireDualControl = configuration.GetValue<bool>("Evidence:RequireDualControl", false),
                    MaxFileSizeMb = configuration.GetValue<int>("Evidence:MaxFileSizeMb", 10240)
                };

                return new EvidenceManager(logger, config, auditContext);
            });

            // Case Management
            services.AddScoped<ICaseManager>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<SqliteCaseManager>>();
                var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                return new SqliteCaseManager(logger, storageConfig);
            });

            // Inference Service (high-level)
            services.AddScoped<IInferenceService, InferenceService>();

            services.Configure<TemplateEngineOptions>(configuration.GetSection("TemplateEngine"));
            services.AddScoped<ITemplateEngine, TemplateEngine>();

            // Export Services
            services.AddExportServices();
      
            services.AddScoped<IVisualizationService, VisualizationService>();

       
            services.AddScoped<IInvestigationService, InvestigationService>();

            return services;
        }
    }
}