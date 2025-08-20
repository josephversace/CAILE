using IIM.Core.Collections;
using IIM.Core.HealthChecks;
using IIM.Core.Inference;
using IIM.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IIM.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering inference pipeline services
    /// </summary>
    public static class InferencePipelineServiceExtensions
    {
        /// <summary>
        /// Adds the production-ready inference pipeline with all supporting services
        /// </summary>
        public static IServiceCollection AddInferencePipeline(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register configuration
            services.Configure<InferencePipelineConfiguration>(
                configuration.GetSection("InferencePipeline"));

            services.Configure<ModelMetadataConfiguration>(
                configuration.GetSection("ModelMetadata"));

            // Register model metadata service
            services.AddSingleton<IModelMetadataService, ModelMetadataService>();

            // Register the inference pipeline
            services.AddSingleton<IInferencePipeline, InferencePipeline>();

            // Register health checks
            services.AddHealthChecks()
                .AddCheck<InferencePipelineHealthCheck>("inference_pipeline");

            return services;
        }
    }
}