using System.Collections.Generic;

namespace IIM.Shared.Models
{
    /// <summary>
    /// WSL status information
    /// </summary>
    public class WslStatus
    {
        public bool WslReady { get; set; }
        public bool DistroRunning { get; set; }
        public bool ServicesHealthy { get; set; }
        public bool NetworkConnected { get; set; }
        public List<string> Issues { get; set; } = new();
    }
}