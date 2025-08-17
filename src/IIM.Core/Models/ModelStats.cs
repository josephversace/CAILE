using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Core.Models
{
    public sealed class ModelStats
    {
        public int LoadedModels { get; init; }
        public long TotalMemoryUsage { get; init; }
        public long AvailableMemory { get; init; }
        public Dictionary<string, ModelInfo> Models { get; init; } = new();
    }

    /// <summary>
    /// Individual model information - ADD TO Models/ModelStats.cs or similar
    /// </summary>
    public sealed class ModelInfo
    {
        public required string ModelId { get; init; }
        public required ModelType Type { get; init; }
        public long MemoryUsage { get; init; }
        public int AccessCount { get; init; }
        public DateTimeOffset LastAccessed { get; init; }
        public TimeSpan LoadTime { get; init; }
        public double AverageTokensPerSecond { get; init; }
    }
}
