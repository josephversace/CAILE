
using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Interface for text and image embedding generation services
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates embeddings for a text string
        /// </summary>
        /// <param name="text">Input text to embed</param>
        /// <param name="model">Optional model name (default: all-MiniLM-L6-v2)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Float array of embeddings</returns>
        Task<float[]> EmbedTextAsync(string text, string? model = null, CancellationToken ct = default);

        /// <summary>
        /// Batch generates embeddings for multiple text strings
        /// </summary>
        /// <param name="texts">List of texts to embed</param>
        /// <param name="model">Optional model name</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of embedding arrays</returns>
        Task<List<float[]>> BatchEmbedTextAsync(List<string> texts, string? model = null, CancellationToken ct = default);

        /// <summary>
        /// Generates embeddings for an image
        /// </summary>
        /// <param name="imageData">Image data as byte array</param>
        /// <param name="model">Optional model name (default: CLIP)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Float array of embeddings</returns>
        Task<float[]> EmbedImageAsync(byte[] imageData, string? model = null, CancellationToken ct = default);

        /// <summary>
        /// Batch generates embeddings for multiple images
        /// </summary>
        /// <param name="images">List of image data arrays</param>
        /// <param name="model">Optional model name</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of embedding arrays</returns>
        Task<List<float[]>> BatchEmbedImagesAsync(List<byte[]> images, string? model = null, CancellationToken ct = default);

        /// <summary>
        /// Generates multi-modal embeddings from text and/or image
        /// </summary>
        /// <param name="text">Optional text input</param>
        /// <param name="imageData">Optional image data</param>
        /// <param name="model">Optional model name</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Float array of combined embeddings</returns>
        Task<float[]> EmbedMultiModalAsync(string? text, byte[]? imageData, string? model = null, CancellationToken ct = default);

        /// <summary>
        /// Gets list of available embedding models
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of available embedding models</returns>
        Task<List<EmbeddingModelInfo>> GetAvailableModelsAsync(CancellationToken ct = default);

        /// <summary>
        /// Loads a specific embedding model into memory
        /// </summary>
        /// <param name="modelName">Name of model to load</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if model loaded successfully</returns>
        Task<bool> LoadModelAsync(string modelName, CancellationToken ct = default);

        /// <summary>
        /// Unloads a specific embedding model from memory
        /// </summary>
        /// <param name="modelName">Name of model to unload</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if model unloaded successfully</returns>
        Task<bool> UnloadModelAsync(string modelName, CancellationToken ct = default);

        /// <summary>
        /// Checks if the embedding service is healthy
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if service is healthy</returns>
        Task<bool> IsHealthyAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets service information and statistics
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Service information including loaded models and stats</returns>
        Task<EmbeddingServiceInfo> GetInfoAsync(CancellationToken ct = default);
    }
}
