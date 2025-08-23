using System;
using System.Collections.Generic;
using IIM.Shared.Enums;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Model capability types
    /// </summary>
    public enum ModelCapability
    {
        Language,      // Text generation, Q&A
        Vision,        // Image analysis, OCR
        Audio,         // Transcription, speaker ID
        Embedding,     // RAG, similarity search
        MultiModal,    // Can handle multiple types
        Specialized    // Forensics, specific tasks
    }

    /// <summary>
    /// Model registration information
    /// </summary>
    public record ModelRegistration(
        string ModelId,
        string Name,
        ModelCapability PrimaryCapability,
        List<ModelCapability>? AdditionalCapabilities,
        bool IsDefault,
        int Priority
    );

    /// <summary>
    /// Request to load a model
    /// </summary>
    public record ModelLoadRequest(
        string ModelId,
        string? ModelPath = null,
        string? Provider = null,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Model configuration request
    /// </summary>
    public record ModelConfigurationRequest(
        string ModelId,
        Dictionary<string, object> Parameters
    );

    /// <summary>
    /// Model operation response
    /// </summary>
    public record ModelOperationResponse(
        bool Success,
        string Message,
        ModelInfo? ModelInfo = null
    );

    /// <summary>
    /// Model information
    /// </summary>
    public record ModelInfo(
        string Id,
        string Name,
        string Type,
        string Provider,
        string Status,
        ModelCapability PrimaryCapability,
        List<ModelCapability>? AdditionalCapabilities,
        long MemoryUsage,
        string? LoadedPath,
        DateTimeOffset? LoadedAt,
        ModelCapabilities Capabilities,
        Dictionary<string, object>? Metadata = null
    );

    /// <summary>
    /// Model capabilities details
    /// </summary>
    public record ModelCapabilities(
        int MaxContextLength,
        List<string> SupportedLanguages,
        List<string>? SpecialFeatures,
        bool SupportsStreaming,
        bool SupportsFineTuning,
        bool SupportsMultiModal,
        Dictionary<string, object>? CustomCapabilities = null
    );

    /// <summary>
    /// Model list response
    /// </summary>
    public record ModelListResponse(
        Dictionary<ModelCapability, List<ModelInfo>> ModelsByCapability,
        long TotalMemoryUsage,
        long AvailableMemory,
        int LoadedCount,
        int AvailableCount
    );

    /// <summary>
    /// Model statistics
    /// </summary>
    public record ModelStats(
        string ModelId,
        string Type,
        long MemoryUsage,
        int AccessCount,
        DateTimeOffset LastAccessed,
        TimeSpan AverageLatency,
        double AverageTokensPerSecond
    );
}