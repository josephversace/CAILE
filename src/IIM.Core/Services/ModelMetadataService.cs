using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IIM.Core.Services
{
    /// <summary>
    /// Model metadata for routing and resource management
    /// </summary>

  

    /// <summary>
    /// Implementation of model metadata service with caching and configuration support
    /// </summary>
    public class ModelMetadataService : IModelMetadataService
    {
        private readonly ILogger<ModelMetadataService> _logger;
        private readonly IOptions<ModelMetadataConfiguration> _config;
        private readonly ConcurrentDictionary<string, ModelMetadata> _metadata = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private bool _isLoaded = false;

        public ModelMetadataService(
            ILogger<ModelMetadataService> logger,
            IOptions<ModelMetadataConfiguration> config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Gets metadata for a specific model
        /// </summary>
        public async Task<ModelMetadata> GetMetadataAsync(string modelId, CancellationToken ct = default)
        {
            // Ensure metadata is loaded
            if (!_isLoaded)
            {
                await LoadFromConfigurationAsync(ct);
            }

            if (_metadata.TryGetValue(modelId, out var metadata))
            {
                return metadata;
            }

            // Return default metadata if not found
            _logger.LogWarning("Metadata not found for model {ModelId}, using defaults", modelId);
            return CreateDefaultMetadata(modelId);
        }

        /// <summary>
        /// Registers or updates model metadata
        /// </summary>
        public Task RegisterMetadataAsync(ModelMetadata metadata, CancellationToken ct = default)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            _metadata.AddOrUpdate(metadata.ModelId, metadata, (_, _) => metadata);
            _logger.LogInformation("Registered metadata for model {ModelId}", metadata.ModelId);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets all registered metadata
        /// </summary>
        public async Task<List<ModelMetadata>> GetAllMetadataAsync(CancellationToken ct = default)
        {
            if (!_isLoaded)
            {
                await LoadFromConfigurationAsync(ct);
            }

            return new List<ModelMetadata>(_metadata.Values);
        }

        /// <summary>
        /// Loads metadata from configuration
        /// </summary>
        public async Task LoadFromConfigurationAsync(CancellationToken ct = default)
        {
            await _loadLock.WaitAsync(ct);
            try
            {
                if (_isLoaded) return;

                _logger.LogInformation("Loading model metadata from configuration");

                // Load from configuration
                foreach (var model in _config.Value.Models)
                {
                    _metadata[model.ModelId] = model;
                }

                // Load from JSON file if specified
                if (!string.IsNullOrEmpty(_config.Value.MetadataFilePath))
                {
                    await LoadFromFileAsync(_config.Value.MetadataFilePath, ct);
                }

                // Add default models if not present
                EnsureDefaultModels();

                _isLoaded = true;
                _logger.LogInformation("Loaded metadata for {Count} models", _metadata.Count);
            }
            finally
            {
                _loadLock.Release();
            }
        }

        /// <summary>
        /// Loads metadata from a JSON file
        /// </summary>
        private async Task LoadFromFileAsync(string filePath, CancellationToken ct)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath, ct);
                    var models = JsonSerializer.Deserialize<List<ModelMetadata>>(json);

                    if (models != null)
                    {
                        foreach (var model in models)
                        {
                            _metadata[model.ModelId] = model;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load metadata from file {FilePath}", filePath);
            }
        }

        /// <summary>
        /// Ensures default models are registered
        /// </summary>
        private void EnsureDefaultModels()
        {
            // LLM Models
            RegisterDefaultIfMissing(new ModelMetadata
            {
                ModelId = "llama3.1:70b",
                Type = ModelType.LLM,
                RequiresGpu = true,
                SupportsBatching = false,
                EstimatedMemoryMb = 40000,
                DefaultPriority = 1,
                Provider = "ollama"
            });

            RegisterDefaultIfMissing(new ModelMetadata
            {
                ModelId = "phi-3-mini",
                Type = ModelType.LLM,
                RequiresGpu = true,
                SupportsBatching = false,
                EstimatedMemoryMb = 4000,
                DefaultPriority = 1,
                Provider = "directml"
            });

            // Whisper Models
            RegisterDefaultIfMissing(new ModelMetadata
            {
                ModelId = "whisper-base",
                Type = ModelType.Whisper,
                RequiresGpu = true,
                SupportsBatching = false,
                EstimatedMemoryMb = 150,
                DefaultPriority = 2, // High priority for real-time transcription
                Provider = "directml"
            });

            // Embedding Models
            RegisterDefaultIfMissing(new ModelMetadata
            {
                ModelId = "all-MiniLM-L6-v2",
                Type = ModelType.Embedding,
                RequiresGpu = false,
                SupportsBatching = true,
                MaxBatchSize = 32,
                EstimatedMemoryMb = 100,
                DefaultPriority = 1,
                Provider = "cpu"
            });

            RegisterDefaultIfMissing(new ModelMetadata
            {
                ModelId = "bge-large-en-v1.5",
                Type = ModelType.Embedding,
                RequiresGpu = true,
                SupportsBatching = true,
                MaxBatchSize = 16,
                EstimatedMemoryMb = 500,
                DefaultPriority = 1,
                Provider = "directml"
            });
        }

        /// <summary>
        /// Registers default metadata if not already present
        /// </summary>
        private void RegisterDefaultIfMissing(ModelMetadata metadata)
        {
            _metadata.TryAdd(metadata.ModelId, metadata);
        }

        /// <summary>
        /// Creates default metadata for unknown models
        /// </summary>
        private ModelMetadata CreateDefaultMetadata(string modelId)
        {
            // Infer properties from model ID
            var metadata = new ModelMetadata
            {
                ModelId = modelId,
                Type = InferModelType(modelId),
                RequiresGpu = InferGpuRequirement(modelId),
                SupportsBatching = InferBatchingSupport(modelId),
                MaxBatchSize = InferMaxBatchSize(modelId),
                EstimatedMemoryMb = InferMemoryRequirement(modelId),
                DefaultPriority = 1,
                Provider = "cpu"
            };

            // Cache for future use
            _metadata.TryAdd(modelId, metadata);

            return metadata;
        }

        private ModelType InferModelType(string modelId)
        {
            var lower = modelId.ToLowerInvariant();

            if (lower.Contains("whisper"))
                return ModelType.Whisper;

            if (lower.Contains("embedding") || lower.Contains("bert") || lower.Contains("minilm"))
                return ModelType.Embedding;

            if (lower.Contains("vision") || lower.Contains("clip"))
                return ModelType.Vision;

            return ModelType.LLM;
        }

        private bool InferGpuRequirement(string modelId)
        {
            var lower = modelId.ToLowerInvariant();

            // Small embedding models can run on CPU
            if (lower.Contains("minilm") || lower.Contains("small"))
                return false;

            // Large models need GPU
            if (lower.Contains("70b") || lower.Contains("13b") || lower.Contains("7b"))
                return true;

            // Default to GPU for unknown models
            return true;
        }

        private bool InferBatchingSupport(string modelId)
        {
            var lower = modelId.ToLowerInvariant();
            return lower.Contains("embedding") || lower.Contains("bert") || lower.Contains("minilm");
        }

        private int InferMaxBatchSize(string modelId)
        {
            if (!InferBatchingSupport(modelId))
                return 1;

            var lower = modelId.ToLowerInvariant();

            if (lower.Contains("large"))
                return 16;

            if (lower.Contains("base") || lower.Contains("medium"))
                return 32;

            return 64; // Small models
        }

        private long InferMemoryRequirement(string modelId)
        {
            var lower = modelId.ToLowerInvariant();

            // Parse size from model ID
            if (lower.Contains("70b")) return 40000;
            if (lower.Contains("13b")) return 8000;
            if (lower.Contains("7b")) return 4000;
            if (lower.Contains("3b")) return 2000;

            // Default sizes by type
            var type = InferModelType(modelId);
            return type switch
            {
                ModelType.LLM => 4000,
                ModelType.Embedding => 200,
                ModelType.Whisper => 150,
                ModelType.Vision => 2000,
                _ => 1000
            };
        }

        Task<Shared.Models.ModelMetadata> IModelMetadataService.GetMetadataAsync(string modelId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

 

        Task<List<Shared.Models.ModelMetadata>> IModelMetadataService.GetAllMetadataAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Configuration for model metadata
    /// </summary>
    public class ModelMetadataConfiguration
    {
        public string MetadataFilePath { get; set; } = "models-metadata.json";
        public List<ModelMetadata> Models { get; set; } = new();
    }
}
