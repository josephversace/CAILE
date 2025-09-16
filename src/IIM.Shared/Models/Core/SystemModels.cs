using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{
 
    public class NetworkInfo
    {
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string Subnet { get; set; } = string.Empty;
    }

   
    
    public class ServiceHealth
    {
        public string ServiceName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    }

 
   
    public class ContainerStatus
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

  
}