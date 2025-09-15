using IIM.Core.AI;
using IIM.Core.Collections; 
using IIM.Shared.Mediator;
using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using LLama.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Diagnostics; // For Stopwatch
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Threading.Channels; // For Channel<T>
using InsufficientMemoryException = IIM.Core.Models.InsufficientMemoryException;



namespace IIM.Application.Inference
{
    /// <summary>
    /// Configuration for the inference pipeline
    /// </summary>
    public class InferencePipelineConfiguration
    {
        public int MaxGpuConcurrency { get; set; } = 2;
        public int MaxCpuConcurrency { get; set; } = 4;
        public int QueueCapacity { get; set; } = 1000;
        public int QueueTimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public int MetricsWindowSize { get; set; } = 10000;
        public int MetricsRetentionMinutes { get; set; } = 5;
        public bool PriorityAgingEnabled { get; set; } = true;
        public int PriorityAgingIntervalMs { get; set; } = 5000;
        public int PriorityAgingThresholdSeconds { get; set; } = 10;
        public bool EnableBackpressure { get; set; } = true;
        public int BackpressureThreshold { get; set; } = 900; // 90% of queue capacity
    }


    /// <summary>
    /// Production-ready inference pipeline with advanced queue management and monitoring
    /// </summary>
    public sealed class InferencePipeline : IInferencePipeline, IDisposable
    {
        private readonly ILogger<InferencePipeline> _logger;
        private readonly IModelOrchestrator _orchestrator;
        private readonly IModelConfigurationService _ModelConfiguration;
        private readonly IModelParameterSetRepository _modelParams;
        private readonly IMediator? _mediator;
        private readonly InferencePipelineConfiguration _config;
        private readonly IOnnxRuntimeManager _onnxManager;
        private readonly ILlamaSharpManager _llamaManager;
        private readonly IDirectMLDeviceManager _directMLDeviceManager;
        

        // Priority queues
        private readonly Channel<QueuedRequest> _highPriorityQueue;
        private readonly Channel<QueuedRequest> _normalPriorityQueue;
        private readonly Channel<QueuedRequest> _lowPriorityQueue;
        private readonly ConcurrentDictionary<string, QueuedRequest> _pendingRequests = new();

        // Resource management
        private readonly SemaphoreSlim _gpuSemaphore;
        private readonly SemaphoreSlim _cpuSemaphore;
        private readonly CancellationTokenSource _shutdownTokenSource = new();
        private readonly List<Task> _processingTasks = new();
        private Task? _priorityAgingTask;
        private Task? _metricsCleanupTask;

        // Metrics with rolling window
        private readonly CircularBuffer<MetricEntry> _metricsBuffer;
        private readonly Timer _metricsCleanupTimer;
        private long _totalRequests;
        private long _completedRequests;
        private long _failedRequests;
        private long _rejectedRequests;
        private readonly ConcurrentDictionary<string, long> _requestsByModel = new();

        // OpenTelemetry instrumentation
        private readonly ActivitySource _activitySource = new("IIM.InferencePipeline", "1.0.0");
        private readonly Meter _meter = new("IIM.InferencePipeline", "1.0.0");
        private readonly Counter<long> _requestCounter;
        private readonly Counter<long> _errorCounter;
        private readonly Histogram<double> _latencyHistogram;
        private readonly UpDownCounter<int> _queueDepthCounter;

