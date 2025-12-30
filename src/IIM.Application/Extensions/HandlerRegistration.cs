
using IIM.Application.Inference;
using IIM.Application.Services;
using IIM.Shared.Mediator;
using IIM.Core.Services;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Application.Extensions
{
    /// <summary>
    /// Provides extension methods for registering inference pipeline notification handlers
    /// and supporting services with the dependency injection container.
    /// </summary>
    public static class HandlerRegistrationExtensions
    {
        /// <summary>
        /// Registers all inference-related notification handlers and supporting services 
        /// with the specified <see cref="IServiceCollection"/>. 
        /// 
        /// This enables the pipeline to publish notifications (such as when an inference 
        /// request is queued, started, completed, or failed), which can then be observed 
        /// for metrics, progress tracking, and auditing.
        /// 
        /// To enable these handlers, call this method during application startup (e.g., 
        /// in <c>Program.cs</c> or <c>Startup.cs</c>).
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the services to.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.
        /// </returns>
        /// <remarks>
        /// Registers the following:
        /// <list type="bullet">
        /// <item><description>
        /// Singleton trackers: <see cref="IProgressTracker"/>, <see cref="IMetricsCollector"/>, <see cref="IErrorTracker"/>
        /// </description></item>
        /// <item><description>
        /// Transient notification handlers for pipeline events:
        /// <see cref="InferenceQueuedHandler"/>, <see cref="InferenceStartedHandler"/>, 
        /// <see cref="InferenceCompletedHandler"/>, <see cref="InferenceFailedHandler"/>
        /// </description></item>
        /// <item><description>
        /// <see cref="InferenceAuditHandler"/> is registered for queued, completed, and failed notifications for audit logging.
        /// </description></item>
        /// </list>
        /// </remarks>
   
    }
}
