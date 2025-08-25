using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    /// <summary>
    /// DirectML device information - extends existing DeviceInfo
    /// </summary>
    public class DirectMLDevice
    {
        public int DeviceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public long DedicatedMemory { get; set; }
        public long SharedMemory { get; set; }
        public bool IsDefault { get; set; }
        public string DeviceType { get; set; } = "GPU";  // Use string to match existing DeviceInfo
        public int ComputeUnits { get; set; }
        public string DriverVersion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Device capabilities
    /// </summary>
    public class DeviceCapabilities
    {
        public int DeviceId { get; set; }
        public bool SupportsFloat16 { get; set; }
        public bool SupportsInt8 { get; set; }
        public bool SupportsDynamicShapes { get; set; }
        public int MaxTensorRank { get; set; }
        public long MaxTensorSizeInBytes { get; set; }
        public int MaxBatchSize { get; set; }
        public List<string> SupportedOperators { get; set; } = new();
        public DirectMLFeatureLevel FeatureLevel { get; set; }
    }

    /// <summary>
    /// DirectML feature level
    /// </summary>
    public enum DirectMLFeatureLevel
    {
        Unknown = 0,
        Level_1_0 = 0x1000,
        Level_2_0 = 0x2000,
        Level_2_1 = 0x2100,
        Level_3_0 = 0x3000,
        Level_3_1 = 0x3100,
        Level_4_0 = 0x4000,
        Level_4_1 = 0x4100,
        Level_5_0 = 0x5000
    }

}
