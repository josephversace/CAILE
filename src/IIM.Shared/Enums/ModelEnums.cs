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

