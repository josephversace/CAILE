using IIM.Core.Mediator;
using IIM.Application.Behaviors;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using IIM.Infrastructure.Data.Services;

namespace IIM.Api.Extensions
{
    /// <summary>
    /// Command handling registration using existing SimpleMediator
    /// </summary>
    public static class CommandHandlingExtensions
    {
        public static IServiceCollection AddCommandHandling(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add SimpleMediator (your custom implementation)
            services.AddSimpleMediator(
                typeof(IIM.Application.Commands.Investigation.CreateSessionCommand).Assembly,  // Application assembly
                typeof(IIM.Core.AI.IModelOrchestrator).Assembly  // Core assembly
            );

            // Register pipeline behaviors (audit is already here)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));  // Audit here!

            // Memory cache for caching behavior
            services.AddMemoryCache();

            return services;
        }

        public static IServiceCollection AddAuditServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register the audit logger (already in Shared)
            services.AddScoped<IAuditLogger, SqliteAuditLogger>();  // Or wherever your implementation is

            return services;
        }
    }
}