using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.AI;
using IIM.Core.Models;
using IIM.Core.Inference;
using Microsoft.Extensions.Logging;
using IIM.Shared.Enums;
using IIM.Shared.Models;

namespace IIM.Core.Services
{
    /// <summary>
    /// Service that manages AI models and GPU resources.
    /// Wraps IModelOrchestrator for simplified access.
    /// </summary>
    public interface IModelManagementService
    {
        Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
        Task<bool> LoadModelAsync(string modelId, CancellationToken cancellationToken = default);
        Task<bool> UnloadModelAsync(string modelId, CancellationToken cancellationToken = default);
        Task<GpuStats> GetGpuStatsAsync(CancellationToken cancellationToken = default);
    }

    public class ModelManagementService : IModelManagementService
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger<ModelManagementService> _logger;
        private readonly string _modelsBasePath;

        public ModelManagementService(IModelOrchestrator modelOrchestrator, ILogger<ModelManagementService> logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize models base path from environment or use default
            _modelsBasePath = Environment.GetEnvironmentVariable("IIM_MODELS_PATH")
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");

            // Ensure models directory exists
            if (!Directory.Exists(_modelsBasePath))
            {
                Directory.CreateDirectory(_modelsBasePath);
                _logger.LogInformation("Created models directory at {Path}", _modelsBasePath);
            }
        }

        public async Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            return await _modelOrchestrator.GetAvailableModelsAsync(cancellationToken);
        }

        public async Task<bool> LoadModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Loading model {ModelId}", modelId);

                // Determine model type based on modelId patterns
                var modelType = DetermineModelType(modelId);
                var modelPath = GetModelPath(modelId);

                // Create properly initialized ModelRequest with all required fields
                var request = new ModelRequest
                {
                    ModelId = modelId,                    // Required field
                    ModelPath = modelPath,                 // Required field  
                    ModelType = modelType,                 // Required field
                    ModelSize = GetModelSize(modelId),
                    Quantization = GetQuantization(modelId),
                    ContextSize = GetContextSize(modelType),
                    BatchSize = 512,
                    GpuLayers = -1,  // Use all available GPU layers
                    Provider = GetProvider(modelType),
                    Options = new Dictionary<string, object>
                    {
                        ["use_gpu"] = true,
                        ["num_threads"] = Environment.ProcessorCount
                    }
                };

                var handle = await _modelOrchestrator.LoadModelAsync(request, null, cancellationToken);
                _logger.LogInformation("Successfully loaded model {ModelId} with session {SessionId}",
                    modelId, handle.SessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model {ModelId}", modelId);
                return false;
            }
        }

        public async Task<bool> UnloadModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _modelOrchestrator.UnloadModelAsync(modelId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload model {ModelId}", modelId);
                return false;
            }
        }

        public async Task<GpuStats> GetGpuStatsAsync(CancellationToken cancellationToken = default)
        {
            return await _modelOrchestrator.GetGpuStatsAsync(cancellationToken);
        }

        // Helper methods to determine model properties
        private ModelType DetermineModelType(string modelId)
        {
            var lowerModelId = modelId.ToLowerInvariant();

            if (lowerModelId.Contains("whisper"))
                return ModelType.Whisper;
            if (lowerModelId.Contains("clip") || lowerModelId.Contains("vision"))
                return ModelType.CLIP;
            if (lowerModelId.Contains("embed") || lowerModelId.Contains("bge") || lowerModelId.Contains("e5"))
                return ModelType.Embedding;
            if (lowerModelId.Contains("ocr") || lowerModelId.Contains("tesseract"))
                return ModelType.OCR;
            if (lowerModelId.Contains("yolo") || lowerModelId.Contains("detection"))
                return ModelType.ObjectDetection;
            if (lowerModelId.Contains("face") || lowerModelId.Contains("recognition"))
                return ModelType.FaceRecognition;
            if (lowerModelId.Contains("llama") || lowerModelId.Contains("mistral") ||
                lowerModelId.Contains("phi") || lowerModelId.Contains("qwen"))
                return ModelType.LLM;

            // Default to LLM if unclear
            return ModelType.LLM;
        }

        private string GetModelPath(string modelId)
        {
            // Check if model has a specific path configured
            var specificPath = Path.Combine(_modelsBasePath, modelId);
            if (Directory.Exists(specificPath))
            {
                // Look for model files in the directory
                var modelFiles = Directory.GetFiles(specificPath, "*.gguf", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.GetFiles(specificPath, "*.bin", SearchOption.TopDirectoryOnly))
                    .Concat(Directory.GetFiles(specificPath, "*.onnx", SearchOption.TopDirectoryOnly))
                    .Concat(Directory.GetFiles(specificPath, "*.pt", SearchOption.TopDirectoryOnly));

                if (modelFiles.Any())
                {
                    return modelFiles.First();
                }
            }

            // Return expected path even if file doesn't exist yet (might be downloaded later)
            return Path.Combine(_modelsBasePath, modelId, $"{modelId}.gguf");
        }

        private string GetModelSize(string modelId)
        {
            var lowerModelId = modelId.ToLowerInvariant();

            if (lowerModelId.Contains("tiny"))
                return "tiny";
            if (lowerModelId.Contains("small"))
                return "small";
            if (lowerModelId.Contains("base"))
                return "base";
            if (lowerModelId.Contains("large"))
                return "large";
            if (lowerModelId.Contains("xl") || lowerModelId.Contains("extra"))
                return "xl";

            // Try to extract size from model name (e.g., "llama-7b", "mistral-8x7b")
            if (System.Text.RegularExpressions.Regex.Match(lowerModelId, @"(\d+)b").Success)
            {
                var match = System.Text.RegularExpressions.Regex.Match(lowerModelId, @"(\d+)b");
                var size = int.Parse(match.Groups[1].Value);

                if (size <= 3) return "small";
                if (size <= 7) return "medium";
                if (size <= 13) return "large";
                return "xl";
            }

            return "medium";
        }

        private string GetQuantization(string modelId)
        {
            var lowerModelId = modelId.ToLowerInvariant();

            // Check for specific quantization in model name
            if (lowerModelId.Contains("q4_k_m"))
                return "Q4_K_M";
            if (lowerModelId.Contains("q5_k_m"))
                return "Q5_K_M";
            if (lowerModelId.Contains("q8_0"))
                return "Q8_0";
            if (lowerModelId.Contains("q4_0"))
                return "Q4_0";
            if (lowerModelId.Contains("f16"))
                return "F16";
            if (lowerModelId.Contains("f32"))
                return "F32";

            // Default quantization for efficiency
            return "Q4_K_M";
        }

        private int GetContextSize(ModelType modelType)
        {
            return modelType switch
            {
                ModelType.LLM => 8192,        // Most modern LLMs support 8k context
                ModelType.Embedding => 512,    // Embedders typically use smaller context
                ModelType.Whisper => 1500,     // ~30 seconds of audio
                ModelType.CLIP => 77,          // CLIP text encoder limit
                _ => 4096                      // Safe default
            };
        }

        private string GetProvider(ModelType modelType)
        {
            return modelType switch
            {
                ModelType.LLM => "llama.cpp",
                ModelType.Whisper => "whisper.cpp",
                ModelType.CLIP => "onnxruntime",
                ModelType.Embedding => "sentence-transformers",
                ModelType.OCR => "tesseract",
                ModelType.ObjectDetection => "yolov8",
                ModelType.FaceRecognition => "insightface",
                _ => "custom"
            };
        }
    }
}