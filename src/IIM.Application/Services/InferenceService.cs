using IIM.Application.Interfaces;
using IIM.Core.AI;
using IIM.Core.Inference;
using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Application.Services
{
    public class InferenceService : IInferenceService
    {
        private readonly IInferencePipeline _pipeline;
        private readonly ILogger<InferenceService> _logger;
        private readonly IModelOrchestrator _orchestrator;
        private DeviceInfo? _cachedDeviceInfo;

        public InferenceService(
            IInferencePipeline pipeline,
            IModelOrchestrator orchestrator,
            ILogger<InferenceService> logger)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> InferAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var request = new InferencePipelineRequest
            {
                ModelId = "default",
                Input = prompt
            };

            return await _pipeline.ExecuteAsync<string>(request, cancellationToken);
        }

        public async Task<T> GenerateAsync<T>(string modelId, object input, CancellationToken cancellationToken = default)
        {
            var request = new InferencePipelineRequest
            {
                ModelId = modelId,
                Input = input
            };

            return await _pipeline.ExecuteAsync<T>(request, cancellationToken);
        }

        /// <summary>
        /// Transcribe audio using Whisper model
        /// Returns the strong TranscriptionResult from Models/Analysis.cs
        /// </summary>
        public async Task<TranscriptionResult> TranscribeAudioAsync(
            string audioPath,
            string language = "en",
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting audio transcription for {AudioPath}", audioPath);

            // Ensure Whisper model is loaded - using correct ModelType enum value
            var modelHandle = await _orchestrator.LoadModelAsync(new ModelRequest
            {
                ModelId = "whisper-base",
                ModelPath = GetModelPath("whisper-base"),
                ModelType = ModelType.Whisper,  // Using existing enum value
                ModelSize = "base"
            }, null, cancellationToken);


            var request = new InferencePipelineRequest
            {
                ModelId = "whisper-base",
                Input = audioPath,
                Parameters = new Dictionary<string, object>
                {
                    ["language"] = language,
                    ["task"] = "transcribe",
                    ["return_timestamps"] = true,
                    ["return_segments"] = true
                }
            };

            try
            {
                var result = await _pipeline.ExecuteAsync<TranscriptionResult>(request, cancellationToken);

                // Enrich with processing metadata
                result.Metadata["model"] = "whisper-base";
                result.Metadata["device"] = await GetDeviceTypeAsync();

                _logger.LogInformation("Transcription completed with {SegmentCount} segments", result.Segments.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transcribe audio");
                throw;
            }
        }

        /// <summary>
        /// Search for similar images using CLIP model
        /// Returns ImageSearchResults that Investigation.razor expects
        /// </summary>
        public async Task<ImageSearchResults> SearchImagesAsync(
            byte[] imageData,
            int topK = 5,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting image search for top {TopK} results", topK);

            // Ensure CLIP model is loaded - using correct ModelType enum value
            var modelHandle = await _orchestrator.LoadModelAsync(new ModelRequest
            {
                ModelId = "clip-vit-base",
                ModelPath = GetModelPath("clip-vit-base"),
                ModelType = ModelType.CLIP,  // Using existing enum value
                ModelSize = "base"
            }, null, cancellationToken);

            var request = new InferencePipelineRequest
            {
                ModelId = "clip-vit-base",
                Input = imageData,
                Parameters = new Dictionary<string, object>
                {
                    ["mode"] = "image_search",
                    ["top_k"] = topK,
                    ["threshold"] = 0.7f,
                    ["return_embeddings"] = true
                }
            };

            try
            {
                // Get the image analysis which includes similar images
                var analysis = await _pipeline.ExecuteAsync<ImageAnalysisResult>(request, cancellationToken);

                // Convert ImageAnalysisResult to ImageSearchResults for UI compatibility
                var searchResults = new ImageSearchResults
                {
                    Matches = analysis.SimilarImages.Select(sim => new ImageMatch
                    {
                        ImagePath = sim.FileName,
                        Score = (float)sim.Similarity,
                        Metadata = new Dictionary<string, string>
                        {
                            ["EvidenceId"] = sim.EvidenceId,
                            ["FileName"] = sim.FileName,
                            ["Similarity"] = sim.Similarity.ToString("F3")
                        }
                    }).ToList(),
                    QueryProcessingTime = TimeSpan.FromMilliseconds(100), // Would come from actual timing
                    TotalImagesSearched = analysis.SimilarImages.Count
                };

                _logger.LogInformation("Found {MatchCount} similar images", searchResults.Matches.Count);

                return searchResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search images");
                throw;
            }
        }

        /// <summary>
        /// Query documents using RAG pipeline
        /// Uses the existing RagResponse model from InferenceModels.cs
        /// </summary>
        public async Task<RagResponse> QueryDocumentsAsync(
            string query,
            string collection = "default",
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Querying documents in collection {Collection}", collection);

            // RAG pipeline uses embedding model + LLM model
            // Using correct ModelType enum values
            var embedderHandle = await _orchestrator.LoadModelAsync(new ModelRequest
            {
                ModelId = "bge-base-en",
                ModelPath = GetModelPath("bge-base-en"),
                ModelType = ModelType.Embedding,  // Using existing enum value
                ModelSize = "base"
            }, null, cancellationToken);

            var llmHandle = await _orchestrator.LoadModelAsync(new ModelRequest
            {
                ModelId = "llama-3.1-8b",
                ModelPath = GetModelPath("llama-3.1-8b"),
                ModelType = ModelType.LLM,  // Using existing enum value (LLM not TextGeneration)
                Quantization = "Q4_K_M",
                ContextSize = 8192
            }, null, cancellationToken);

            var request = new InferencePipelineRequest
            {
                ModelId = "rag-pipeline",
                Input = query,
                Parameters = new Dictionary<string, object>
                {
                    ["collection"] = collection,
                    ["max_sources"] = 5,
                    ["min_relevance"] = 0.6f,
                    ["embedder_model"] = "bge-base-en",
                    ["llm_model"] = "llama-3.1-8b",
                    ["temperature"] = 0.7f
                }
            };

            try
            {
                return await _pipeline.ExecuteAsync<RagResponse>(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query documents");
                throw;
            }
        }

        /// <summary>
        /// Get device information and capabilities
        /// Uses the existing DeviceInfo model from InferenceModels.cs
        /// </summary>
        public async Task<DeviceInfo> GetDeviceInfo()
        {
            // Cache device info as it doesn't change during runtime
            if (_cachedDeviceInfo != null)
            {
                return _cachedDeviceInfo;
            }

            try
            {
                // Get stats from the orchestrator
                var stats = await _orchestrator.GetStatsAsync();

                _cachedDeviceInfo = await Task.Run(() =>
                {
                    var info = new DeviceInfo
                    {
                        DeviceType = GetDeviceType(),
                        DeviceName = GetDeviceName(),
                        MemoryAvailable = stats.AvailableMemory,
                        MemoryTotal = GetTotalMemory(),
                        SupportsDirectML = CheckDirectMLSupport(),
                        SupportsROCm = CheckROCmSupport()
                    };

                    _logger.LogInformation("Device info: {DeviceType} with {MemoryGB}GB RAM",
                        info.DeviceType,
                        info.MemoryTotal / (1024 * 1024 * 1024));

                    return info;
                });

                return _cachedDeviceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get device info");
                // Return fallback info
                return new DeviceInfo
                {
                    DeviceType = "CPU",
                    DeviceName = "Unknown",
                    MemoryAvailable = 0,
                    MemoryTotal = 0,
                    SupportsDirectML = false,
                    SupportsROCm = false
                };
            }
        }

        /// <summary>
        /// Check if GPU acceleration is available
        /// </summary>
        public async Task<bool> IsGpuAvailable()
        {
            var deviceInfo = await GetDeviceInfo();
            return deviceInfo.SupportsDirectML || deviceInfo.SupportsROCm;
        }

        // Private helper methods
        private string GetModelPath(string modelId)
        {
            // In production, this would resolve from configuration
            var modelsBasePath = Environment.GetEnvironmentVariable("IIM_MODELS_PATH")
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");

            return Path.Combine(modelsBasePath, modelId);
        }

        private async Task<string> GetDeviceTypeAsync()
        {
            var deviceInfo = await GetDeviceInfo();
            return deviceInfo.DeviceType;
        }

        private string GetDeviceType()
        {
            // Check for GPU availability
            if (CheckDirectMLSupport() || CheckROCmSupport())
            {
                return "GPU";
            }
            return "CPU";
        }

        private string GetDeviceName()
        {
            // Platform-specific device detection
            if (CheckROCmSupport())
            {
                // For Framework laptop with AMD GPU
                return "AMD Radeon (ROCm)";
            }
            if (CheckDirectMLSupport())
            {
                return "DirectML Compatible Device";
            }
            return $"{Environment.ProcessorCount} Core CPU";
        }

        private long GetTotalMemory()
        {
            // Framework laptop has 128GB RAM as per requirements
            // In production, use proper system APIs
            const long frameworkLaptopRam = 128L * 1024 * 1024 * 1024;

            if (Environment.GetEnvironmentVariable("IIM_TOTAL_MEMORY") is string memStr
                && long.TryParse(memStr, out var configuredMem))
            {
                return configuredMem;
            }

            return frameworkLaptopRam;
        }

        private bool CheckDirectMLSupport()
        {
            // Check for DirectML support on Windows
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // Check if DirectML.dll exists in system
                    var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                    var directMlPath = Path.Combine(systemPath, "DirectML.dll");

                    return File.Exists(directMlPath) ||
                           Environment.GetEnvironmentVariable("DIRECTML_AVAILABLE") == "1";
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        private bool CheckROCmSupport()
        {
            // Check for ROCm support (WSL2 or Linux)
            if (OperatingSystem.IsLinux() || IsWSL())
            {
                try
                {
                    // Check for ROCm installation
                    return Directory.Exists("/opt/rocm") ||
                           Environment.GetEnvironmentVariable("ROCM_PATH") != null;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        private bool IsWSL()
        {
            // Detect if running in WSL
            try
            {
                if (OperatingSystem.IsLinux())
                {
                    var procVersion = File.ReadAllText("/proc/version");
                    return procVersion.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                           procVersion.Contains("WSL", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }

            return false;
        }
    }
}
