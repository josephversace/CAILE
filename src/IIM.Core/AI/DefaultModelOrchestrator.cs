using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Models;
using IIM.Shared.Enums;

using IIM.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using LLama;
using LLama.Common;
using LLama.Native;

namespace IIM.Core.AI
{
    /// <summary>
    /// Production implementation of IModelOrchestrator for managing AI model lifecycle.
    /// Handles model loading/unloading, memory management, and inference coordination.
    /// Supports both ONNX models (via ONNX Runtime with DirectML/CUDA/CPU) and 
    /// GGUF models (via LlamaSharp for efficient LLM inference).
    /// </summary>
    public class DefaultModelOrchestrator : IModelOrchestrator, IDisposable
    {
        #region Fields and Constants

        private readonly ILogger<DefaultModelOrchestrator> _logger;
        private readonly StorageConfiguration _storageConfig;
        private readonly ConcurrentDictionary<string, LoadedModel> _loadedModels = new();
        private readonly ConcurrentDictionary<string, InferenceSession> _onnxSessions = new();
        private readonly ConcurrentDictionary<string, LLamaContext> _llamaContexts = new();
        private readonly ConcurrentDictionary<string, LLamaWeights> _llamaWeights = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private readonly SemaphoreSlim _inferenceLock = new(1, 1);

        // Memory management constants
        private const long MaxMemoryBytes = 120L * 1024 * 1024 * 1024; // 120GB for Framework laptop
        private const long MemoryWarningThreshold = 100L * 1024 * 1024 * 1024; // Warn at 100GB
        private const long MinMemoryBuffer = 2L * 1024 * 1024 * 1024; // Keep 2GB free

        // Model size estimation multipliers (conservative estimates)
        private const float FP32_BYTES_PER_PARAM = 4.0f; // 4 bytes per parameter for FP32
        private const float FP16_BYTES_PER_PARAM = 2.0f; // 2 bytes per parameter for FP16
        private const float INT8_BYTES_PER_PARAM = 1.0f; // 1 byte per parameter for INT8
        private const float OVERHEAD_MULTIPLIER = 1.3f; // 30% overhead for runtime memory

        // Session options cache for reuse
        private readonly ConcurrentDictionary<string, SessionOptions> _sessionOptionsCache = new();

        // Performance metrics
        private long _totalInferenceCount = 0;
        private TimeSpan _totalInferenceTime = TimeSpan.Zero;

        // Disposal tracking
        private bool _disposed = false;

        #endregion

        #region Events

        public event EventHandler<ModelLoadedEventArgs>? ModelLoaded;
        public event EventHandler<ModelUnloadedEventArgs>? ModelUnloaded;
        public event EventHandler<ModelErrorEventArgs>? ModelError;
        public event EventHandler<ResourceThresholdEventArgs>? ResourceThresholdExceeded;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DefaultModelOrchestrator with StorageConfiguration.
        /// Sets up ONNX Runtime environment and validates system capabilities.
        /// </summary>
        public DefaultModelOrchestrator(
            ILogger<DefaultModelOrchestrator> logger,
            StorageConfiguration storageConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageConfig = storageConfig ?? throw new ArgumentNullException(nameof(storageConfig));

            // Ensure all necessary directories exist
            _storageConfig.EnsureDirectoriesExist();

            // Validate ONNX Runtime availability
            ValidateOnnxRuntimeEnvironment();

            // Initialize LlamaSharp native library
            InitializeLlamaSharp();

            _logger.LogInformation(
                "DefaultModelOrchestrator initialized with models path: {ModelsPath}",
                _storageConfig.ModelsPath);
        }

        #endregion

        #region Model Loading

        /// <summary>
        /// Loads a model into memory with full ONNX Runtime or LlamaSharp integration.
        /// Automatically detects format and uses appropriate engine.
        /// </summary>
        public async Task<ModelHandle> LoadModelAsync(
            ModelRequest request,
            IProgress<float>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // Validate request
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.ModelId))
                throw new ArgumentException("ModelId cannot be null or empty", nameof(request));

            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.LogInformation("Loading model {ModelId} of type {Type}",
                    request.ModelId, request.ModelType);

                // Report initial progress
                progress?.Report(0.05f);

