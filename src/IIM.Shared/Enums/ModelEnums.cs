namespace IIM.Shared.Enums;

public enum ModelFormat
{
    ONNX,
    GGUF,
    GGML,
    Safetensors,
    Pytorch,
    TensorFlow,
    Unknown
}


public enum ModelType
{
    LLM,
    Embedding,
    Whisper,
    CLIP,
    ONNX,
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

public enum PromptFormat
{
    /// <summary>
    /// Plain, untitled prompt (e.g., GPT-2/3 base, no instruction).
    /// </summary>
    PlainText = 0,

    /// <summary>
    /// Instruction/response format (e.g., Alpaca, Dolly, FLAN).
    /// </summary>
    Instruction,

    /// <summary>
    /// OpenAI ChatML format (&lt;|system|&gt; / &lt;|user|&gt; / &lt;|assistant|&gt;).
    /// </summary>
    ChatML,

    /// <summary>
    /// LLaMA-2/3 chat format ([INST], &lt;&lt;SYS&gt;&gt;).
    /// </summary>
    LlamaChat,

    /// <summary>
    /// Vicuna/ShareGPT or OpenChat (USER: ... ASSISTANT: ...).
    /// </summary>
    Vicuna,

    /// <summary>
    /// Harmony format (&lt;|im_start|&gt;role ... &lt;|im_end|&gt;).
    /// </summary>
    Harmony,

    /// <summary>
    /// Zephyr/OpenHermes chat format (&lt;|system|&gt; / &lt;|user|&gt; / &lt;|assistant|&gt;).
    /// </summary>
    Zephyr,

    /// <summary>
    /// MPT Chat (User: ... Assistant: ...).
    /// </summary>
    Mpt,

    /// <summary>
    /// Orca format (&lt;|prompter|&gt; / &lt;|assistant|&gt;).
    /// </summary>
    Orca,

    /// <summary>
    /// Tool-calling/function-calling (structured JSON with function calls).
    /// </summary>
    ToolCall,

    /// <summary>
    /// Multimodal (for vision+text, e.g., with &lt;image&gt; tags).
    /// </summary>
    Multimodal
}



public enum ModelRuntimeState
{
    Initializing,
    Ready,
    Processing,
    Error,
    Disposing
}

/// <summary>
/// Available ONNX execution providers.
/// </summary>
public enum ExecutionProvider
{
    CPU,
    DirectML,
    Vulkan,
    CUDA,
    ROCm
}