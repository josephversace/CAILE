using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IIM.Shared.Enums;

namespace IIM.Core.Models;




public sealed class ModelRequest
{
    public required string ModelId { get; init; }
    public required string ModelPath { get; init; }
    public required ModelType ModelType { get; init; }
    public string ModelSize { get; init; } = "medium";
    public string Quantization { get; init; } = "Q4_K_M";
    public int ContextSize { get; init; } = 4096;
    public int BatchSize { get; init; } = 512;
    public int GpuLayers { get; init; } = -1;
    public string? Provider { get; set; }
    public Dictionary<string, object>? Options { get; set; }
}


public class ModelHandle
{
    public string ModelId { get; set; } = string.Empty;
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string Provider { get; set; } = string.Empty;
    public ModelType Type { get; set; }
    public IntPtr Handle { get; set; }
    public long MemoryUsage { get; set; }
    public DateTimeOffset LoadedAt { get; set; } = DateTimeOffset.UtcNow;
}