                // Check if already loaded
                if (_loadedModels.ContainsKey(request.ModelId))
                {
                    _logger.LogInformation("Model {ModelId} already loaded, returning existing handle",
                        request.ModelId);
                    return _loadedModels[request.ModelId].Handle;
                }

                // Resolve and validate model path
                var modelPath = ResolveModelPath(request);
                if (!ValidateModelPath(modelPath))
                {
                    var error = $"Model file not found at: {modelPath}. Model ID: {request.ModelId}";
                    _logger.LogError(error);
                    throw new FileNotFoundException(error);
                }

                // Report progress - model file found
                progress?.Report(0.15f);

                // Estimate and check memory requirements
                var estimatedMemory = await EstimateModelMemoryAsync(request, modelPath);
                var currentMemory = GetCurrentMemoryUsage();

                _logger.LogInformation(
                    "Memory check - Required: {Required:N0} MB, Current: {Current:N0} MB, Max: {Max:N0} MB",
                    estimatedMemory / (1024 * 1024),
                    currentMemory / (1024 * 1024),
                    MaxMemoryBytes / (1024 * 1024));

                // Check memory availability with buffer
                if (currentMemory + estimatedMemory + MinMemoryBuffer > MaxMemoryBytes)
                {
                    // Try to free memory by unloading least recently used models
                    var freedMemory = await TryFreeMemoryAsync(estimatedMemory, cancellationToken);

                    if (freedMemory < estimatedMemory)
                    {
                        var error = $"Insufficient memory for model {request.ModelId}. " +
                                   $"Required: {estimatedMemory:N0} bytes, Available: {MaxMemoryBytes - currentMemory:N0} bytes";
                        _logger.LogError(error);
                        throw new InsufficientMemoryException(estimatedMemory, MaxMemoryBytes - currentMemory);
                    }
                }

                // Report progress - memory check passed
                progress?.Report(0.25f);

                // Determine model format and create appropriate session
                var modelFormat = DetermineModelFormat(modelPath);
                _logger.LogInformation("Detected model format: {Format} for {ModelId}", modelFormat, request.ModelId);

                object? inferenceEngine = null;
                Dictionary<string, object> metadata;

                if (modelFormat == ModelFormat.ONNX)
                {
                    // Create ONNX Runtime session with appropriate provider
                    var session = await CreateInferenceSessionAsync(modelPath, request, progress, cancellationToken);
                    _onnxSessions[request.ModelId] = session;
                    inferenceEngine = session;
                    metadata = ExtractModelMetadata(session, request);
                }
                else if (modelFormat == ModelFormat.GGUF || modelFormat == ModelFormat.GGML)
                {
                    // Create LlamaSharp context for GGUF/GGML models
                    var context = await CreateLlamaContextAsync(modelPath, request, progress, cancellationToken);
                    _llamaContexts[request.ModelId] = context;
                    inferenceEngine = context;
                    metadata = ExtractLlamaMetadata(context, request);
                }
                else
                {
                    throw new NotSupportedException($"Model format {modelFormat} is not supported");
                }

                // Report progress - session/context created
                progress?.Report(0.85f);

                // Create model handle
                var handle = new ModelHandle
                {
                    ModelId = request.ModelId,
                    SessionId = Guid.NewGuid().ToString(),
                    LoadedAt = DateTimeOffset.UtcNow,
                    MemoryUsage = estimatedMemory,
                    State = ModelState.Ready,
                    Metadata = metadata
                };

                // Store loaded model information
                var loadedModel = new LoadedModel
                {
                    Handle = handle,
                    Request = request,
                    ModelPath = modelPath,
                    RuntimeOptions = new ModelRuntimeOptions
                    {
                        MaxMemory = estimatedMemory,
                        DeviceId = request.DeviceId ?? 0,
                        Priority = request.Priority,
                        ExecutionProvider = DetermineExecutionProvider(request),
                        CustomOptions = request.CustomOptions ?? new Dictionary<string, object>()
                    },
                    Process = Process.GetCurrentProcess(), // Track current process
                    LastAccessed = DateTimeOffset.UtcNow
                };

                // Store in concurrent dictionary
                _loadedModels[request.ModelId] = loadedModel;

                // Report completion
                progress?.Report(1.0f);

