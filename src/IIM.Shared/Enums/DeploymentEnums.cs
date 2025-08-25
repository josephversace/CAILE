using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Enums
{
    public enum DeploymentMode
    {
        /// <summary>
        /// Standalone mode - API runs locally on same machine
        /// </summary>
        Standalone,

        /// <summary>
        /// Client mode - Connects to remote API server
        /// </summary>
        Client,

        /// <summary>
        /// Server mode - Not applicable for desktop client
        /// </summary>
        Server
    }

    public enum DeviceType
    {
        /// <summary>
        /// CPU device
        /// </summary>
        CPU,
        /// <summary>
        /// NVIDIA GPU device
        /// </summary>
        CUDA,
        /// <summary>
        /// AMD GPU device
        /// </summary>
        ROCm,
        /// <summary>
        /// Apple Silicon GPU device
        /// </summary>
        MPS,

        Vulkan,

        GPU
    }

}
