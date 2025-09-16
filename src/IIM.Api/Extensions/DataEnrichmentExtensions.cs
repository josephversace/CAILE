using IIM.Application.AI.DataEnrichment;
using IIM.Application.AI.DataEnrichment.Helpers;
using IIM.Application.AI.DataEnrichment.Services;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;


namespace IIM.Api.Extensions
{
    /// <summary>
    /// Extension methods for registering data enrichment services
    /// </summary>
    public static class DataEnrichmentServiceExtensions
    {
        public static IServiceCollection AddDataEnrichmentServices(this IServiceCollection services)
        {
            // Data Router Core Services (Industry Agnostic)
            // ========================================

            // Data reasoning and enrichment service
            services.AddScoped<IDataReasoningService, DataEnrichmentOrchestrator>();

   

            // Helper services
            services.AddScoped<AIPromptBuilder>();
            services.AddScoped<ConfidenceCalculator>();
            services.AddScoped<MetadataExtractor>();

            // Text extraction services
            services.AddScoped<ITextExtractionService, TextExtractionService>();


            // Data enrichment services (these are registered in CoreServiceExtensions but included here for completeness)
            services.AddScoped<IFileClassificationService, FileClassificationService>();
            services.AddScoped<IDataQueryService, DataQueryService>();
            services.AddScoped<IGovernanceSuggestionService, GovernanceSuggestionService>();
            services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();

            return services;
        }
    }
}