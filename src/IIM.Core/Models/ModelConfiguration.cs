using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

public class ModelRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string? ModelPath { get; set; }
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

public enum ModelType
{
    LLM,
    Embedding,
    Whisper,
    CLIP,
    OCR,
    ObjectDetection,
    FaceRecognition,
    Custom
}

public enum ModelStatus
{
    Available,
    Downloading,
    Loading,
    Loaded,
    Running,
    Unloading,
    Error
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
