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
            services.AddScoped<IInvestigationService, InvestigationService>();

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
                var logger = sp.GetRequiredService<ILogger<JsonCaseManager>>();
                var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                return new JsonCaseManager(logger, storageConfig);
            });

            // Inference Service (high-level)
            services.AddScoped<IInferenceService, InferenceService>();

            // Export Services
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddScoped<IWordService, WordService>();
            services.AddScoped<IExcelService, ExcelService>();
            services.AddScoped<IVisualizationService, VisualizationService>();

            // File Services
            services.AddScoped<IFileService, FileService>();
          

            return services;
        }
    }
}