                // Raise model loaded event
                ModelLoaded?.Invoke(this, new ModelLoadedEventArgs
                {
                    ModelId = request.ModelId,
                    Type = request.ModelType,
                    MemoryUsage = estimatedMemory,
                    LoadTime = stopwatch.Elapsed,
                    ExecutionProvider = loadedModel.RuntimeOptions.ExecutionProvider
                });

                // Check memory warning threshold
                CheckMemoryThreshold(currentMemory + estimatedMemory);

                _logger.LogInformation(
                    "Model {ModelId} loaded successfully in {Time}ms from {Path}. " +
                    "Memory used: {Memory:N0} MB, Execution Provider: {Provider}, Format: {Format}",
                    request.ModelId,
                    stopwatch.ElapsedMilliseconds,
                    modelPath,
                    estimatedMemory / (1024 * 1024),
                    loadedModel.RuntimeOptions.ExecutionProvider,
                    modelFormat);

                return handle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model {ModelId}", request.ModelId);

                // Raise error event
                ModelError?.Invoke(this, new ModelErrorEventArgs
                {
                    ModelId = request.ModelId,
                    Error = ex.Message,
                    Exception = ex
                });

                throw;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        #endregion

        #region ONNX Runtime Support

        /// <summary>
        /// Creates an ONNX Runtime InferenceSession with appropriate execution providers.
        /// Supports DirectML for AMD GPUs, CUDA for NVIDIA, and CPU fallback.
        /// </summary>
        private async Task<InferenceSession> CreateInferenceSessionAsync(
            string modelPath,
            ModelRequest request,
            IProgress<float>? progress,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report(0.35f);

                    // Create or get cached session options
                    var sessionOptions = GetOrCreateSessionOptions(request);

                    progress?.Report(0.45f);

                    // Determine and add execution providers based on request and system capabilities
                    var executionProvider = DetermineExecutionProvider(request);

                    switch (executionProvider)
                    {
                        case "DirectML":
                            // AMD GPU support via DirectML
                            if (IsDirectMLAvailable())
                            {
                                try
                                {
                                    sessionOptions.AppendExecutionProvider_DML(request.DeviceId ?? 0);
                                    _logger.LogInformation("Using DirectML execution provider for AMD GPU");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to enable DirectML, falling back to CPU");
                                    sessionOptions.AppendExecutionProvider_CPU(0);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("DirectML not available, using CPU fallback");
                                sessionOptions.AppendExecutionProvider_CPU(0);
                            }
                            break;

                        case "CUDA":
                            // NVIDIA GPU support
                            if (IsCudaAvailable())
                            {
                                try
                                {
                                    sessionOptions.AppendExecutionProvider_CUDA(request.DeviceId ?? 0);
                                    _logger.LogInformation("Using CUDA execution provider for NVIDIA GPU");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to enable CUDA, falling back to CPU");
                                    sessionOptions.AppendExecutionProvider_CPU(0);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("CUDA not available, using CPU fallback");
                                sessionOptions.AppendExecutionProvider_CPU(0);
                            }
                            break;

                        case "CPU":
                        default:
                            // CPU execution
                            sessionOptions.AppendExecutionProvider_CPU(0);
                            _logger.LogInformation("Using CPU execution provider");
                            break;
                    }

                    progress?.Report(0.55f);

                    // Create the inference session
                    _logger.LogDebug("Creating InferenceSession for model at {Path}", modelPath);
                    var session = new InferenceSession(modelPath, sessionOptions);

                    progress?.Report(0.75f);

                    // Validate session inputs and outputs
                    ValidateSessionIO(session, request);

                    _logger.LogInformation(
                        "InferenceSession created successfully. Inputs: {InputCount}, Outputs: {OutputCount}",
                        session.InputMetadata.Count,
                        session.OutputMetadata.Count);

                    return session;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create InferenceSession for model at {Path}", modelPath);
                    throw new ModelLoadException($"Failed to create inference session: {ex.Message}", ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Creates or retrieves cached SessionOptions for ONNX Runtime.
        /// </summary>
        private SessionOptions GetOrCreateSessionOptions(ModelRequest request)
        {
            var cacheKey = $"{request.ModelId}_{request.DeviceId}_{request.Priority}";

            return _sessionOptionsCache.GetOrAdd(cacheKey, _ =>
            {
                var options = new SessionOptions();

                // Set optimization level based on priority
                options.GraphOptimizationLevel = request.Priority switch
                {
                    ModelPriority.Realtime => GraphOptimizationLevel.ORT_ENABLE_BASIC,
                    ModelPriority.Balanced => GraphOptimizationLevel.ORT_ENABLE_EXTENDED,
                    ModelPriority.Throughput => GraphOptimizationLevel.ORT_ENABLE_ALL,
                    _ => GraphOptimizationLevel.ORT_ENABLE_EXTENDED
                };

                // Configure threading for optimal performance
                options.IntraOpNumThreads = Environment.ProcessorCount / 2;
                options.InterOpNumThreads = 2;

                // Enable memory pattern optimization
                options.EnableMemoryPattern = true;
                options.EnableCpuMemArena = true;

                // Set execution mode
                options.ExecutionMode = request.Priority == ModelPriority.Realtime
                    ? ExecutionMode.ORT_SEQUENTIAL
                    : ExecutionMode.ORT_PARALLEL;

                _logger.LogDebug(
                    "Created SessionOptions: OptLevel={OptLevel}, IntraThreads={Intra}, InterThreads={Inter}",
                    options.GraphOptimizationLevel,
                    options.IntraOpNumThreads,
                    options.InterOpNumThreads);

                return options;
            });
        }

        #endregion

        #region LlamaSharp/GGUF Support

        /// <summary>
        /// Creates a LlamaSharp context for GGUF/GGML models.
        /// Supports GPU acceleration and various quantization formats.
        /// </summary>
        private async Task<LLamaContext> CreateLlamaContextAsync(
            string modelPath,
            ModelRequest request,
            IProgress<float>? progress,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report(0.35f);

                    // Configure model parameters based on request
                    var modelParams = new ModelParams(modelPath)
                    {
                        ContextSize = (uint)(request.ContextSize ?? 2048),
                        GpuLayerCount = DetermineGpuLayers(request),
                        Seed = (uint)(request.CustomOptions?.GetValueOrDefault("seed") as int? ?? 1337),
                        UseMemorymap = true,
                        UseMemoryLock = false,
                        MainGpu = request.DeviceId ?? 0,
                        SplitMode = GGMLSplitMode.None
                    };

                    _logger.LogDebug(
                        "Creating LLama context with ContextSize={Context}, GpuLayers={Gpu}",
                        modelParams.ContextSize,
                        modelParams.GpuLayerCount);

                    progress?.Report(0.45f);

                    // Load the model weights
                    var weights = LLamaWeights.LoadFromFile(modelParams);

                    // Store weights for potential reuse
                    _llamaWeights[request.ModelId] = weights;

                    progress?.Report(0.65f);

                    // Create context from weights
                    var context = weights.CreateContext(modelParams);

                    progress?.Report(0.75f);

                    _logger.LogInformation(
                        "LLama context created successfully for {ModelId}. " +
                        "Context size: {ContextSize}, GPU layers: {GpuLayers}",
                        request.ModelId,
                        modelParams.ContextSize,
                        modelParams.GpuLayerCount);

                    return context;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create LLama context for model at {Path}", modelPath);
                    throw new ModelLoadException($"Failed to create LLama context: {ex.Message}", ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Determines the number of GPU layers to offload for GGUF models.
        /// </summary>
        private int DetermineGpuLayers(ModelRequest request)
        {
            // Check if GPU offloading is explicitly set
            if (request.GpuLayers.HasValue)
                return request.GpuLayers.Value;

            if (request.CustomOptions?.GetValueOrDefault("gpu_layers") is int explicitLayers)
                return explicitLayers;

            // Auto-determine based on model size and available GPU
            if (!IsGpuAvailable())
            {
                _logger.LogInformation("No GPU available, using CPU for all layers");
                return 0;
            }

            // Estimate layers based on model size
            var modelSize = request.ModelSize?.ToLowerInvariant() ?? "";

            return modelSize switch
            {
                var s when s.Contains("70b") => 35,  // Offload ~50% of 70B model
                var s when s.Contains("30b") || s.Contains("33b") => 30,
                var s when s.Contains("13b") => 40,  // Can offload most of 13B
                var s when s.Contains("7b") => 35,   // Offload all layers of 7B
                var s when s.Contains("3b") => 28,   // Offload all layers of 3B
                _ => 20  // Conservative default
            };
        }

        #endregion

        #region Inference Execution

        /// <summary>
        /// Runs inference using either ONNX Runtime or LlamaSharp based on loaded model.
        /// </summary>
        public async Task<InferenceResult> RunInferenceAsync(
            string modelId,
            object input,
            CancellationToken cancellationToken = default)
        {
            // Validate model is loaded
            if (!_loadedModels.TryGetValue(modelId, out var loadedModel))
            {
                var error = $"Model {modelId} is not loaded. Please load the model first.";
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }

            // Update last accessed time
            loadedModel.LastAccessed = DateTimeOffset.UtcNow;

            // Determine which engine to use
            if (_onnxSessions.TryGetValue(modelId, out var onnxSession))
            {
                return await RunOnnxInferenceAsync(modelId, onnxSession, input, loadedModel, cancellationToken);
            }
            else if (_llamaContexts.TryGetValue(modelId, out var llamaContext))
            {
                return await RunLlamaInferenceAsync(modelId, llamaContext, input, loadedModel, cancellationToken);
            }
            else
            {
                var error = $"No inference engine found for model {modelId}. Model may be corrupted.";
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }
        }

        /// <summary>
        /// Runs inference using ONNX Runtime.
        /// </summary>
        private async Task<InferenceResult> RunOnnxInferenceAsync(
            string modelId,
            InferenceSession session,
            object input,
            LoadedModel loadedModel,
            CancellationToken cancellationToken)
        {
            await _inferenceLock.WaitAsync(cancellationToken);
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Prepare input tensors based on model type and input data
                var inputTensors = PrepareInputTensors(session, input, loadedModel.Request.ModelType);

                _logger.LogDebug(
                    "Running ONNX inference on model {ModelId} with {InputCount} input tensors",
                    modelId,
                    inputTensors.Count);

                // Run inference
                IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results;

                try
                {
                    results = await Task.Run(() =>
                        session.Run(inputTensors),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ONNX inference failed for model {ModelId}", modelId);
                    throw new InferenceException($"Inference failed: {ex.Message}", ex);
                }

                // Process outputs based on model type
                var output = ProcessOutputTensors(results, loadedModel.Request.ModelType);

                stopwatch.Stop();

                // Update metrics
                Interlocked.Increment(ref _totalInferenceCount);
                _totalInferenceTime = _totalInferenceTime.Add(stopwatch.Elapsed);

                // Calculate tokens/throughput
                var (tokensProcessed, tokensPerSecond) = CalculateTokenMetrics(
                    input,
                    output,
                    stopwatch.Elapsed,
                    loadedModel.Request.ModelType);

                var result = new InferenceResult
                {
                    ModelId = modelId,
                    Output = output,
                    InferenceTime = stopwatch.Elapsed,
                    TokensProcessed = tokensProcessed,
                    TokensPerSecond = tokensPerSecond,
                    DeviceUsed = loadedModel.RuntimeOptions.ExecutionProvider,
                    Metadata = new Dictionary<string, object>
                    {
                        ["SessionId"] = loadedModel.Handle.SessionId,
                        ["Engine"] = "ONNX Runtime",
                        ["InputShape"] = GetTensorShapeString(inputTensors.First()),
                        ["OutputType"] = output?.GetType().Name ?? "null"
                    }
                };

                _logger.LogInformation(
                    "ONNX inference completed for model {ModelId} in {Time}ms. " +
                    "Tokens: {Tokens}, TPS: {TPS:F1}",
                    modelId,
                    stopwatch.ElapsedMilliseconds,
                    tokensProcessed,
                    tokensPerSecond);

                // Dispose of input/output tensors
                foreach (var tensor in inputTensors)
                {
                    tensor?.Dispose();
                }
                results?.Dispose();

                return result;
            }
            finally
            {
                _inferenceLock.Release();
            }
        }

        /// <summary>
        /// Runs inference using LlamaSharp for GGUF/GGML models.
        /// </summary>
        private async Task<InferenceResult> RunLlamaInferenceAsync(
            string modelId,
            LLamaContext context,
            object input,
            LoadedModel loadedModel,
            CancellationToken cancellationToken)
        {
            await _inferenceLock.WaitAsync(cancellationToken);
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Convert input to string for text generation
                var prompt = input switch
                {
                    string text => text,
                    InferenceRequest req => req.Prompt,
                    _ => input?.ToString() ?? ""
                };

                _logger.LogDebug("Running LLama inference on model {ModelId} with prompt length {Length}",
                    modelId, prompt.Length);

                // Get inference parameters from request or use defaults
                var inferParams = GetInferenceParams(loadedModel.Request);

                // Create executor for this inference
                var executor = new InteractiveExecutor(context);

                // Run inference
                var output = new System.Text.StringBuilder();
                var tokensProcessed = 0;

                await foreach (var text in executor.InferAsync(prompt, inferParams, cancellationToken))
                {
                    output.Append(text);
                    tokensProcessed++;

                    // Optional: Report progress for long-running inference
                    if (tokensProcessed % 10 == 0)
                    {
                        _logger.LogTrace("Generated {Tokens} tokens so far", tokensProcessed);
                    }
                }

                stopwatch.Stop();

                // Update metrics
                Interlocked.Increment(ref _totalInferenceCount);
                _totalInferenceTime = _totalInferenceTime.Add(stopwatch.Elapsed);

                var tokensPerSecond = tokensProcessed / stopwatch.Elapsed.TotalSeconds;

                var result = new InferenceResult
                {
                    ModelId = modelId,
                    Output = output.ToString(),
                    InferenceTime = stopwatch.Elapsed,
                    TokensProcessed = tokensProcessed,
                    TokensPerSecond = tokensPerSecond,
                    DeviceUsed = loadedModel.RuntimeOptions.ExecutionProvider,
                    Metadata = new Dictionary<string, object>
                    {
                        ["SessionId"] = loadedModel.Handle.SessionId,
                        ["Engine"] = "LlamaSharp",
                        ["PromptLength"] = prompt.Length,
                        ["GeneratedLength"] = output.Length,
                        ["Temperature"] = inferParams.Temperature,
                        ["TopP"] = inferParams.TopP
                    }
                };

                _logger.LogInformation(
                    "LLama inference completed for model {ModelId} in {Time}ms. " +
                    "Generated {Tokens} tokens at {TPS:F1} t/s",
                    modelId,
                    stopwatch.ElapsedMilliseconds,
                    tokensProcessed,
                    tokensPerSecond);

                return result;
            }
            finally
            {
                _inferenceLock.Release();
            }
        }

        #endregion

        #region Model Unloading

        /// <summary>
        /// Unloads a model from memory, freeing all associated resources.
        /// Handles both ONNX and LLama models.
        /// </summary>
        public async Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                if (!_loadedModels.TryRemove(modelId, out var loadedModel))
                {
                    _logger.LogWarning("Model {ModelId} not found in loaded models", modelId);
                    return;
                }

                // Dispose of ONNX session if exists
                if (_onnxSessions.TryRemove(modelId, out var session))
                {
                    session.Dispose();
                    _logger.LogDebug("Disposed ONNX InferenceSession for model {ModelId}", modelId);
                }

                // Dispose of LLama context if exists
                if (_llamaContexts.TryRemove(modelId, out var context))
                {
                    context.Dispose();
                    _logger.LogDebug("Disposed LLama context for model {ModelId}", modelId);
                }

                // Dispose of LLama weights if exists and not shared
                if (_llamaWeights.TryRemove(modelId, out var weights))
                {
                    // Check if weights are shared by other models
                    var isShared = _loadedModels.Values.Any(m =>
                        m.Request.ModelPath == loadedModel.Request.ModelPath &&
                        m.Request.ModelId != modelId);

                    if (!isShared)
                    {
                        weights.Dispose();
                        _logger.LogDebug("Disposed LLama weights for model {ModelId}", modelId);
                    }
                    else
                    {
                        // Put weights back if shared
                        _llamaWeights[modelId] = weights;
                        _logger.LogDebug("Keeping shared LLama weights for model {ModelId}", modelId);
                    }
                }

                // Clear session options cache for this model
                var cacheKeysToRemove = _sessionOptionsCache.Keys
                    .Where(k => k.StartsWith($"{modelId}_"))
                    .ToList();

                foreach (var key in cacheKeysToRemove)
                {
                    if (_sessionOptionsCache.TryRemove(key, out var options))
                    {
                        options.Dispose();
                    }
                }

                // Force garbage collection to reclaim memory immediately
                var memoryBefore = GC.GetTotalMemory(false);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var memoryAfter = GC.GetTotalMemory(false);

                var freedMemory = memoryBefore - memoryAfter;
                _logger.LogInformation(
                    "Model {ModelId} unloaded. Memory freed: {Freed:N0} MB",
                    modelId,
                    freedMemory / (1024 * 1024));

                // Raise model unloaded event
                ModelUnloaded?.Invoke(this, new ModelUnloadedEventArgs
                {
                    ModelId = modelId,
                    MemoryFreed = loadedModel.Handle.MemoryUsage
                });
            }
            finally
            {
                _loadLock.Release();
            }
        }

        #endregion

        #region Helper Methods

        // ... (Include all the helper methods from the previous artifacts)
        // Including: DetermineModelFormat, IsOnnxFormat, IsGgufFormat, IsGgmlFormat,
        // ExtractModelMetadata, ExtractLlamaMetadata, GetInferenceParams,
        // PrepareInputTensors, ProcessOutputTensors, EstimateModelMemoryAsync,
        // GetCurrentMemoryUsage, TryFreeMemoryAsync, etc.

        // I'll include a few key ones here:

        /// <summary>
        /// Determines the format of a model file based on extension and magic bytes.
        /// </summary>
        private ModelFormat DetermineModelFormat(string modelPath)
        {
            var extension = Path.GetExtension(modelPath).ToLowerInvariant();

            // Check by extension first
            switch (extension)
            {
                case ".onnx":
                    return ModelFormat.ONNX;
                case ".gguf":
                    return ModelFormat.GGUF;
                case ".ggml":
                case ".bin":
                    // Check magic bytes for GGML format
                    if (IsGgmlFormat(modelPath))
                        return ModelFormat.GGML;
                    break;
            }

            // Check magic bytes if extension is ambiguous
            if (IsOnnxFormat(modelPath))
                return ModelFormat.ONNX;
            if (IsGgufFormat(modelPath))
                return ModelFormat.GGUF;
            if (IsGgmlFormat(modelPath))
                return ModelFormat.GGML;

            // Default based on extension
            return extension switch
            {
                ".onnx" => ModelFormat.ONNX,
                ".gguf" => ModelFormat.GGUF,
                _ => ModelFormat.Unknown
            };
        }

        /// <summary>
        /// Validates ONNX Runtime environment is properly configured.
        /// </summary>
        private void ValidateOnnxRuntimeEnvironment()
        {
            try
            {
                // Test ONNX Runtime availability
                using var testOptions = new SessionOptions();
                testOptions.AppendExecutionProvider_CPU(0);
                _logger.LogDebug("ONNX Runtime validated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ONNX Runtime validation failed");
                throw new InvalidOperationException("ONNX Runtime is not properly configured", ex);
            }
        }

        /// <summary>
        /// Initializes LlamaSharp native library.
        /// </summary>
        private void InitializeLlamaSharp()
        {
            try
            {
                // Set native library path if needed
                var nativeLibPath = Path.Combine(_storageConfig.ModelsPath, "runtimes");
                if (Directory.Exists(nativeLibPath))
                {
                    NativeLibraryConfig.Instance.WithLibrary(nativeLibPath);
                }

                _logger.LogDebug("LlamaSharp initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LlamaSharp initialization failed - GGUF support may be limited");
            }
        }

        // ... (Include all other helper methods from previous implementation)

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes all resources used by the orchestrator.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            // Unload all models
            var modelIds = _loadedModels.Keys.ToList();
            foreach (var modelId in modelIds)
            {
                UnloadModelAsync(modelId).Wait();
            }

            // Dispose semaphores
            _loadLock?.Dispose();
            _inferenceLock?.Dispose();

            // Clear all collections
            _loadedModels.Clear();
            _onnxSessions.Clear();
            _llamaContexts.Clear();
            _llamaWeights.Clear();
            _sessionOptionsCache.Clear();

            _disposed = true;
            _logger.LogInformation("DefaultModelOrchestrator disposed");
        }

        #endregion
    }

  
   

}