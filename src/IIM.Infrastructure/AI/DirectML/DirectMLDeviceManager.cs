// ============================================
// File: src/IIM.Infrastructure/AI/DirectML/DirectMLDeviceManager.cs
// Purpose: DirectML device enumeration and management for AMD GPUs
// Author: IIM Platform Team
// Created: 2024
// ============================================

using IIM.Shared.Enums;  // Only use Shared - it has no dependencies
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vortice.DirectML;

namespace IIM.Infrastructure.AI.DirectML
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
    }

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

    /// <summary>
    /// Implementation of DirectML device manager
    /// </summary>
    public class DirectMLDeviceManager : IDirectMLDeviceManager, IDisposable
    {
        private readonly ILogger<DirectMLDeviceManager> _logger;
        private readonly Dictionary<int, SessionOptions> _sessionOptionsCache = new();
        private readonly object _lock = new();

        public DirectMLDeviceManager(ILogger<DirectMLDeviceManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("DirectML Device Manager initialized");
        }

        /// <summary>
        /// Creates a DirectML device for inference
        /// </summary>
        public async Task<DirectMLDevice> CreateDeviceAsync(int deviceId = 0)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Creating DirectML device with ID {DeviceId}", deviceId);

                    // Check if DirectML is available
                    if (!IsDirectMLAvailable())
                    {
                        throw new InvalidOperationException("DirectML is not available on this system");
                    }

                    // Get device information
                    var device = GetDeviceInfo(deviceId);

                    // Create and cache session options for this device
                    lock (_lock)
                    {
                        if (!_sessionOptionsCache.ContainsKey(deviceId))
                        {
                            var options = CreateSessionOptions(deviceId);
                            _sessionOptionsCache[deviceId] = options;
                        }
                    }

                    _logger.LogInformation("Successfully created DirectML device: {DeviceName}", device.Name);
                    return device;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create DirectML device {DeviceId}", deviceId);
                    throw;
                }
            });
        }

        /// <summary>
        /// Enumerates all available DirectML-capable devices
        /// </summary>
        public async Task<IList<DirectMLDevice>> EnumerateDevicesAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<DirectMLDevice>();

                try
                {
                    _logger.LogInformation("Enumerating DirectML devices");

                    // Check for AMD GPUs first (primary target)
                    var amdDevices = EnumerateAMDDevices();
                    devices.AddRange(amdDevices);

                    // Check for other DirectML-capable devices
                    var otherDevices = EnumerateOtherDevices();
                    devices.AddRange(otherDevices);

                    // Always add CPU as fallback
                    devices.Add(new DirectMLDevice
                    {
                        DeviceId = -1,
                        Name = "CPU (Fallback)",
                        Vendor = "Generic",
                        DeviceType = "CPU",
                        DedicatedMemory = 0,
                        SharedMemory = GetAvailableSystemMemory()
                    });

                    _logger.LogInformation("Found {Count} DirectML-capable devices", devices.Count);
                    return devices;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to enumerate DirectML devices");

                    // Return CPU fallback on error
                    return new List<DirectMLDevice>
                    {
                        new DirectMLDevice
                        {
                            DeviceId = -1,
                            Name = "CPU (Fallback)",
                            Vendor = "Generic",
                            DeviceType = "CPU",
                            DedicatedMemory = 0,
                            SharedMemory = GetAvailableSystemMemory()
                        }
                    };
                }
            });
        }

        /// <summary>
        /// Gets device capabilities
        /// </summary>
        public async Task<DeviceCapabilities> GetCapabilitiesAsync(int deviceId)
        {
            //Todo change for production
            return await Task.Run(() =>
            {
                _logger.LogInformation("Getting capabilities for device {DeviceId}", deviceId);

                var capabilities = new DeviceCapabilities
                {
                    DeviceId = deviceId,
                    SupportsFloat16 = true,
                    SupportsInt8 = true,
                    SupportsDynamicShapes = true,
                    MaxTensorRank = 8,
                    MaxTensorSizeInBytes = 2L * 1024 * 1024 * 1024, // 2GB
                    MaxBatchSize = 32,
                    FeatureLevel = GetFeatureLevel()
                };

                // Add supported operators
                capabilities.SupportedOperators.AddRange(GetSupportedOperators());

                return capabilities;
            });
        }

        /// <summary>
        /// Estimates memory requirements for a model
        /// </summary>
        public async Task<long> EstimateMemoryRequirementsAsync(string modelPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Estimating memory requirements for {Model}", modelPath);

                    if (!System.IO.File.Exists(modelPath))
                    {
                        throw new System.IO.FileNotFoundException($"Model file not found: {modelPath}");
                    }

                    // Get file size as base
                    var fileInfo = new System.IO.FileInfo(modelPath);
                    var modelSize = fileInfo.Length;

                    // Estimate total memory (model + working memory)
                    // Typically need 2-3x model size for inference
                    var estimatedMemory = modelSize * 3;

                    // Add buffer for DirectML overhead (500MB)
                    estimatedMemory += 500 * 1024 * 1024;

                    _logger.LogInformation("Estimated memory requirement: {Memory:N0} bytes", estimatedMemory);
                    return estimatedMemory;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to estimate memory for {Model}", modelPath);
                    throw;
                }
            });
        }

        /// <summary>
        /// Validates model compatibility with DirectML
        /// </summary>
        public async Task<bool> ValidateModelCompatibilityAsync(string modelPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Validating model compatibility: {Model}", modelPath);

                    if (!System.IO.File.Exists(modelPath))
                    {
                        _logger.LogWarning("Model file not found: {Model}", modelPath);
                        return false;
                    }

                    // Try to create an inference session with DirectML
                    using var sessionOptions = CreateSessionOptions(0);
                    using var session = new InferenceSession(modelPath, sessionOptions);

                    // If we get here, model is compatible
                    _logger.LogInformation("Model is compatible with DirectML: {Model}", modelPath);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Model is not compatible with DirectML: {Model}", modelPath);
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets the ONNX session options for a device
        /// </summary>
        public SessionOptions GetSessionOptions(int deviceId)
        {
            lock (_lock)
            {
                if (_sessionOptionsCache.TryGetValue(deviceId, out var options))
                {
                    return options;
                }

                var newOptions = CreateSessionOptions(deviceId);
                _sessionOptionsCache[deviceId] = newOptions;
                return newOptions;
            }
        }

        // ========================================
        // Private Helper Methods
        // ========================================

        private bool IsDirectMLAvailable()
        {
            try
            {
                // Check if DirectML.dll exists
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                    var directMLPath = System.IO.Path.Combine(systemPath, "DirectML.dll");
                    return System.IO.File.Exists(directMLPath);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private DirectMLDevice GetDeviceInfo(int deviceId)
        {
            // For now, return a mock AMD device
            // In production, this would use WMI or DirectX APIs
            return new DirectMLDevice
            {
                DeviceId = deviceId,
                Name = "AMD Radeon RX 7900 XTX",
                Vendor = "Advanced Micro Devices, Inc.",
                DedicatedMemory = 24L * 1024 * 1024 * 1024, // 24GB
                SharedMemory = 16L * 1024 * 1024 * 1024, // 16GB
                IsDefault = deviceId == 0,
                DeviceType = "GPU",
                ComputeUnits = 96,
                DriverVersion = "23.11.1"
            };

            //Todo change in production

            //var query = "SELECT * FROM Win32_VideoController";
            //using (var searcher = new ManagementObjectSearcher(query))
            //{
            //    int currentId = 0;
            //    foreach (ManagementObject mo in searcher.Get())
            //    {
            //        if (currentId == deviceId)
            //        {
            //            string name = mo["Name"]?.ToString() ?? "Unknown";
            //            string vendor = mo["AdapterCompatibility"]?.ToString() ?? "Unknown";
            //            string driverVersion = mo["DriverVersion"]?.ToString() ?? "Unknown";
            //            long? dedicatedMemory = mo["AdapterRAM"] as long?;
            //            // Shared memory not always available from WMI; default to 0 or use another source if needed
            //            long sharedMemory = 0;

            //            return new DirectMLDevice
            //            {
            //                DeviceId = deviceId,
            //                Name = name,
            //                Vendor = vendor,
            //                DedicatedMemory = dedicatedMemory ?? 0,
            //                SharedMemory = sharedMemory,
            //                IsDefault = deviceId == 0,
            //                DeviceType = "GPU",
            //                ComputeUnits = 0, // WMI does not provide this; you'd need DirectX for actual CU count
            //                DriverVersion = driverVersion
            //            };
            //        }
            //        currentId++;
            //    }
            //}
            //throw new ArgumentOutOfRangeException(nameof(deviceId), $"GPU with id {deviceId} not found.");

        }

        private SessionOptions CreateSessionOptions(int deviceId)
        {
            var options = new SessionOptions();

            // Configure for optimal performance
            options.ExecutionMode = ExecutionMode.ORT_PARALLEL;
            options.InterOpNumThreads = Environment.ProcessorCount;
            options.IntraOpNumThreads = Environment.ProcessorCount;
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

            // Enable memory pattern optimization
            options.EnableMemoryPattern = true;
            options.EnableCpuMemArena = true;

            // Add DirectML execution provider
            if (deviceId >= 0)
            {
                try
                {
                    options.AppendExecutionProvider_DML(deviceId);
                    _logger.LogInformation("DirectML execution provider added for device {DeviceId}", deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add DirectML provider, falling back to CPU");
                    options.AppendExecutionProvider_CPU();
                }
            }
            else
            {
                options.AppendExecutionProvider_CPU();
            }

            return options;
        }

        private List<DirectMLDevice> EnumerateAMDDevices()
        {
            var devices = new List<DirectMLDevice>();

            // This is a simplified implementation
            // In production, use WMI or DirectX enumeration
            try
            {
                devices.Add(new DirectMLDevice
                {
                    DeviceId = 0,
                    Name = "AMD Radeon RX 7900 XTX",
                    Vendor = "AMD",
                    DeviceType = "GPU",
                    DedicatedMemory = 24L * 1024 * 1024 * 1024,
                    SharedMemory = 16L * 1024 * 1024 * 1024,
                    ComputeUnits = 96,
                    IsDefault = true,
                    DriverVersion = "23.11.1"
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "No AMD devices found");
            }

            return devices;

            //Todo change in production
            //var devices = new List<DirectMLDevice>();
            //var query = "SELECT * FROM Win32_VideoController";

            //try
            //{
            //    using (var searcher = new ManagementObjectSearcher(query))
            //    {
            //        int deviceIndex = 0;
            //        foreach (ManagementObject mo in searcher.Get())
            //        {
            //            var vendor = mo["AdapterCompatibility"]?.ToString() ?? string.Empty;
            //            // Filter for AMD only (case-insensitive, handles various naming)
            //            if (!vendor.ToLower().Contains("amd") && !vendor.ToLower().Contains("advanced micro devices"))
            //                continue;

            //            var name = mo["Name"]?.ToString() ?? "Unknown";
            //            var driverVersion = mo["DriverVersion"]?.ToString() ?? "Unknown";
            //            long dedicatedMemory = mo["AdapterRAM"] as long? ?? 0;

            //            devices.Add(new DirectMLDevice
            //            {
            //                DeviceId = deviceIndex,
            //                Name = name,
            //                Vendor = vendor,
            //                DeviceType = "GPU",
            //                DedicatedMemory = dedicatedMemory,
            //                SharedMemory = 0, // Not easily available via WMI
            //                ComputeUnits = 0, // Not available via WMI
            //                IsDefault = deviceIndex == 0,
            //                DriverVersion = driverVersion
            //            });

            //            deviceIndex++;
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger?.LogError(ex, "Error while enumerating AMD GPU devices.");
            //}

            //if (devices.Count == 0)
            //    _logger?.LogWarning("No AMD devices found!");

            //return devices;
        }

        private List<DirectMLDevice> EnumerateOtherDevices()
        {
            var devices = new List<DirectMLDevice>();

            // Check for integrated graphics
            try
            {
                devices.Add(new DirectMLDevice
                {
                    DeviceId = 1,
                    Name = "AMD Radeon Graphics (Integrated)",
                    Vendor = "AMD",
                    DeviceType = "GPU",  // Integrated GPU
                    DedicatedMemory = 512 * 1024 * 1024, // 512MB
                    SharedMemory = 8L * 1024 * 1024 * 1024, // 8GB shared
                    ComputeUnits = 8,
                    DriverVersion = "23.11.1"
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "No integrated GPU found");
            }

            return devices;

            //Todo change in production
            //var devices = new List<DirectMLDevice>();
            //var query = "SELECT * FROM Win32_VideoController";

            //try
            //{
            //    using (var searcher = new ManagementObjectSearcher(query))
            //    {
            //        int deviceIndex = 0;
            //        foreach (ManagementObject mo in searcher.Get())
            //        {
            //            var vendor = mo["AdapterCompatibility"]?.ToString() ?? string.Empty;
            //            // Filter for NON-AMD devices (e.g., Intel, NVIDIA, Microsoft Basic)
            //            if (vendor.ToLower().Contains("amd") || vendor.ToLower().Contains("advanced micro devices"))
            //                continue;

            //            var name = mo["Name"]?.ToString() ?? "Unknown";
            //            var driverVersion = mo["DriverVersion"]?.ToString() ?? "Unknown";
            //            long dedicatedMemory = mo["AdapterRAM"] as long? ?? 0;

            //            devices.Add(new DirectMLDevice
            //            {
            //                DeviceId = deviceIndex,
            //                Name = name,
            //                Vendor = vendor,
            //                DeviceType = "GPU", // WMI cannot reliably tell integrated vs discrete, but you can parse 'Name' if needed
            //                DedicatedMemory = dedicatedMemory,
            //                SharedMemory = 0, // Not available via WMI
            //                ComputeUnits = 0, // Not available via WMI
            //                DriverVersion = driverVersion,
            //                IsDefault = deviceIndex == 0
            //            });

            //            deviceIndex++;
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger?.LogError(ex, "Error while enumerating non-AMD GPU devices.");
            //}

            //if (devices.Count == 0)
            //    _logger?.LogWarning("No non-AMD GPU devices found!");

            //return devices;
        }



private DirectMLFeatureLevel GetFeatureLevel()
    {
    //    // Try highest to lowest
    //    var levels = new[]
    //    {
    //    DirectMLFeatureLevel.Level_5_0,
    //    DirectMLFeatureLevel.Level_4_1,
    //    DirectMLFeatureLevel.Level_4_0,
    //    DirectMLFeatureLevel.Level_3_0
    //};

    //    foreach (var level in levels)
    //    {
    //        try
    //        {
    //            using var device = DML.DMLCreateDevice(
    //                d3d12Device: null,
    //                CreateDeviceFlags.None,
    //                minimumFeatureLevel: level
    //            );
    //            if (device != null)
    //                return level;
    //        }
    //        catch
    //        {
    //            // Ignore and try lower level
    //        }
    //    }

       throw new InvalidOperationException("No supported DirectML feature level found.");
    }




    private List<string> GetSupportedOperators()
        {
            // Return list of ONNX operators supported by DirectML
            return new List<string>
            {
                "Add", "Sub", "Mul", "Div",
                "Conv", "ConvTranspose",
                "BatchNormalization", "LayerNormalization",
                "Relu", "LeakyRelu", "Gelu", "Tanh", "Sigmoid",
                "MaxPool", "AveragePool", "GlobalAveragePool",
                "Gemm", "MatMul",
                "Softmax", "LogSoftmax",
                "Concat", "Split", "Slice",
                "Reshape", "Transpose", "Squeeze", "Unsqueeze",
                "Cast", "Clip", "Pad",
                "ReduceMean", "ReduceSum", "ReduceMax", "ReduceMin",
                "Gather", "GatherElements", "ScatterElements",
                "Where", "Equal", "Greater", "Less",
                "LSTM", "GRU", "RNN"
            };
        }

private long GetAvailableSystemMemory()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: use Windows API via PerformanceCounter or ManagementObject
                return GetWindowsAvailableMemory();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: read /proc/meminfo
                return GetLinuxAvailableMemory();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // MacOS: use vm_stat
                return GetMacAvailableMemory();
            }
        }
        catch { }

        // Fallback: Assume 16 GB
        return 16L * 1024 * 1024 * 1024;
    }

    private long GetWindowsAvailableMemory()
    {
        // This is the most compatible way without VisualBasic:
        // Use Windows API via interop, or WMI, or PerformanceCounter.
        // For brevity, here is a WMI solution (requires System.Management NuGet package):
        try
        {
            var wmiQuery = "SELECT FreePhysicalMemory FROM Win32_OperatingSystem";
            using var searcher = new System.Management.ManagementObjectSearcher(wmiQuery);
            foreach (var obj in searcher.Get())
            {
                var freeKb = Convert.ToInt64(obj["FreePhysicalMemory"]);
                return freeKb * 1024; // KB to bytes
            }
        }
        catch { }
        return 0;
    }

    private long GetLinuxAvailableMemory()
    {
        try
        {
            // Read MemAvailable from /proc/meminfo
            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                if (line.StartsWith("MemAvailable:"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (long.TryParse(parts[1], out var kb))
                        return kb * 1024; // kB to bytes
                }
            }
        }
        catch { }
        return 0;
    }

    private long GetMacAvailableMemory()
    {
        try
        {
            // Use 'vm_stat' to get free pages
            var output = "";
            using (var proc = new System.Diagnostics.Process())
            {
                proc.StartInfo.FileName = "vm_stat";
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
            }
            var pageSize = 4096; // bytes (typical, check if you need to parse from output)
            foreach (var line in output.Split('\n'))
            {
                if (line.Trim().StartsWith("Pages free:"))
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2 && long.TryParse(parts[1].Trim().Replace(".", ""), out var pages))
                        return pages * pageSize;
                }
            }
        }
        catch { }
        return 0;
    }


    public void Dispose()
        {
            lock (_lock)
            {
                foreach (var options in _sessionOptionsCache.Values)
                {
                    options?.Dispose();
                }
                _sessionOptionsCache.Clear();
            }
        }
    }
}