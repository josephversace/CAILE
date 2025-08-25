using IIM.Application.Inference;
using IIM.Core.Collections;
using IIM.Core.Services;
using IIM.Infrastructure.Models;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IIM.Api.Extensions
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

            services.Configure<ModelConfigurationConfiguration>(
                configuration.GetSection("ModelConfiguration"));

            // Register model metadata service
            services.AddSingleton<IModelConfigurationService, ModelConfigurationService>();

            // Register the inference pipeline
            services.AddSingleton<IInferencePipeline, InferencePipeline>();

            // Register health checks
            services.AddHealthChecks()
                .AddCheck<InferencePipelineHealthCheck>("inference_pipeline");

            return services;
        }
    }
}