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
