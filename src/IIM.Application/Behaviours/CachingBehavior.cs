using IIM.Core.Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that caches query results
    /// </summary>
    [PipelineOrder(4)]
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes the caching behavior
        /// </summary>
        public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                MaxDepth = 3
            };
        }

        /// <summary>
        /// Caches query results for improved performance
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Only cache queries, not commands
            if (request is not IQuery<TResponse>)
            {
                return await next();
            }

            var requestName = typeof(TRequest).Name;
            var cacheKey = GenerateCacheKey(request);

            // Try to get from cache
            if (_cache.TryGetValue<TResponse>(cacheKey, out var cachedResponse))
            {
                _logger.LogDebug("Cache hit for {RequestName} with key {CacheKey}", requestName, cacheKey);
                return cachedResponse!;
            }

            _logger.LogDebug("Cache miss for {RequestName} with key {CacheKey}", requestName, cacheKey);

            // Execute the request
            var response = await next();

            // Cache the result
            var cacheOptions = GetCacheOptions(request);
            _cache.Set(cacheKey, response, cacheOptions);

            _logger.LogDebug("Cached result for {RequestName} with key {CacheKey}", requestName, cacheKey);

            return response;
        }

        /// <summary>
        /// Generates a cache key for the request
        /// </summary>
        private string GenerateCacheKey(TRequest request)
        {
            try
            {
                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(requestJson));
                return $"{typeof(TRequest).Name}:{Convert.ToBase64String(hash)}";
            }
            catch
            {
                // Fallback to type name only if serialization fails
                return $"{typeof(TRequest).Name}:{Guid.NewGuid()}";
            }
        }

        /// <summary>
        /// Gets cache options based on request type
        /// </summary>
        private MemoryCacheEntryOptions GetCacheOptions(TRequest request)
        {
            var options = new MemoryCacheEntryOptions();

            // Set different cache durations based on query type
            var requestName = typeof(TRequest).Name;

            if (requestName.Contains("Search", StringComparison.OrdinalIgnoreCase))
            {
                // Cache search results for 5 minutes
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            }
            else if (requestName.Contains("Get", StringComparison.OrdinalIgnoreCase))
            {
                // Cache get queries for 10 minutes
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            }
            else
            {
                // Default cache duration of 2 minutes
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(2));
            }

            // Set sliding expiration to keep frequently accessed items in cache
            options.SetSlidingExpiration(TimeSpan.FromMinutes(1));

            // Set priority
            options.SetPriority(CacheItemPriority.Normal);

            return options;
        }
    }
}