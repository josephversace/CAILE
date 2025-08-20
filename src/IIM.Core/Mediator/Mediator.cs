// ============================================
// File: src/IIM.Core/Mediator/IMediator.cs
// Custom lightweight mediator implementation
// ============================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Mediator
{
    /// <summary>
    /// Marker interface for requests that return a response
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    public interface IRequest<out TResponse>
    {
    }

    /// <summary>
    /// Marker interface for commands that don't return a value
    /// </summary>
    public interface ICommand : IRequest<Unit>
    {
    }

    /// <summary>
    /// Marker interface for queries that return data
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    public interface IQuery<out TResponse> : IRequest<TResponse>
    {
    }

    /// <summary>
    /// Marker interface for notifications (one-to-many)
    /// </summary>
    public interface INotification
    {
    }

    /// <summary>
    /// Handler interface for requests
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Handles the request
        /// </summary>
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handler interface for notifications
    /// </summary>
    /// <typeparam name="TNotification">Notification type</typeparam>
    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// Handles the notification
        /// </summary>
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Pipeline behavior for pre/post processing
    /// </summary>
    public interface IPipelineBehavior<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Pipeline handler. Wraps the inner handler
        /// </summary>
        Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Delegate for next handler in pipeline
    /// </summary>
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

    /// <summary>
    /// Unit type for commands without return value
    /// </summary>
    public readonly struct Unit : IEquatable<Unit>
    {
        /// <summary>
        /// Default unit value
        /// </summary>
        public static readonly Unit Value = new();

        /// <summary>
        /// Task with Unit result
        /// </summary>
        public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

        public bool Equals(Unit other) => true;
        public override bool Equals(object? obj) => obj is Unit;
        public override int GetHashCode() => 0;
        public static bool operator ==(Unit left, Unit right) => true;
        public static bool operator !=(Unit left, Unit right) => false;
    }

    /// <summary>
    /// Mediator interface for sending requests and notifications
    /// </summary>
    public interface IMediator
    {
        /// <summary>
        /// Send a request to a single handler
        /// </summary>
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publish a notification to multiple handlers
        /// </summary>
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }

    /// <summary>
    /// Default mediator implementation
    /// </summary>
    public class SimpleMediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SimpleMediator> _logger;
        private readonly Dictionary<Type, Type> _handlerCache = new();
        private readonly Dictionary<Type, List<Type>> _notificationHandlerCache = new();

        /// <summary>
        /// Initializes the mediator
        /// </summary>
        public SimpleMediator(IServiceProvider serviceProvider, ILogger<SimpleMediator> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            BuildHandlerCache();
        }

        /// <summary>
        /// Build cache of handlers for performance
        /// </summary>
        private void BuildHandlerCache()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName?.StartsWith("IIM") == true);

            foreach (var assembly in assemblies)
            {
                var handlerTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType);

                foreach (var handlerType in handlerTypes)
                {
                    // Cache request handlers
                    var requestInterfaces = handlerType.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

                    foreach (var requestInterface in requestInterfaces)
                    {
                        var requestType = requestInterface.GetGenericArguments()[0];
                        _handlerCache[requestType] = handlerType;
                    }

                    // Cache notification handlers
                    var notificationInterfaces = handlerType.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

                    foreach (var notificationInterface in notificationInterfaces)
                    {
                        var notificationType = notificationInterface.GetGenericArguments()[0];
                        if (!_notificationHandlerCache.ContainsKey(notificationType))
                        {
                            _notificationHandlerCache[notificationType] = new List<Type>();
                        }
                        _notificationHandlerCache[notificationType].Add(handlerType);
                    }
                }
            }
        }

        /// <summary>
        /// Send a request to its handler
        /// </summary>
        public async Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            _logger.LogDebug("Processing request {RequestType}", requestType.Name);

            try
            {
                // Get handler type from cache or resolve dynamically
                if (!_handlerCache.TryGetValue(requestType, out var handlerType))
                {
                    var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
                    var handler = _serviceProvider.GetService(handlerInterfaceType);

                    if (handler == null)
                    {
                        throw new InvalidOperationException($"No handler registered for {requestType.Name}");
                    }

                    // Execute handler directly if not cached
                    var method = handler.GetType().GetMethod("Handle");
                    var task = (Task<TResponse>)method!.Invoke(handler, new object[] { request, cancellationToken })!;
                    return await task;
                }

                // Create handler instance
                var handlerInstance = ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);

                // Get pipeline behaviors
                var behaviors = GetPipelineBehaviors<TResponse>(requestType);

                // Build pipeline
                RequestHandlerDelegate<TResponse> handlerDelegate = async () =>
                {
                    var method = handlerType.GetMethod("Handle");
                    var task = (Task<TResponse>)method!.Invoke(handlerInstance, new object[] { request, cancellationToken })!;
                    return await task;
                };

                // Execute pipeline in reverse order
                foreach (var behavior in behaviors.AsEnumerable().Reverse())
                {
                    var currentDelegate = handlerDelegate;
                    handlerDelegate = () => behavior.Handle(request, currentDelegate, cancellationToken);
                }

                return await handlerDelegate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request {RequestType}", requestType.Name);
                throw;
            }
        }

        /// <summary>
        /// Publish a notification to all handlers
        /// </summary>
        public async Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            var notificationType = notification.GetType();
            _logger.LogDebug("Publishing notification {NotificationType}", notificationType.Name);

            if (!_notificationHandlerCache.TryGetValue(notificationType, out var handlerTypes))
            {
                _logger.LogDebug("No handlers found for notification {NotificationType}", notificationType.Name);
                return;
            }

            var tasks = new List<Task>();

            foreach (var handlerType in handlerTypes)
            {
                try
                {
                    var handler = ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);
                    var method = handlerType.GetMethod("Handle");
                    var task = (Task)method!.Invoke(handler, new object[] { notification, cancellationToken })!;
                    tasks.Add(task);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in notification handler {HandlerType}", handlerType.Name);
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Get pipeline behaviors for request type
        /// </summary>
        private List<IPipelineBehavior<IRequest<TResponse>, TResponse>> GetPipelineBehaviors<TResponse>(Type requestType)
        {
            var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
            var behaviors = _serviceProvider.GetServices(behaviorType);

            return behaviors
                .Cast<IPipelineBehavior<IRequest<TResponse>, TResponse>>()
                .OrderBy(b => b.GetType().GetCustomAttribute<PipelineOrderAttribute>()?.Order ?? 0)
                .ToList();
        }
    }

    /// <summary>
    /// Attribute to control pipeline execution order
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PipelineOrderAttribute : Attribute
    {
        /// <summary>
        /// Order of execution (lower runs first)
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Initialize with order
        /// </summary>
        public PipelineOrderAttribute(int order)
        {
            Order = order;
        }
    }

    /// <summary>
    /// Extension methods for service registration
    /// </summary>
    public static class MediatorServiceExtensions
    {
        /// <summary>
        /// Register mediator and scan for handlers
        /// </summary>
        public static IServiceCollection AddSimpleMediator(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            // Register mediator
            services.AddSingleton<IMediator, SimpleMediator>();

            // Scan assemblies for handlers
            var assembliesToScan = assemblies.Any()
                ? assemblies
                : AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.FullName?.StartsWith("IIM") == true)
                    .ToArray();

            foreach (var assembly in assembliesToScan)
            {
                // Register request handlers
                var requestHandlers = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType)
                    .SelectMany(t => t.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                        .Select(i => new { HandlerType = t, Interface = i }));

                foreach (var handler in requestHandlers)
                {
                    services.AddTransient(handler.Interface, handler.HandlerType);
                }

                // Register notification handlers
                var notificationHandlers = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType)
                    .SelectMany(t => t.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                        .Select(i => new { HandlerType = t, Interface = i }));

                foreach (var handler in notificationHandlers)
                {
                    services.AddTransient(handler.Interface, handler.HandlerType);
                }

                // Register pipeline behaviors
                var behaviors = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType)
                    .Where(t => t.GetInterfaces().Any(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)));

                foreach (var behavior in behaviors)
                {
                    var interfaces = behavior.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

                    foreach (var behaviorInterface in interfaces)
                    {
                        services.AddTransient(behaviorInterface, behavior);
                    }
                }
            }

            return services;
        }
    }
}