        /// <summary>
        /// Initializes the production-ready inference pipeline
        /// </summary>
        public InferencePipeline(
            ILogger<InferencePipeline> logger,
            IModelOrchestrator orchestrator,
            IModelConfigurationService ModelConfiguration,
            IModelParameterSetRepository modelParameterSetRepository,
            IOptions<InferencePipelineConfiguration> config,
            IOnnxRuntimeManager onnxManager,
            ILlamaSharpManager llamaSharp,
            IDirectMLDeviceManager directMLDeviceManager,
            IMediator? mediator = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _ModelConfiguration = ModelConfiguration ?? throw new ArgumentNullException(nameof(ModelConfiguration));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _mediator = mediator;
            _onnxManager = onnxManager;
            _llamaManager = llamaSharp;
            _directMLDeviceManager = directMLDeviceManager;
            _modelParams = modelParameterSetRepository ?? throw new ArgumentNullException(nameof(modelParameterSetRepository));

            // Initialize metrics
            _metricsBuffer = new CircularBuffer<MetricEntry>(_config.MetricsWindowSize);
            _requestCounter = _meter.CreateCounter<long>("inference.requests", "requests", "Total inference requests");
            _errorCounter = _meter.CreateCounter<long>("inference.errors", "errors", "Total inference errors");
            _latencyHistogram = _meter.CreateHistogram<double>("inference.latency", "ms", "Inference latency");
            _queueDepthCounter = _meter.CreateUpDownCounter<int>("inference.queue.depth", "requests", "Queue depth");

            // Create priority channels
            var options = new BoundedChannelOptions(_config.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };

            _highPriorityQueue = Channel.CreateBounded<QueuedRequest>(options);
            _normalPriorityQueue = Channel.CreateBounded<QueuedRequest>(options);
            _lowPriorityQueue = Channel.CreateBounded<QueuedRequest>(options);

            // Resource semaphores
            _gpuSemaphore = new SemaphoreSlim(_config.MaxGpuConcurrency, _config.MaxGpuConcurrency);
            _cpuSemaphore = new SemaphoreSlim(_config.MaxCpuConcurrency, _config.MaxCpuConcurrency);

            // Start processing tasks
            for (int i = 0; i < _config.MaxGpuConcurrency + _config.MaxCpuConcurrency; i++)
            {
                var task = Task.Run(() => ProcessQueuesAsync(_shutdownTokenSource.Token));
                _processingTasks.Add(task);
            }

            // Start priority aging if enabled
            if (_config.PriorityAgingEnabled)
            {
                _priorityAgingTask = Task.Run(() => AgePrioritiesAsync(_shutdownTokenSource.Token));
            }

            // Start metrics cleanup task
            _metricsCleanupTask = Task.Run(() => CleanupMetricsAsync(_shutdownTokenSource.Token));

            // Start metrics cleanup timer
            _metricsCleanupTimer = new Timer(
                _ => CleanupOldMetrics(),
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1));

            _logger.LogInformation("InferencePipeline initialized: GPU={GpuSlots}, CPU={CpuSlots}, QueueCapacity={Capacity}",
                _config.MaxGpuConcurrency, _config.MaxCpuConcurrency, _config.QueueCapacity);
        }

