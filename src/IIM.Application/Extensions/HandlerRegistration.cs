using IIM.Application.Handlers;
using IIM.Core.Inference;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Application.Extensions
{
    public static class HandlerRegistrationExtensions
    {
        /// <summary>
        /// Registers all inference notification handlers
        /// </summary>
        public static IServiceCollection AddInferenceHandlers(this IServiceCollection services)
        {
            // Register supporting services
            services.AddSingleton<IProgressTracker, InMemoryProgressTracker>();
            services.AddSingleton<IMetricsCollector, InMemoryMetricsCollector>();
            services.AddSingleton<IErrorTracker, InMemoryErrorTracker>();

            // Register notification handlers
            services.AddTransient<INotificationHandler<InferenceQueuedNotification>, InferenceQueuedHandler>();
            services.AddTransient<INotificationHandler<InferenceStartedNotification>, InferenceStartedHandler>();
            services.AddTransient<INotificationHandler<InferenceCompletedNotification>, InferenceCompletedHandler>();
            services.AddTransient<INotificationHandler<InferenceFailedNotification>, InferenceFailedHandler>();

            // Register audit handler for all notifications
            services.AddTransient<INotificationHandler<InferenceQueuedNotification>, InferenceAuditHandler>();
            services.AddTransient<INotificationHandler<InferenceCompletedNotification>, InferenceAuditHandler>();
            services.AddTransient<INotificationHandler<InferenceFailedNotification>, InferenceAuditHandler>();

            return services;
        }
    }
}