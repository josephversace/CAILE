namespace IIM.Shared.Enums;

public enum ModelType
{
    LLM,
    Embedding,
    Whisper,
    CLIP,
    OCR,
    ObjectDetection,
    FaceRecognition,
    Custom,
    Vision,
    Unknown
}

public enum ModelStatus
{
    Available,
    Downloading,
    Loading,
    Loaded,
    Running,
    Unloading,
    Unknown,
    Error
}

public enum ModelSize
{
    Tiny,      // ~100MB
    Small,     // ~500MB
    Base,      // ~1GB
    Medium,    // ~2GB
    Large,     // ~5GB
    XLarge,    // ~10GB
    XXLarge    // >10GB
}

public enum ModelQuantization
{
    Q4_0,      // 4-bit (lowest quality, smallest)
    Q4_K_M,    // 4-bit with k-means (balanced)
    Q5_K_M,    // 5-bit with k-means
    Q8_0,      // 8-bit
    F16,       // 16-bit float
    F32        // 32-bit float (highest quality, largest)
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

/// <summary>
/// Enums needed for the model system.
/// Add these to IIM.Shared.Enums if they don't exist.
/// </summary>
public enum ModelState
{
    Loading,
    Ready,
    Busy,
    Error,
    Unloading
}

public enum ModelPriority
{
    Realtime,   // Lowest latency, may use more resources
    Balanced,   // Default - balance between speed and resource usage
    Throughput  // Maximize throughput, may have higher latency
}

public enum ModelFormat
{
    Unknown,
    ONNX,
    GGUF,
    GGML,
    PyTorch,
    TensorFlow
}



public enum ModelRuntimeState
{
    Initializing,
    Ready,
    Processing,
    Error,
    Disposing
}

