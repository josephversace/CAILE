using System;
using System.Collections.Generic;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    // ===== Storage Models =====

    /// <summary>
    /// Result from deduplication process
    /// </summary>
    public class DeduplicationResult
    {
        public string FileHash { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public long OriginalSize { get; set; }
        public long DeduplicatedSize { get; set; }
        public List<string> ChunkHashes { get; set; } = new();
        public List<ChunkData> UniqueChunks { get; set; } = new();
        public List<ChunkData> DuplicateChunks { get; set; } = new();
        public long BytesSaved { get; set; }
        public double DeduplicationRatio { get; set; }
        public bool IsDuplicate { get; set; }
        public string? DuplicateOfId { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        public double GetSpaceSavingPercentage()
        {
            return OriginalSize > 0 ? (double)BytesSaved / OriginalSize * 100 : 0;
        }
    }

    /// <summary>
    /// Data chunk for deduplication
    /// </summary>
    public class ChunkData
    {
        public string Hash { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int Size { get; set; }
        public int Offset { get; set; }
        public int Index { get; set; }
        public bool IsDuplicate { get; set; }
        public string? DuplicateChunkId { get; set; }

        public string GetHashPrefix(int length = 8)
        {
            return Hash.Length >= length ? Hash.Substring(0, length) : Hash;
        }
    }

    /// <summary>
    /// Device information
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceType { get; set; } = string.Empty; // CPU, GPU, TPU
        public string DeviceName { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string Driver { get; set; } = string.Empty;
        public long MemoryAvailable { get; set; }
        public long MemoryTotal { get; set; }
        public int ComputeUnits { get; set; }
        public bool SupportsDirectML { get; set; }
        public bool SupportsROCm { get; set; }
        public bool SupportsCUDA { get; set; }
        public int? CudaVersion { get; set; }
        public Dictionary<string, object>? Capabilities { get; set; }

        public double GetMemoryUsagePercentage()
        {
            return MemoryTotal > 0 ? ((double)(MemoryTotal - MemoryAvailable) / MemoryTotal) * 100 : 0;
        }

        public bool IsGPU()
        {
            return DeviceType.Equals("GPU", StringComparison.OrdinalIgnoreCase);
        }
    }

    // ===== Configuration Models =====

    /// <summary>
    /// Application settings
    /// </summary>
    public class Settings 
    {
        public string Id { get; set; } = "default";
        public string ProfileName { get; set; } = "Default";
        public Dictionary<string, object> General { get; set; } = new();
        public Dictionary<string, object> Models { get; set; } = new();
        public Dictionary<string, object> Storage { get; set; } = new();
        public Dictionary<string, object> Security { get; set; } = new();
        public Dictionary<string, object> Notifications { get; set; } = new();
        public Dictionary<string, object> Api { get; set; } = new();
        public Dictionary<string, object> Ui { get; set; } = new();
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
        public string ModifiedBy { get; set; } = string.Empty;
        public int Version { get; set; } = 1;

        public T GetValue<T>(string section, string key, T defaultValue = default!)
        {
            var sectionDict = section.ToLower() switch
            {
                "general" => General,
                "models" => Models,
                "storage" => Storage,
                "security" => Security,
                "notifications" => Notifications,
                "api" => Api,
                "ui" => Ui,
                _ => null
            };

            if (sectionDict?.TryGetValue(key, out var value) == true)
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        public void SetValue(string section, string key, object value)
        {
            var sectionDict = section.ToLower() switch
            {
                "general" => General,
                "models" => Models,
                "storage" => Storage,
                "security" => Security,
                "notifications" => Notifications,
                "api" => Api,
                "ui" => Ui,
                _ => null
            };

            if (sectionDict != null)
            {
                sectionDict[key] = value;
                LastModified = DateTimeOffset.UtcNow;
                Version++;
            }
        }
    }

    /// <summary>
    /// Model metadata information
    /// </summary>
    public class ModelMetadata
    {
        public string ModelId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public long Size { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Capabilities { get; set; } = new();
        public Dictionary<string, object> Requirements { get; set; } = new();
        public ModelPerformanceMetrics? Performance { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUsed { get; set; }
        public int UsageCount { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        public bool MeetsRequirements(DeviceInfo device)
        {
            if (Requirements.TryGetValue("minMemory", out var minMem))
            {
                var minMemory = Convert.ToInt64(minMem);
                if (device.MemoryAvailable < minMemory)
                    return false;
            }

            if (Requirements.TryGetValue("gpu", out var needsGpu))
            {
                var requiresGpu = Convert.ToBoolean(needsGpu);
                if (requiresGpu && !device.IsGPU())
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Model performance metrics
    /// </summary>
    public class ModelPerformanceMetrics
    {
        public double AverageLatencyMs { get; set; }
        public double TokensPerSecond { get; set; }
        public double Accuracy { get; set; }
        public long MemoryUsageBytes { get; set; }
        public int MaxBatchSize { get; set; }
        public int MaxSequenceLength { get; set; }
        public Dictionary<string, double>? BenchmarkScores { get; set; }
    }
}