using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    /// <summary>
    /// GPU/Device information
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public long MemoryAvailable { get; set; }
        public long MemoryTotal { get; set; }
        public bool SupportsDirectML { get; set; }
        public bool SupportsROCm { get; set; }
    }

}
