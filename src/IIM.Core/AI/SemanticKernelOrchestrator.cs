// src/IIM.Core/AI/SemanticKernelOrchestrator.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using IIM.Core.Models;
using IIM.Core.Services.Configuration;
using IIM.Infrastructure.Storage;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Core.AI
{
    /// <summary>
    /// Semantic Kernel-based orchestrator for AI model management
    /// Replaces the existing ModelOrchestrator with SK integration
    /// </summary>
    public class SemanticKernelOrchestrator : IModelOrchestrator
    {
        private readonly ILogger<SemanticKernelOrchestrator> _logger;
        private readonly IModelConfigurationTemplateService? _templateService;
        private readonly Dictionary<string, Kernel> _kernels = new();
        private readonly Dictionary<string, ModelHandle> _loadedModels = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);

        public SemanticKernelOrchestrator(
            ILogger<SemanticKernelOrchestrator> logger,
            IModelConfigurationTemplateService? templateService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templateService = templateService;
        }

        // Events
        public event EventHandler<ModelLoadedEventArgs>? ModelLoaded;
        public event EventHandler<ModelUnloadedEventArgs>? ModelUnloaded;
        public event EventHandler<ModelErrorEventArgs>? ModelError;
        public event EventHandler<ResourceThresholdEventArgs>? ResourceThresholdExceeded;

        /// <summary>
        /// Loads a model using Semantic Kernel
        /// </summary>
        public async Task<ModelHandle> LoadModelAsync(
            ModelRequest request,
            IProgress<float>? progress = null,
            CancellationToken cancellationToken = default)
        {
            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("Loading model {ModelId} with Semantic Kernel", request.ModelId);
                progress?.Report(0.1f);

                // Build kernel based on model type
                var kernelBuilder = Kernel.CreateBuilder();

                // Configure based on provider
                switch (request.Provider?.ToLowerInvariant())
                {
                    case "ollama":
                        // For local Ollama models
                        kernelBuilder.AddOpenAIChatCompletion(
                            modelId: request.ModelId,
                            endpoint: new Uri("http://localhost:11434"),
                            apiKey: "ollama");
                        break;

                    case "directml":
                    case "onnx":
                        // For ONNX models with DirectML
                        // Note: This requires custom connector implementation
                        kernelBuilder.Services.AddSingleton<ITextGenerationService>(sp =>
                            new DirectMLTextGenerationService(request.ModelPath, _logger));
                        break;

                    default:
                        // Default to CPU/mock for now
                        kernelBuilder.Services.AddSingleton<ITextGenerationService>(sp =>
                            new MockTextGenerationService(request.ModelId, _logger));
                        break;
                }

                progress?.Report(0.5f);

                // Build the kernel
                var kernel = kernelBuilder.Build();
                _kernels[request.ModelId] = kernel;

                // Create model handle
                var handle = new ModelHandle
                {
                    ModelId = request.ModelId,
                    SessionId = Guid.NewGuid().ToString(),
                    Provider = request.Provider ?? "default",
                    Type = request.ModelType,
                    MemoryUsage = EstimateMemoryUsage(request),
                    LoadedAt = DateTimeOffset.UtcNow
                };

                _loadedModels[request.ModelId] = handle;
                progress?.Report(1.0f);

                // Raise event
                ModelLoaded?.Invoke(this, new ModelLoadedEventArgs
                {
                    ModelId = request.ModelId,
                    LoadTime = TimeSpan.FromSeconds(1),
                    MemoryUsage = handle.MemoryUsage
                });

                _logger.LogInformation("Model {ModelId} loaded successfully", request.ModelId);
                return handle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model {ModelId}", request.ModelId);
                ModelError?.Invoke(this, new ModelErrorEventArgs
                {
                    ModelId = request.ModelId,
                    Error = ex.Message
                });
                throw;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        /// <summary>
        /// Performs inference using the loaded SK kernel
        /// </summary>
        public async Task<InferenceResult> InferAsync(
            string modelId,
            object input,
            CancellationToken ct = default)
        {
            if (!_kernels.TryGetValue(modelId, out var kernel))
            {
                throw new ModelNotLoadedException(modelId);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("Starting inference for model {ModelId}", modelId);

                // Convert input to string if needed
                var prompt = input switch
                {
                    string s => s,
                    InferencePipelineRequest ipr => ipr.Input?.ToString() ?? "",
                    _ => input?.ToString() ?? ""
                };

                // Execute with SK
                var result = await kernel.InvokePromptAsync(
                    prompt,
                    cancellationToken: ct);

                stopwatch.Stop();

                return new InferenceResult
                {
                    ModelId = modelId,
                    Output = result.GetValue<object>() ?? result.ToString(),
                    InferenceTime = stopwatch.Elapsed,
                    TokensProcessed = EstimateTokens(prompt),
                    TokensPerSecond = EstimateTokens(prompt) / stopwatch.Elapsed.TotalSeconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inference failed for model {ModelId}", modelId);
                throw new InferenceException($"Inference failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Unloads a model and frees resources
        /// </summary>
        public Task<bool> UnloadModelAsync(string modelId, CancellationToken ct = default)
        {
            if (_kernels.Remove(modelId) && _loadedModels.Remove(modelId))
            {
                _logger.LogInformation("Model {ModelId} unloaded", modelId);

                ModelUnloaded?.Invoke(this, new ModelUnloadedEventArgs
                {
                    ModelId = modelId,
                    Reason = "Requested"
                });

                // Force garbage collection to free memory
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets statistics about loaded models
        /// </summary>
        public Task<ModelStats> GetStatsAsync()
        {
            var totalMemory = _loadedModels.Values.Sum(m => m.MemoryUsage);
            var availableMemory = 128L * 1024 * 1024 * 1024 - totalMemory; // 128GB total

            var modelInfos = _loadedModels.ToDictionary(
                kvp => kvp.Key,
                kvp => new ModelInfo
                {
                    ModelId = kvp.Key,
                    Type = kvp.Value.Type,
                    MemoryUsage = kvp.Value.MemoryUsage,
                    AccessCount = 0,
                    LastAccessed = DateTimeOffset.UtcNow,
                    LoadTime = TimeSpan.Zero,
                    AverageTokensPerSecond = 0
                });

            return Task.FromResult(new ModelStats
            {
                LoadedModels = _loadedModels.Count,
                TotalMemoryUsage = totalMemory,
                AvailableMemory = availableMemory,
                Models = modelInfos
            });
        }

        /// <summary>
        /// Gets available models from templates and file system
        /// </summary>
        public async Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            var models = new List<ModelConfiguration>();

            try
            {
                // 1. Get models from templates (this is the primary source)
                if (_templateService != null)
                {
                    var templates = await _templateService.GetTemplatesAsync(null, cancellationToken);

                    foreach (var template in templates)
                    {
                        foreach (var (capability, modelConfig) in template.Models)
                        {
                            // Add primary model
                            var config = new ModelConfiguration
                            {
                                ModelId = modelConfig.ModelId,
                                Provider = DetermineProvider(modelConfig.ModelId),
                                Type = DetermineModelType(modelConfig.ModelId),
                                Status = ModelStatus.Available,
                                Parameters = modelConfig.Parameters,
                                MemoryUsage = modelConfig.MinMemoryRequired
                            };

                            if (!models.Any(m => m.ModelId == config.ModelId))
                            {
                                models.Add(config);
                            }

                            // Add alternative models
                            foreach (var altModelId in modelConfig.AlternativeModels)
                            {
                                var altConfig = new ModelConfiguration
                                {
                                    ModelId = altModelId,
                                    Provider = DetermineProvider(altModelId),
                                    Type = DetermineModelType(altModelId),
                                    Status = ModelStatus.Available
                                };

                                if (!models.Any(m => m.ModelId == altConfig.ModelId))
                                {
                                    models.Add(altConfig);
                                }
                            }
                        }
                    }
                }

                // 2. Discover ONNX models from file system
                var modelsPath = Environment.GetEnvironmentVariable("IIM_MODELS_PATH")
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");

                if (Directory.Exists(modelsPath))
                {
                    var onnxFiles = Directory.GetFiles(modelsPath, "*.onnx", SearchOption.AllDirectories);
                    foreach (var file in onnxFiles)
                    {
                        var modelId = Path.GetFileNameWithoutExtension(file);
                        if (!models.Any(m => m.ModelId == modelId))
                        {
                            models.Add(new ModelConfiguration
                            {
                                ModelId = modelId,
                                Provider = "ONNX",
                                Type = DetermineModelTypeFromPath(file),
                                Status = ModelStatus.Available
                                // Removed ModelPath as it doesn't exist in ModelConfiguration
                            });
                        }
                    }
                }

                // 3. Check Ollama for available models
                if (await IsOllamaAvailable())
                {
                    var ollamaModels = await GetOllamaModelsAsync();
                    foreach (var modelId in ollamaModels)
                    {
                        if (!models.Any(m => m.ModelId == modelId))
                        {
                            models.Add(new ModelConfiguration
                            {
                                ModelId = modelId,
                                Provider = "Ollama",
                                Type = ModelType.LLM,
                                Status = ModelStatus.Available
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering available models");
            }

            return models;
        }

        // Other required interface methods
        public Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_loadedModels.ContainsKey(modelId));
        }

        public Task<List<ModelConfiguration>> GetLoadedModelsAsync(CancellationToken cancellationToken = default)
        {
            var configs = _loadedModels.Values.Select(h => new ModelConfiguration
            {
                ModelId = h.ModelId,
                Provider = h.Provider,
                Type = h.Type,
                Status = ModelStatus.Loaded,
                MemoryUsage = h.MemoryUsage
            }).ToList();

            return Task.FromResult(configs);
        }

        public Task<ModelConfiguration?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (_loadedModels.TryGetValue(modelId, out var handle))
            {
                return Task.FromResult<ModelConfiguration?>(new ModelConfiguration
                {
                    ModelId = modelId,
                    Provider = handle.Provider,
                    Type = handle.Type,
                    Status = ModelStatus.Loaded,
                    MemoryUsage = handle.MemoryUsage
                });
            }

            return Task.FromResult<ModelConfiguration?>(null);
        }

        public Task<bool> UpdateModelParametersAsync(string modelId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            // SK doesn't support runtime parameter updates easily
            _logger.LogWarning("Parameter updates not yet implemented for SK");
            return Task.FromResult(false);
        }

        // Additional required methods from interface
        public Task<bool> DownloadModelAsync(string modelId, string source, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Download not implemented in SK orchestrator");
            return Task.FromResult(false);
        }

        public Task<bool> DeleteModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            return UnloadModelAsync(modelId, cancellationToken);
        }

        public Task<long> GetModelSizeAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (_loadedModels.TryGetValue(modelId, out var handle))
            {
                return Task.FromResult(handle.MemoryUsage);
            }
            return Task.FromResult(0L);
        }

        public Task<GpuStats> GetGpuStatsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GpuStats
            {
                DeviceName = "DirectML Device",
                TotalMemory = 128L * 1024 * 1024 * 1024,
                UsedMemory = _loadedModels.Values.Sum(m => m.MemoryUsage),
                AvailableMemory = 128L * 1024 * 1024 * 1024 - _loadedModels.Values.Sum(m => m.MemoryUsage),
                IsDirectMLAvailable = CheckDirectMLSupport(),
                IsROCmAvailable = false
            });
        }

        public Task<ModelResourceUsage> GetModelResourceUsageAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (_loadedModels.TryGetValue(modelId, out var handle))
            {
                return Task.FromResult(new ModelResourceUsage
                {
                    ModelId = modelId,
                    MemoryBytes = handle.MemoryUsage,
                    VramBytes = 0,
                    CpuPercent = 0,
                    GpuPercent = 0,
                    ActiveSessions = 1,
                    Uptime = DateTimeOffset.UtcNow - handle.LoadedAt
                });
            }

            return Task.FromResult(new ModelResourceUsage { ModelId = modelId });
        }

        public Task<bool> OptimizeMemoryAsync(CancellationToken cancellationToken = default)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return Task.FromResult(true);
        }

        public Task<long> GetTotalMemoryUsageAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_loadedModels.Values.Sum(m => m.MemoryUsage));
        }

        // Helper methods
        private string DetermineProvider(string modelId)
        {
            if (modelId.Contains(".onnx"))
                return "ONNX";
            if (modelId.Contains(":"))
                return "Ollama";
            return "Default";
        }

        private ModelType DetermineModelType(string modelId)
        {
            var lower = modelId.ToLowerInvariant();
            if (lower.Contains("whisper"))
                return ModelType.Whisper;
            if (lower.Contains("clip"))
                return ModelType.CLIP;
            if (lower.Contains("embed") || lower.Contains("bge") || lower.Contains("minilm"))
                return ModelType.Embedding;
            return ModelType.LLM;
        }

        private ModelType DetermineModelTypeFromPath(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            return DetermineModelType(fileName);
        }

        private async Task<bool> IsOllamaAvailable()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = await client.GetAsync("http://localhost:11434/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<List<string>> GetOllamaModelsAsync()
        {
            var models = new List<string>();
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync("http://localhost:11434/api/tags");
                // Parse JSON response to get model list
                // This is simplified - you'd use proper JSON parsing
                _logger.LogInformation("Retrieved Ollama models");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Ollama models");
            }
            return models;
        }

        private long EstimateMemoryUsage(ModelRequest request)
        {
            // Estimate based on model type and size
            return request.ModelType switch
            {
                ModelType.LLM => 4L * 1024 * 1024 * 1024, // 4GB
                ModelType.Whisper => 2L * 1024 * 1024 * 1024, // 2GB
                ModelType.CLIP => 1L * 1024 * 1024 * 1024, // 1GB
                ModelType.Embedding => 500L * 1024 * 1024, // 500MB
                _ => 1L * 1024 * 1024 * 1024 // 1GB default
            };
        }

        private int EstimateTokens(string text)
        {
            // Rough estimate: 1 token per 4 characters
            return text.Length / 4;
        }

        private bool CheckDirectMLSupport()
        {
            if (OperatingSystem.IsWindows())
            {
                var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                return File.Exists(Path.Combine(systemPath, "DirectML.dll"));
            }
            return false;
        }
    }

    // Mock text generation service for testing
    internal class MockTextGenerationService : ITextGenerationService
    {
        private readonly string _modelId;
        private readonly ILogger _logger;

        public MockTextGenerationService(string modelId, ILogger logger)
        {
            _modelId = modelId;
            _logger = logger;
        }

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
            string prompt,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            return new List<TextContent> { new($"Mock response from {_modelId}: {prompt}") };
        }

        public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
            string prompt,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            yield return new StreamingTextContent($"Mock streaming response from {_modelId}: {prompt}");
        }
    }

    // DirectML text generation service (stub for now)
    internal class DirectMLTextGenerationService : ITextGenerationService
    {
        private readonly string _modelPath;
        private readonly ILogger _logger;

        public DirectMLTextGenerationService(string modelPath, ILogger logger)
        {
            _modelPath = modelPath;
            _logger = logger;
        }

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
            string prompt,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            // TODO: Implement actual DirectML/ONNX inference
            await Task.Delay(200, cancellationToken);
            return new List<TextContent> { new($"DirectML response for: {prompt}") };
        }

        public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
            string prompt,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(200, cancellationToken);
            yield return new StreamingTextContent($"DirectML streaming response for: {prompt}");
        }
    }

    // Event args classes
    public class InferenceException : Exception
    {
        public InferenceException(string message) : base(message) { }
        public InferenceException(string message, Exception inner) : base(message, inner) { }
    }
}