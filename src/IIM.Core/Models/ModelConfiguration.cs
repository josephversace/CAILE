using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IIM.Shared.Enums;

namespace IIM.Core.Models;


public class ModelConfiguration
{
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public ModelType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public ModelStatus Status { get; set; }
    public long MemoryUsage { get; set; }
    public string? LoadedPath { get; set; }
    public DateTimeOffset? LoadedAt { get; set; }
    public ModelCapabilities Capabilities { get; set; } = new();
    public string Name { get; set; } = string.Empty;
}

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

public class ModelCapabilities
{
    public int MaxContextLength { get; set; }
    public List<string> SupportedLanguages { get; set; } = new();
    public List<string> SpecialFeatures { get; set; } = new();
    public bool SupportsStreaming { get; set; }
    public bool SupportsFineTuning { get; set; }
    public bool SupportsMultiModal { get; set; }
    public Dictionary<string, object> CustomCapabilities { get; set; } = new();
}





public enum ModalityType
{
    Text,
    Audio,
    Image,
    Video,
    Document,
    Structured
}

