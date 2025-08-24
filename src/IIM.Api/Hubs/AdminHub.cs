using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

using IIM.Api.Configuration;

namespace IIM.Api.Hubs
{
    /// <summary>
    /// SignalR hub for admin notifications (server mode only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminHub : Hub
    {
        private readonly ILogger<AdminHub> _logger;
        
        public AdminHub(ILogger<AdminHub> logger)
        {
            _logger = logger;
        }
        
        // System notifications
        public async Task NotifyConfigurationChanged(string configKey, object newValue)
        {
            await Clients.All.SendAsync("ConfigurationChanged", configKey, newValue);
        }
        
        public async Task NotifySystemStatus(SystemStatus status)
        {
            await Clients.All.SendAsync("SystemStatusUpdate", status);
        }
        
        // User management
        public async Task NotifyUserConnected(string userId, string userName)
        {
            await Clients.All.SendAsync("UserConnected", userId, userName);
        }
        
        public async Task NotifyUserDisconnected(string userId)
        {
            await Clients.All.SendAsync("UserDisconnected", userId);
        }
        
        // Resource monitoring
        public async Task NotifyResourceUsage(ResourceMetrics metrics)
        {
            await Clients.All.SendAsync("ResourceUsageUpdate", metrics);
        }
        
        // Model template changes
        public async Task NotifyTemplateChanged(string templateId, ModelTemplate template)
        {
            await Clients.All.SendAsync("ModelTemplateChanged", templateId, template);
        }
    }
    
    public class SystemStatus
    {
        public bool IsHealthy { get; set; }
        public Dictionary<string, ServiceStatus> Services { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class ResourceMetrics
    {
        public double CpuUsage { get; set; }
        public long MemoryUsed { get; set; }
        public long MemoryTotal { get; set; }
        public double GpuUsage { get; set; }
        public long DiskUsed { get; set; }
        public long DiskTotal { get; set; }
    }
}
