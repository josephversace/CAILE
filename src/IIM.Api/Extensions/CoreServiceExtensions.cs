using IIM.Api.Configuration;
using IIM.Application.AI;
using IIM.Application.Inference;
using IIM.Application.Investigation;
using IIM.Core.AI;
using IIM.Core.Configuration;

using IIM.Shared.Mediator;
using IIM.Core.Services;
using IIM.Core.Templates;
using IIM.Infrastructure.AI.DirectML;
using IIM.Infrastructure.AI.LlamaSharp;
using IIM.Infrastructure.AI.OnnxRuntime;
using IIM.Infrastructure.Data;
using IIM.Infrastructure.Models;
using IIM.Infrastructure.Platform;
using IIM.Infrastructure.Storage;
using IIM.Infrastructure.Templates;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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

            // DirectML Device Manager as Singleton
            services.AddSingleton<IDirectMLDeviceManager, DirectMLDeviceManager>();

            // ONNX Runtime Manager as Singleton
            services.AddSingleton<IOnnxRuntimeManager, OnnxRuntimeManager>();

            //LlamaSharp Runtime Manager as Singleton
            services.AddSingleton<ILlamaSharpManager, LlamaSharpManager>();

            // Model Orchestration (Singleton - manages loaded models)
            services.AddSingleton<IModelOrchestrator>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultModelOrchestrator>>();
                var storageConfig = sp.GetRequiredService<IStorageConfiguration>();
                return new DefaultModelOrchestrator(logger, storageConfig);
            });

            // Model Metadata (Singleton - cached metadata)
            services.AddSingleton<IModelConfigurationService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ModelConfigurationService>>();
                var config = sp.GetRequiredService<IOptions<ModelConfigurationConfiguration>>();
                return new ModelConfigurationService(logger, config);
            });

            // Inference Pipeline (Singleton - manages GPU/CPU resources)
            services.AddSingleton<IInferencePipeline>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<InferencePipeline>>();
                var orchestrator = sp.GetRequiredService<IModelOrchestrator>();
                var ModelConfiguration = sp.GetRequiredService<IModelConfigurationService>();
                var config = sp.GetRequiredService<IOptions<InferencePipelineConfiguration>>();
                var modelParams = sp.GetRequiredService<IModelParameterSetRepository>();
                var mediator = sp.GetService<IMediator>();
                var onnxManager = sp.GetRequiredService<IOnnxRuntimeManager>();
                var llamaManager = sp.GetRequiredService<ILlamaSharpManager>();
                var directMLDeviceManager = sp.GetRequiredService<IDirectMLDeviceManager>();
                return new InferencePipeline(logger, orchestrator, ModelConfiguration, modelParams,  config, onnxManager, llamaManager, directMLDeviceManager, mediator);
            });


    

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
         

                return new ModelConfigurationTemplateService(
                    logger,
                    storageConfig,
                    orchestrator);
            });

            // Reasoning Service (Scoped - uses session)
            services.AddScoped<IReasoningService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<SemanticKernelOrchestrator>>();
                var modelOrchestrator = sp.GetRequiredService<IModelOrchestrator>();
          
                var templateService = sp.GetRequiredService<IModelConfigurationTemplateService>();

                return new SemanticKernelOrchestrator(
                    logger,
                    modelOrchestrator,
                    templateService);
            });

            // ========================================
            // Audit Services (Scoped for request tracking)
            // ========================================

            // Audit Logger (Scoped - tracks per request)
            services.AddScoped<IAuditService, SqliteAuditLogger>();

            return services;
        }
    }
}