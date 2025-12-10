
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Microsoft.ML.OnnxRuntime;


namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Manages DirectML device enumeration and initialization for GPU acceleration
    /// </summary>
    public interface IDirectMLDeviceManager
    {
        /// <summary>
        /// Creates a DirectML device for inference
        /// </summary>
        /// <param name="deviceId">Device ID (0 for default GPU)</param>
        /// <returns>DirectML device instance</returns>
        Task<DirectMLDevice> CreateDeviceAsync(int deviceId = 0);

        /// <summary>
        /// Enumerates all available DirectML-capable devices
        /// </summary>
        /// <returns>List of available devices</returns>
        Task<IList<DirectMLDevice>> EnumerateDevicesAsync();

        /// <summary>
        /// Gets device capabilities for a specific device
        /// </summary>
        /// <param name="deviceId">Device ID to query</param>
        /// <returns>Device capabilities</returns>
        Task<DeviceCapabilities> GetCapabilitiesAsync(int deviceId);

        /// <summary>
        /// Estimates memory requirements for a model
        /// </summary>
        /// <param name="modelPath">Path to the ONNX model</param>
        /// <returns>Estimated memory in bytes</returns>
        Task<long> EstimateMemoryRequirementsAsync(string modelPath);

        /// <summary>
        /// Validates if a model is compatible with DirectML
        /// </summary>
        /// <param name="modelPath">Path to the ONNX model</param>
        /// <returns>True if compatible</returns>
        Task<bool> ValidateModelCompatibilityAsync(string modelPath);

        SessionOptions GetSessionOptions(int deviceId);

	}
}
