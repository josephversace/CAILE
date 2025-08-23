using IIM.Api.Configuration;
using IIM.Core.AI;
using IIM.Core.Configuration;
using IIM.Core.Inference;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Infrastructure.Storage;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Options;

namespace IIM.Api.Extensions
{
    public static class CoreServiceExtensions
    {
        public static IServiceCollection AddCoreServices(
            this IServiceCollection services,
            IConfiguration configuration,
            DeploymentConfiguration deployment)
        {
            // ========================================
            // AI/Model Services (Singleton for performance)
            // ========================================

            // Model Orchestration (Singleton - manages loaded models)
            services.AddSingleton<IModelOrchestrator>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultModelOrchestrator>>();
                var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                return new DefaultModelOrchestrator(logger, storageConfig);
            });

            // Model Metadata (Singleton - cached metadata)
            services.AddSingleton<IModelMetadataService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ModelMetadataService>>();
                var config = sp.GetRequiredService<IOptions<ModelMetadataConfiguration>>();
                return new ModelMetadataService(logger, config);
            });

            // Inference Pipeline (Singleton - manages GPU/CPU resources)
            services.AddSingleton<IInferencePipeline>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<InferencePipeline>>();
                var orchestrator = sp.GetRequiredService<IModelOrchestrator>();
                var metadataService = sp.GetRequiredService<IModelMetadataService>();
                var config = sp.GetRequiredService<IOptions<InferencePipelineConfiguration>>();
                var mediator = sp.GetService<IMediator>(); // Optional mediator

                return new InferencePipeline(logger, orchestrator, metadataService, config, mediator);
            });

            // ========================================
            // Session/User Services (Scoped per request)
            // ========================================

            // Session Management (Scoped - per request)
            services.AddScoped<ISessionService, SessionService>();

            // User Context (Scoped - per request)
            services.AddScoped<IUserContext, UserContextService>();

            // Configuration Service (Scoped - uses HttpContext)
            services.AddScoped<IConfigurationService, ConfigurationService>();

            // ========================================
            // Template Services (Scoped - uses session)
            // ========================================

            // Model Configuration Templates (Scoped - uses SessionService)
            services.AddScoped<IModelConfigurationTemplateService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ModelConfigurationTemplateService>>();
                var storageConfig = sp.GetRequiredService<StorageConfiguration>();
                var orchestrator = sp.GetRequiredService<IModelOrchestrator>();
                var sessionService = sp.GetRequiredService<ISessionService>();

                return new ModelConfigurationTemplateService(
                    logger,
                    storageConfig,
                    orchestrator,
                    sessionService);
            });

            // Reasoning Service (Scoped - uses session)
            services.AddScoped<IReasoningService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<SemanticKernelOrchestrator>>();
                var modelOrchestrator = sp.GetRequiredService<IModelOrchestrator>();
                var sessionService = sp.GetRequiredService<ISessionService>();
                var templateService = sp.GetRequiredService<IModelConfigurationTemplateService>();

                return new SemanticKernelOrchestrator(
                    logger,
                    modelOrchestrator,
                    sessionService,
                    templateService);
            });

            // ========================================
            // Audit Services (Scoped for request tracking)
            // ========================================

            // Audit Logger (Scoped - tracks per request)
            services.AddScoped<IAuditLogger, SqliteAuditLogger>();

            return services;
        }
    }
}