        /// <summary>
        /// Executes a single inference request with timeout, mapping, and backpressure handling
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            InferencePipelineRequest request,
            Func<InferenceResult, T>? converter = null,
            CancellationToken ct = default)
        {
            // All pipeline and notification logic stays as-is
            using var activity = _activitySource.StartActivity("ExecuteInference");
            activity?.SetTag("model.id", request.ModelId);
            activity?.SetTag("priority", request.Priority);

            // Backpressure, queue setup, etc (same as before)
            if (_config.EnableBackpressure)
            {
                var totalQueued = _highPriorityQueue.Reader.Count +
                                 _normalPriorityQueue.Reader.Count +
                                 _lowPriorityQueue.Reader.Count;

                if (totalQueued > _config.BackpressureThreshold)
                {
                    Interlocked.Increment(ref _rejectedRequests);
                    _errorCounter.Add(1, new KeyValuePair<string, object?>("error.type", "backpressure"));
                    _logger.LogWarning("Request rejected due to backpressure. Queue depth: {QueueDepth}", totalQueued);
                    throw new InferencePipelineException($"System overloaded. Queue depth: {totalQueued}");
                }
            }

            Interlocked.Increment(ref _totalRequests);
            _requestCounter.Add(1, new KeyValuePair<string, object?>("model", request.ModelId));
            _requestsByModel.AddOrUpdate(request.ModelId, 1, (_, count) => count + 1);

            var queuedRequest = new QueuedRequest
            {
                Request = request,
                Priority = await DeterminePriorityAsync(request),
                QueuedAt = DateTimeOffset.UtcNow,
                CompletionSource = new TaskCompletionSource<InferenceResult>(TaskCreationOptions.RunContinuationsAsynchronously),
                CancellationToken = ct
            };

            _pendingRequests[queuedRequest.Id] = queuedRequest;
            _queueDepthCounter.Add(1);

            CancellationTokenSource? timeoutCts = null;

            try
            {
                var channel = queuedRequest.Priority switch
                {
                    Priority.High => _highPriorityQueue,
                    Priority.Low => _lowPriorityQueue,
                    _ => _normalPriorityQueue
                };

                timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.QueueTimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                var queued = await channel.Writer.WaitToWriteAsync(linkedCts.Token);
                if (!queued)
                {
                    throw new InferencePipelineException("Failed to queue request - timeout or cancellation");
                }

                await channel.Writer.WriteAsync(queuedRequest, linkedCts.Token);

                _logger.LogDebug("Request {RequestId} queued with {Priority} priority for model {ModelId}",
                    queuedRequest.Id, queuedRequest.Priority, request.ModelId);

                if (_mediator != null)
                {
                    await _mediator.Publish(new InferenceQueuedNotification
                    {
                        RequestId = queuedRequest.Id,
                        ModelId = request.ModelId,
                        Priority = queuedRequest.Priority,
                        QueueDepth = channel.Reader.Count
                    }, ct);
                }

                using (ct.Register(() => queuedRequest.CompletionSource.TrySetCanceled()))
                {
                    var result = await queuedRequest.CompletionSource.Task;

                    Interlocked.Increment(ref _completedRequests);
                    activity?.SetTag("inference.success", true);

                    if (converter != null)
                        return converter(result);
                    if (typeof(T) == typeof(InferenceResult))
                        return (T)(object)result;
                    throw new InvalidOperationException($"No converter provided for type {typeof(T).FullName}");
                }
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
            {
                Interlocked.Increment(ref _rejectedRequests);
                _errorCounter.Add(1, new KeyValuePair<string, object?>("error.type", "timeout"));
                activity?.SetTag("inference.success", false);
                activity?.SetTag("error.type", "timeout");
                throw new TimeoutException($"Request timed out after {_config.QueueTimeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedRequests);
                _errorCounter.Add(1, new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
                activity?.SetTag("inference.success", false);
                activity?.SetTag("error.type", ex.GetType().Name);

                _logger.LogError(ex, "Request {RequestId} failed", queuedRequest.Id);

                if (_mediator != null)
                {
                    await _mediator.Publish(new InferenceFailedNotification
                    {
                        RequestId = queuedRequest.Id,
                        ModelId = request.ModelId,
                        Error = ex.Message
                    }, CancellationToken.None);
                }

                throw;
            }
            finally
            {
                timeoutCts?.Dispose();
                _pendingRequests.TryRemove(queuedRequest.Id, out _);
                _queueDepthCounter.Add(-1);
            }
        }



        /// <summary>
        /// Processes queues with advanced scheduling and monitoring
        /// </summary>
        private async Task ProcessQueuesAsync(CancellationToken ct)
        {
            _logger.LogDebug("Starting queue processing task");

            while (!ct.IsCancellationRequested)
            {
                QueuedRequest? request = null;

                try
                {
                    // Create read tasks for all queues
                    var readTasks = new[]
                    {
                        WaitForRequestAsync(_highPriorityQueue, Priority.High, ct),
                        WaitForRequestAsync(_normalPriorityQueue, Priority.Normal, ct),
                        WaitForRequestAsync(_lowPriorityQueue, Priority.Low, ct)
                    };

                    // Wait for any queue to have data
                    var completedTask = await Task.WhenAny(readTasks);
                    request = await completedTask;

                    if (request != null)
                    {
                        await ProcessSingleRequestAsync(request, ct);
                    }
                    else
                    {
                        // No requests available, brief pause
                        await Task.Delay(10, ct);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger.LogDebug("Queue processing task cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in queue processing loop");

                    if (request != null)
                    {
                        request.CompletionSource.TrySetException(
                            new InferencePipelineException($"Queue processing failed: {ex.Message}", ex));
                    }

                    await Task.Delay(100, ct);
                }
            }

            _logger.LogDebug("Queue processing task completed");
        }

        /// <summary>
        /// Helper to wait for request from a specific queue
        /// </summary>
        private async Task<QueuedRequest?> WaitForRequestAsync(
            Channel<QueuedRequest> channel,
            Priority priority,
            CancellationToken ct)
        {
            try
            {
                if (await channel.Reader.WaitToReadAsync(ct))
                {
                    if (channel.Reader.TryRead(out var request))
                    {
                        _logger.LogTrace("Retrieved {Priority} priority request {RequestId}",
                            priority, request.Id);
                        return request;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }

            return null;
        }

        /// <summary>
        /// Processes a single request with full telemetry and error handling
        /// Executes the model using IOnnxRuntimeManager and picks DirectML or CPU as appropriate.
        /// </summary>
        private async Task ProcessSingleRequestAsync(QueuedRequest request, CancellationToken ct)
        {
            using var activity = _activitySource.StartActivity("ProcessRequest");
            activity?.SetTag("request.id", request.Id);
            activity?.SetTag("model.id", request.Request.ModelId);
            activity?.SetTag("priority", request.Priority);

            var stopwatch = Stopwatch.StartNew();
            var metadata = await _ModelConfiguration.GetMetadataAsync(request.Request.ModelId);
            var semaphore = metadata.RequiresGpu ? _gpuSemaphore : _cpuSemaphore;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var cancellationToken = linkedCts.Token;

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogDebug("Processing request {RequestId} for model {ModelId} (GPU: {RequiresGpu})",
                    request.Id, request.Request.ModelId, metadata.RequiresGpu);

                // Queue time metric
                var queueTime = DateTimeOffset.UtcNow - request.QueuedAt;
                _latencyHistogram.Record(queueTime.TotalMilliseconds,
                    new KeyValuePair<string, object?>("latency.type", "queue"));

                if (_mediator != null)
                {
                    await _mediator.Publish(new InferenceStartedNotification
                    {
                        RequestId = request.Id,
                        ModelId = request.Request.ModelId,
                        QueueTimeMs = (long)queueTime.TotalMilliseconds
                    }, cancellationToken);
                }

                object? result = null;
                Exception? lastException = null;

                for (int retry = 0; retry <= _config.MaxRetries; retry++)
                {
                    try
                    {
                        // ---- BRANCH BY MODEL TYPE ----
                        if (metadata.Format == ModelFormat.GGUF || metadata.Format == ModelFormat.GGML)
                        {
                            var context = await _llamaManager.CreateContextAsync(metadata.ModelPath, null);
                            var prompt = request.Request.Input as string ?? string.Empty;

                            // --- New: Merge parameter sets ---
                            var mergedDict = await GetMergedParametersAsync(request, cancellationToken);
                            var inferenceParams = ConvertToInferenceParams(mergedDict);

                            var resultText = await _llamaManager.RunPromptAsync(context, prompt, inferenceParams, cancellationToken);
                            result = resultText;


                            // chatHistory = new ChatHistory();
                            // chatHistory.AddMessage(AuthorRole.User, "Hello, who are you?");
                            // chatHistory.AddMessage(AuthorRole.Assistant, "I am an AI assistant.");
                            // ChatSession session = new ChatSession(executor, chatHistory);
                            // await foreach (var token in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, "What can you do?"), ...)) { ... }
                            //var resultText = "";
                            //await foreach (var token in _llamaManager.RunChatAsync(context, prompt, inferenceParams, cancellationToken))
                            //    resultText += token;

                            //result = resultText;


                        }

                        else
                        {
                            // ONNX path (your current logic)
                            var provider = ExecutionProvider.CPU;
                            if (metadata.RequiresGpu)
                            {
                                var devices = await _directMLDeviceManager.EnumerateDevicesAsync();
                                if (devices.Any(d => d.DeviceType == "GPU"))
                                    provider = ExecutionProvider.DirectML;
                            }

                            var session = await _onnxManager.CreateSessionAsync(metadata.ModelPath, provider);
                            var inputs = await _onnxManager.PreprocessInputAsync(session, request.Request.Input, metadata.Type);
                            var outputs = await _onnxManager.RunAsync(session, inputs, cancellationToken);
                            result = await _onnxManager.PostprocessOutputAsync(outputs, metadata.Type);
                        }
                        break; // Success

                        break; // Success
                    }
                    catch (ModelNotLoadedException ex) when (retry < _config.MaxRetries)
                    {
                        lastException = ex;
                        var delay = _config.RetryDelayMs * (retry + 1);
                        _logger.LogWarning(ex, "Model not loaded, retry {Retry}/{Max} after {Delay}ms",
                            retry + 1, _config.MaxRetries, delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (InsufficientMemoryException ex)
                    {
                        lastException = ex;
                        _logger.LogError(ex, "Insufficient memory for inference");
                        break;
                    }
                    catch (Exception ex) when (retry < _config.MaxRetries && IsTransientError(ex))
                    {
                        lastException = ex;
                        var delay = _config.RetryDelayMs * Math.Pow(2, retry); // Exponential backoff
                        _logger.LogWarning(ex, "Transient error, retry {Retry}/{Max} after {Delay}ms",
                            retry + 1, _config.MaxRetries, delay);
                        await Task.Delay((int)delay, cancellationToken);
                    }
                }

                // After model logic:
                if (result == null)
                    throw lastException ?? new InferencePipelineException("Inference failed after retries");

                // Always wrap into InferenceResult (customize as needed)
                InferenceResult inferenceResult;
                if (result is InferenceResult ir)
                    inferenceResult = ir;
                else
                    inferenceResult = new InferenceResult { Output = result };

                request.CompletionSource.TrySetResult(inferenceResult);


                stopwatch.Stop();

                // Record metrics
                var inferenceTime = stopwatch.ElapsedMilliseconds;
                _latencyHistogram.Record(inferenceTime, new KeyValuePair<string, object?>("latency.type", "inference"));

                _metricsBuffer.Add(new MetricEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    LatencyMs = inferenceTime,
                    ModelId = request.Request.ModelId,
                    Success = true
                });

                _logger.LogInformation("Request {RequestId} completed in {ElapsedMs}ms (Queue: {QueueMs}ms, Inference: {InferenceMs}ms)",
                    request.Id, stopwatch.ElapsedMilliseconds, queueTime.TotalMilliseconds, inferenceTime);

                activity?.SetTag("inference.success", true);
                activity?.SetTag("inference.latency_ms", inferenceTime);

                if (_mediator != null)
                {
                    await _mediator.Publish(new InferenceCompletedNotification
                    {
                        RequestId = request.Id,
                        ModelId = request.Request.ModelId,
                        QueueTimeMs = (long)queueTime.TotalMilliseconds,
                        InferenceTimeMs = inferenceTime,
                        TokensGenerated = 0 // Fill if available from result
                    }, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process request {RequestId}", request.Id);

                request.CompletionSource.TrySetException(ex);

                _metricsBuffer.Add(new MetricEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    ModelId = request.Request.ModelId,
                    Success = false,
                    ErrorType = ex.GetType().Name
                });

                activity?.SetTag("inference.success", false);
                activity?.SetTag("error.type", ex.GetType().Name);

                if (_mediator != null)
                {
                    await _mediator.Publish(new InferenceFailedNotification
                    {
                        RequestId = request.Id,
                        ModelId = request.Request.ModelId,
                        Error = ex.Message,
                        ErrorType = ex.GetType().Name
                    }, CancellationToken.None);
                }
            }
            finally
            {
                semaphore.Release();
                _logger.LogTrace("Released {ResourceType} slot for request {RequestId}",
                    metadata.RequiresGpu ? "GPU" : "CPU", request.Id);
            }
        }


        /// <summary>
        /// Ensures model is loaded before inference
        /// </summary>
        private async Task EnsureModelLoadedAsync(
            string modelId,
            ModelConfiguration metadata,
            CancellationToken ct)
        {
            var isLoaded = await _orchestrator.IsModelLoadedAsync(modelId);
            if (!isLoaded)
            {
                _logger.LogInformation("Auto-loading model {ModelId}", modelId);

                var ModelLoadRequest = new ModelLoadRequest
                {
                    ModelId = modelId,
                    ModelType = metadata.Type,
                    Provider = metadata.RequiresGpu ? "directml" : "cpu",
                    ModelPath = metadata.ModelPath
                };

                await _orchestrator.LoadModelAsync(ModelLoadRequest, null, ct);
            }
        }

        /// <summary>
        /// Ages priorities to prevent starvation
        /// </summary>
        private async Task AgePrioritiesAsync(CancellationToken ct)
        {
            _logger.LogDebug("Starting priority aging task");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_config.PriorityAgingIntervalMs, ct);

                    var threshold = TimeSpan.FromSeconds(_config.PriorityAgingThresholdSeconds);
                    var now = DateTimeOffset.UtcNow;

                    // Age normal priority to high
                    await AgeQueueAsync(_normalPriorityQueue, _highPriorityQueue, threshold, now, ct);

                    // Age low priority to normal
                    await AgeQueueAsync(_lowPriorityQueue, _normalPriorityQueue, threshold, now, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in priority aging");
                }
            }

            _logger.LogDebug("Priority aging task completed");
        }

        /// <summary>
        /// Ages requests from one queue to another
        /// </summary>
        private async Task AgeQueueAsync(
            Channel<QueuedRequest> fromQueue,
            Channel<QueuedRequest> toQueue,
            TimeSpan threshold,
            DateTimeOffset now,
            CancellationToken ct)
        {
            var toPromote = new List<QueuedRequest>();

            // Check for aged requests
            while (fromQueue.Reader.TryRead(out var request))
            {
                if (now - request.QueuedAt > threshold)
                {
                    toPromote.Add(request);
                    _logger.LogDebug("Promoting request {RequestId} due to age", request.Id);
                }
                else
                {
                    // Put it back if not aged
                    await fromQueue.Writer.WriteAsync(request, ct);
                    break; // Requests are in order, so we can stop
                }
            }

            // Promote aged requests
            foreach (var request in toPromote)
            {
                await toQueue.Writer.WriteAsync(request, ct);
            }
        }

        /// <summary>
        /// Cleans up old metrics periodically
        /// </summary>
        private async Task CleanupMetricsAsync(CancellationToken ct)
        {
            _logger.LogDebug("Starting metrics cleanup task");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), ct);
                    CleanupOldMetrics();
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in metrics cleanup");
                }
            }

            _logger.LogDebug("Metrics cleanup task completed");
        }

        /// <summary>
        /// Removes metrics older than retention period
        /// </summary>
        private void CleanupOldMetrics()
        {
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-_config.MetricsRetentionMinutes);
            var removed = _metricsBuffer.RemoveWhere(m => m.Timestamp < cutoff);

            if (removed > 0)
            {
                _logger.LogDebug("Removed {Count} old metrics", removed);
            }
        }

        /// <summary>
        /// Determines priority using model metadata
        /// </summary>
        private async Task<Priority> DeterminePriorityAsync(InferencePipelineRequest request)
        {
            // Use explicit priority if set
            if (request.Priority != 1)
            {
                return request.Priority switch
                {
                    0 => Priority.Low,
                    2 => Priority.High,
                    _ => Priority.Normal
                };
            }

            // Check model metadata for default priority
            var metadata = await _ModelConfiguration.GetMetadataAsync(request.ModelId);
            if (metadata.DefaultPriority != 1)
            {
                return metadata.DefaultPriority switch
                {
                    0 => Priority.Low,
                    2 => Priority.High,
                    _ => Priority.Normal
                };
            }

            // Tag-based priority
            if (request.Tags?.Contains("evidence") == true)
                return Priority.High;

            if (request.Tags?.Contains("interactive") == true)
                return Priority.High;

            if (request.Tags?.Contains("batch") == true)
                return Priority.Low;

            if (request.Tags?.Contains("background") == true)
                return Priority.Low;

            return Priority.Normal;
        }

        /// <summary>
        /// Checks if error is transient and should be retried
        /// </summary>
        private bool IsTransientError(Exception ex)
        {
            return ex is TimeoutException ||
                   ex is TaskCanceledException ||
                   (ex is HttpRequestException && ex.Message.Contains("503")) ||
                   ex.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Executes multiple requests as a batch, with optional converter and full batching support
        /// </summary>
        public async Task<BatchResult<T>> ExecuteBatchAsync<T>(
            IEnumerable<InferencePipelineRequest> requests,
            Func<InferenceResult, T>? converter = null,
            CancellationToken ct = default)
        {
            var requestList = requests.ToList();
            if (!requestList.Any())
            {
                return new BatchResult<T>
                {
                    Results = new List<T>(),
                    TotalRequests = 0,
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            for (int i = 0; i < requestList.Count; i++)
            {
                requestList[i].Index ??= i;
            }

            var grouped = requestList.GroupBy(r => r.ModelId);
            var results = new ConcurrentBag<(int index, T result)>();
            var errors = new ConcurrentDictionary<int, Exception>();

            await Parallel.ForEachAsync(grouped, ct, async (group, token) =>
            {
                var modelId = group.Key;
                var batch = group.ToList();
                var metadata = await _ModelConfiguration.GetMetadataAsync(modelId);

                if (metadata.SupportsBatching && batch.Count >= 2)
                {
                    try
                    {
                        var batchResults = await ExecuteBatchInternalAsync(modelId, batch, metadata, converter, token);
                        for (int i = 0; i < batchResults.Length; i++)
                        {
                            results.Add((batch[i].Index!.Value, batchResults[i]));
                        }
                    }
                    catch (Exception ex)
                    {
                        foreach (var req in batch)
                        {
                            errors.TryAdd(req.Index!.Value, ex);
                        }
                    }
                }
                else
                {
                    foreach (var req in batch)
                    {
                        try
                        {
                            var result = await ExecuteAsync(req, converter, token);
                            results.Add((req.Index!.Value, result));
                        }
                        catch (Exception ex)
                        {
                            errors.TryAdd(req.Index!.Value, ex);
                        }
                    }
                }
            });

            var sortedResults = results.OrderBy(r => r.index).Select(r => r.result).ToList();

            return new BatchResult<T>
            {
                Results = sortedResults,
                Errors = new Dictionary<int, Exception>(errors),
                TotalRequests = requestList.Count,
                SuccessCount = results.Count,
                FailureCount = errors.Count
            };
        }


        /// <summary>
        /// Internal batch execution for models that support it, with mapping
        /// </summary>
        private async Task<T[]> ExecuteBatchInternalAsync<T>(
        string modelId,
        List<InferencePipelineRequest> batch,
        ModelConfiguration metadata,
        Func<InferenceResult, T>? converter,
        CancellationToken ct)
        {
            if (batch.Count > metadata.MaxBatchSize)
            {
                var results = new List<T>();
                for (int i = 0; i < batch.Count; i += metadata.MaxBatchSize)
                {
                    var chunk = batch.Skip(i).Take(metadata.MaxBatchSize).ToList();
                    var chunkResults = await ExecuteBatchInternalAsync(modelId, chunk, metadata, converter, ct);
                    results.AddRange(chunkResults);
                }
                return results.ToArray();
            }

            // Universal fallback: run each request one by one (sequentially)
            var fallbackResults = new T[batch.Count];
            for (int i = 0; i < batch.Count; i++)
            {
                fallbackResults[i] = await ExecuteAsync(batch[i], converter, ct);
            }
            return fallbackResults;
        }


        /// <summary>
        /// Gets comprehensive pipeline statistics
        /// </summary>
        public InferencePipelineStats GetStats()
        {
            var metrics = _metricsBuffer.ToArray();
            var stats = new InferencePipelineStats
            {
                TotalRequests = _totalRequests,
                CompletedRequests = _completedRequests,
                FailedRequests = _failedRequests,
                RejectedRequests = _rejectedRequests,
                PendingRequests = _pendingRequests.Count,
                HighPriorityQueueDepth = _highPriorityQueue.Reader.Count,
                NormalPriorityQueueDepth = _normalPriorityQueue.Reader.Count,
                LowPriorityQueueDepth = _lowPriorityQueue.Reader.Count,
                GpuSlotsAvailable = _gpuSemaphore.CurrentCount,
                CpuSlotsAvailable = _cpuSemaphore.CurrentCount,
                RequestsByModel = new Dictionary<string, long>(_requestsByModel)
            };

            if (metrics.Length > 0)
            {
                var latencies = metrics.Where(m => m.Success).Select(m => m.LatencyMs).OrderBy(l => l).ToArray();
                if (latencies.Length > 0)
                {
                    stats.AverageLatencyMs = latencies.Average();
                    stats.P50LatencyMs = GetPercentile(latencies, 0.50);
                    stats.P95LatencyMs = GetPercentile(latencies, 0.95);
                    stats.P99LatencyMs = GetPercentile(latencies, 0.99);
                }

                var recentWindow = DateTimeOffset.UtcNow.AddMinutes(-1);
                var recentMetrics = metrics.Where(m => m.Timestamp > recentWindow).ToArray();
                if (recentMetrics.Length > 0)
                {
                    stats.RequestsPerMinute = recentMetrics.Length;
                    stats.ErrorRate = (double)recentMetrics.Count(m => !m.Success) / recentMetrics.Length;
                }
            }

            return stats;
        }

        /// <summary>
        /// Health check for monitoring
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
        {
            var issues = new List<string>();

            // Check queue depths
            var totalQueued = _highPriorityQueue.Reader.Count +
                             _normalPriorityQueue.Reader.Count +
                             _lowPriorityQueue.Reader.Count;

            if (totalQueued > _config.QueueCapacity * 0.8)
            {
                issues.Add($"Queue depth high: {totalQueued}/{_config.QueueCapacity}");
            }

            // Check error rate
            var stats = GetStats();
            if (stats.ErrorRate > 0.1) // >10% errors
            {
                issues.Add($"High error rate: {stats.ErrorRate:P1}");
            }

            // Check resource availability
            if (_gpuSemaphore.CurrentCount == 0 && _cpuSemaphore.CurrentCount == 0)
            {
                issues.Add("No inference slots available");
            }

            // Check processing tasks
            var deadTasks = _processingTasks.Count(t => t.IsCompleted);
            if (deadTasks > 0)
            {
                issues.Add($"{deadTasks} processing tasks have stopped");
            }

            return new HealthCheckResult
            {
                IsHealthy = issues.Count == 0,
                Issues = issues,
                Stats = stats
            };
        }

        /// <summary>
        /// Calculates percentile from sorted array
        /// </summary>
        private double GetPercentile(double[] sortedArray, double percentile)
        {
            if (sortedArray.Length == 0) return 0;

            var index = (int)Math.Ceiling(percentile * sortedArray.Length) - 1;
            return sortedArray[Math.Max(0, Math.Min(index, sortedArray.Length - 1))];
        }

        /// <summary>
        /// Graceful shutdown with resource cleanup
        /// </summary>
        public void Dispose()
        {
            _logger.LogInformation("Shutting down InferencePipeline");

            // Stop accepting new requests
            _highPriorityQueue.Writer.TryComplete();
            _normalPriorityQueue.Writer.TryComplete();
            _lowPriorityQueue.Writer.TryComplete();

            // Signal shutdown
            _shutdownTokenSource.Cancel();

            // Wait for tasks to complete
            var allTasks = _processingTasks
                .Concat(new[] { _priorityAgingTask, _metricsCleanupTask })
                .Where(t => t != null)
                .ToArray();

            try
            {
                Task.WaitAll(allTasks!, TimeSpan.FromSeconds(10));
            }
            catch (AggregateException ex)
            {
                _logger.LogWarning(ex, "Some tasks did not complete cleanly");
            }

            // Cancel pending requests
            foreach (var request in _pendingRequests.Values)
            {
                request.CompletionSource.TrySetCanceled();
            }

            // Dispose resources
            _gpuSemaphore?.Dispose();
            _cpuSemaphore?.Dispose();
            _shutdownTokenSource?.Dispose();
            _metricsCleanupTimer?.Dispose();
            _activitySource?.Dispose();
            _meter?.Dispose();

            _logger.LogInformation("InferencePipeline shutdown complete. Processed {Total} requests ({Success} success, {Failed} failed, {Rejected} rejected)",
                _totalRequests, _completedRequests, _failedRequests, _rejectedRequests);
        }

        //LlamaSharp requires Inference params 
        private async Task<Dictionary<string, object>> GetMergedParametersAsync(QueuedRequest request, CancellationToken ct)
        {
            // 1. Fetch default parameter set for model
            var modelParamsList = await _modelParams.GetByModelIdAsync(request.Request.ModelId, ct);
            var defaultParams = modelParamsList.FirstOrDefault(p => p.Name == "Default")
                ?.Parameters ?? new Dictionary<string, object>();

            // 2. Overlay request-specific parameters (overrides default)
            var merged = new Dictionary<string, object>(defaultParams);

            if (request.Request.Parameters != null)
            {
                foreach (var kvp in request.Request.Parameters)
                    merged[kvp.Key] = kvp.Value;
            }

            return merged;
        }

        private InferenceParams ConvertToInferenceParams(Dictionary<string, object> dict)
        {
            var json = JsonSerializer.Serialize(dict);
            return JsonSerializer.Deserialize<InferenceParams>(json)!;
        }


    }


}