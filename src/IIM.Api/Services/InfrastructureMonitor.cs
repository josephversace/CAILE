using IIM.Infrastructure.Platform;
using IIM.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IIM.Api.Services
{
    /// <summary>
    /// Background service that monitors infrastructure health
    /// </summary>
    public class InfrastructureMonitor : BackgroundService
    {
        private readonly ILogger<InfrastructureMonitor> _logger;
        private readonly IWslManager _wslManager;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
        
        public InfrastructureMonitor(
            ILogger<InfrastructureMonitor> logger,
            IWslManager wslManager)
        {
            _logger = logger;
            _wslManager = wslManager;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var health = await _wslManager.HealthCheckAsync();
                    if (!health.IsHealthy)
                    {
                        _logger.LogWarning("Infrastructure unhealthy: {Issues}", 
                            string.Join(", ", health.Issues));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking infrastructure health");
                }
                
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
