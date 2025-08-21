using IIM.Core.AI;
using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;

namespace IIM.Application.Commands.Models
{
    /// <summary>
    /// Command to load an AI model into memory
    /// </summary>
    public class LoadModelCommand : IRequest<ModelHandle>
    {
        /// <summary>
        /// Unique identifier for the model
        /// </summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>
        /// Type of model (LLM, Whisper, CLIP, etc.)
        /// </summary>
        public ModelType ModelType { get; set; }

        /// <summary>
        /// Provider for the model (ONNX, Ollama, LlamaCpp)
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Path to the model file (if local)
        /// </summary>
        public string? ModelPath { get; set; }

        /// <summary>
        /// Download URL if model needs to be fetched
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// Configuration parameters for the model
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }

        /// <summary>
        /// Maximum memory allocation for this model (bytes)
        /// </summary>
        public long? MaxMemoryBytes { get; set; }

        /// <summary>
        /// Device to load the model on (CPU, GPU, etc.)
        /// </summary>
        public string? Device { get; set; }

        /// <summary>
        /// Whether to warm up the model after loading
        /// </summary>
        public bool WarmUp { get; set; } = true;

        /// <summary>
        /// Priority for loading (higher = more important)
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}
