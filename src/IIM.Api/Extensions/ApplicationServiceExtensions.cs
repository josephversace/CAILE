using IIM.Api.Configuration;
using IIM.Application.Interfaces;
using IIM.Application.Services;
using IIM.Core.Configuration;
using IIM.Core.Services;
using IIM.Infrastructure.Storage;

namespace IIM.Api.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            DeploymentConfiguration deployment)
        {
            // ========================================
            // Investigation Services (Scoped)
            // ========================================

            // Investigation Service (Scoped - per request)
            services.AddScoped<IInvestigationService, InvestigationService>();

            // Evidence Manager (Scoped - uses DB context)
            services.AddScoped<IEvidenceManager>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<EvidenceManager>>();
                var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                var auditContext = sp.GetRequiredService<AuditDbContext>();

                var config = new EvidenceConfiguration
                {
                    StorePath = storageConfig.EvidencePath,
                    EnableEncryption = configuration.GetValue<bool>("Evidence:EnableEncryption", false),
                    RequireDualControl = configuration.GetValue<bool>("Evidence:RequireDualControl", false),
                    MaxFileSizeMb = configuration.GetValue<int>("Evidence:MaxFileSizeMb", 10240)
                };

                return new EvidenceManager(logger, config, auditContext);
            });

            // Case Manager (Scoped - uses DB)
            services.AddScoped<ICaseManager>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<SqliteCaseManager>>();
                var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                return new SqliteCaseManager(logger, storageConfig);
            });

            // ========================================
            // Processing Services (Scoped)
            // ========================================

            // Inference Service (Scoped - high-level operations)
            services.AddScoped<IInferenceService, InferenceService>();

            // Template Engine (Scoped)
            services.Configure<TemplateEngineOptions>(configuration.GetSection("TemplateEngine"));
            services.AddScoped<ITemplateEngine, TemplateEngine>();

            // Visualization Service (Scoped)
            services.AddScoped<IVisualizationService, VisualizationService>();

            // Export Services
            services.AddExportServices();

            return services;
        }
    }
}