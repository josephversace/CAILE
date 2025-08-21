using System.Collections.Generic;

namespace IIM.Shared.Models
{
    /// <summary>
    /// System status information
    /// </summary>
    public class SystemStatus
    {
        public bool IsHealthy { get; set; }
        public long MemoryUsed { get; set; }
        public long MemoryTotal { get; set; }
        public int LoadedModels { get; set; }
        public double CpuUsage { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}