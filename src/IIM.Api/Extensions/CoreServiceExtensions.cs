using IIM.Api.Configuration;
using IIM.Core.AI;
using IIM.Core.Configuration;
using IIM.Core.Inference;
using IIM.Core.Services;
using IIM.Infrastructure.Storage;  
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            // Model Orchestration - use existing DefaultModelOrchestrator for all modes
            services.AddSingleton<IModelOrchestrator>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultModelOrchestrator>>();
                var storageConfig = sp.GetRequiredService<StorageConfiguration>();

                // Use DefaultModelOrchestrator for all modes
                // (TemplateBasedModelOrchestrator doesn't exist yet)
                return new DefaultModelOrchestrator(logger, storageConfig);
            });


            services.AddHttpContextAccessor(); // For ConfigurationService
            services.AddScoped<IConfigurationService, ConfigurationService>();
            services.AddScoped<IAuditLogger, SqliteAuditLogger>();

            // Remove IModelManager section - it doesn't exist
            // The model management is handled by IModelOrchestrator

            // Inference Pipeline
            services.AddSingleton<IInferencePipeline>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<InferencePipeline>>();
                var orchestrator = sp.GetRequiredService<IModelOrchestrator>();
                var metadataService = sp.GetRequiredService<IModelMetadataService>();
                var config = sp.GetRequiredService<IOptions<InferencePipelineConfiguration>>();

                return new InferencePipeline(logger, orchestrator, metadataService, config);
            });

            // Model Metadata Service
            services.AddScoped<IModelMetadataService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ModelMetadataService>>();
                var config = sp.GetRequiredService<IOptions<ModelMetadataConfiguration>>();
                return new ModelMetadataService(logger, config);
            });

            // Reasoning Service (Semantic Kernel)
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

            // Session Management
            services.AddScoped<ISessionService, SessionService>();

            // Model Configuration Templates
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

        

            return services;
        }
    }
}