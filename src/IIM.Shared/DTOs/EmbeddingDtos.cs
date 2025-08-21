using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 using System;
using IIM.Shared.DTOs;

    namespace IIM.Shared.DTOs;

    /// <summary>
    /// Request DTO for text embedding generation
    /// </summary>
    /// <param name="Text">Text to embed</param>
    /// <param name="Model">Optional model name (default: all-MiniLM-L6-v2)</param>
    public record EmbeddingRequest(
        string Text,
        string? Model = null
    );

    /// <summary>
    /// Request DTO for batch text embedding generation
    /// </summary>
    /// <param name="Texts">List of texts to embed</param>
    /// <param name="Model">Optional model name</param>
    public record BatchEmbeddingRequest(
        List<string> Texts,
        string? Model = null
    );

    /// <summary>
    /// Request DTO for image embedding generation
    /// </summary>
    /// <param name="ImageData">Base64 encoded image data</param>
    /// <param name="Model">Optional model name (default: CLIP)</param>
    public record ImageEmbeddingRequest(
        string ImageData,
        string? Model = null
    );

    /// <summary>
    /// Request DTO for multi-modal embedding generation
    /// </summary>
    /// <param name="Text">Optional text input</param>
    /// <param name="ImageData">Optional base64 encoded image data</param>
    /// <param name="Model">Optional model name</param>
    public record MultiModalEmbeddingRequest(
        string? Text,
        string? ImageData,
        string? Model = null
    );

    /// <summary>
    /// Response DTO for embedding generation
    /// </summary>
    /// <param name="Embedding">Generated embedding vector</param>
    /// <param name="Dimensions">Number of dimensions in the embedding</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="ProcessingTime">Time taken to generate embedding</param>
    public record EmbeddingResponse(
        float[] Embedding,
        int Dimensions,
        string Model,
        TimeSpan ProcessingTime
    );

    /// <summary>
    /// Response DTO for batch embedding generation
    /// </summary>
    /// <param name="Embeddings">List of generated embedding vectors</param>
    /// <param name="Count">Number of embeddings generated</param>
    /// <param name="Model">Model used for generation</param>
    /// <param name="TotalProcessingTime">Total time for batch processing</param>
    public record BatchEmbeddingResponse(
        List<float[]> Embeddings,
        int Count,
        string Model,
        TimeSpan TotalProcessingTime
    );

    /// <summary>
    /// Information about an available embedding model
    /// </summary>
    /// <param name="Name">Model name</param>
    /// <param name="Type">Model type (text, image, multimodal)</param>
    /// <param name="Dimensions">Output dimensions</param>
    /// <param name="IsLoaded">Whether model is currently loaded</param>
    /// <param name="MemoryUsage">Memory usage in bytes</param>
    /// <param name="SupportedInputTypes">List of supported input types</param>
    public record EmbeddingModelInfo(
        string Name,
        string Type,
        int Dimensions,
        bool IsLoaded,
        long MemoryUsage,
        List<string> SupportedInputTypes
    );

    /// <summary>
    /// Response DTO for available models query
    /// </summary>
    /// <param name="Models">List of available embedding models</param>
    public record EmbeddingModelsResponse(
        List<EmbeddingModelInfo> Models
    );

    /// <summary>
    /// Service information and statistics
    /// </summary>
    /// <param name="Status">Service health status</param>
    /// <param name="LoadedModels">Currently loaded models</param>
    /// <param name="TotalMemoryUsage">Total memory used by loaded models</param>
    /// <param name="RequestsProcessed">Total requests processed</param>
    /// <param name="AverageLatencyMs">Average processing latency in milliseconds</param>
    /// <param name="Version">Service version</param>
    public record EmbeddingServiceInfo(
        string Status,
        List<EmbeddingModelInfo> LoadedModels,
        long TotalMemoryUsage,
        int RequestsProcessed,
        double AverageLatencyMs,
        string Version
    );

    /// <summary>
    /// Request DTO for loading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to load</param>
    /// <param name="Options">Optional model configuration options</param>
    public record LoadEmbeddingModelRequest(
        string ModelName,
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Request DTO for unloading an embedding model
    /// </summary>
    /// <param name="ModelName">Name of the model to unload</param>
    public record UnloadEmbeddingModelRequest(
        string ModelName
    );

    /// <summary>
    /// Response DTO for model operations
    /// </summary>
    /// <param name="Success">Whether the operation was successful</param>
    /// <param name="Message">Optional message</param>
    /// <param name="ModelInfo">Information about the affected model</param>
    public record ModelOperationResponse(
        bool Success,
        string? Message,
        EmbeddingModelInfo? ModelInfo
    );

