using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Inference;

public interface IInferencePipeline
{
    Task<T> ExecuteAsync<T>(InferencePipelineRequest request, CancellationToken ct = default);
    Task<BatchResult<T>> ExecuteBatchAsync<T>(IEnumerable<InferencePipelineRequest> requests, CancellationToken ct = default);
    InferencePipelineStats GetStats();
}

public sealed class InferencePipeline : IInferencePipeline, IDisposable
{
    private readonly ILogger<InferencePipeline> _logger;
    private readonly IModelOrchestrator _orchestrator;
    private readonly Channel<QueuedRequest> _highPriorityQueue;
    private readonly Channel<QueuedRequest> _normalPriorityQueue;
    private readonly Channel<QueuedRequest> _lowPriorityQueue;
    private readonly ConcurrentDictionary<string, QueuedRequest> _pendingRequests = new();
    private readonly SemaphoreSlim _gpuSemaphore;
    private readonly SemaphoreSlim _cpuSemaphore;

    // Metrics
    private long _totalRequests;
    private long _completedRequests;
    private long _failedRequests;
    private readonly ConcurrentDictionary<string, ModelMetrics> _modelMetrics = new();

    // Configuration
    private const int MaxGpuConcurrency = 2;  // Max simultaneous GPU operations
    private const int MaxCpuConcurrency = 4;  // Max simultaneous CPU operations
    private const int QueueCapacity = 1000;
    private const int BatchTimeout = 100; // ms to wait for batch accumulation

    public InferencePipeline(ILogger<InferencePipeline> logger, IModelOrchestrator orchestrator)
    {
        _logger = logger;
        _orchestrator = orchestrator;

        // Create priority channels
        var options = new BoundedChannelOptions(QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _highPriorityQueue = Channel.CreateBounded<QueuedRequest>(options);
        _normalPriorityQueue = Channel.CreateBounded<QueuedRequest>(options);
        _lowPriorityQueue = Channel.CreateBounded<QueuedRequest>(options);

        // Resource semaphores
        _gpuSemaphore = new SemaphoreSlim(MaxGpuConcurrency, MaxGpuConcurrency);
        _cpuSemaphore = new SemaphoreSlim(MaxCpuConcurrency, MaxCpuConcurrency);

        // Start processing loops
        for (int i = 0; i < MaxGpuConcurrency + MaxCpuConcurrency; i++)
        {
            _ = Task.Run(() => ProcessQueuesAsync(CancellationToken.None));
        }
    }

    public async Task<T> ExecuteAsync<T>(InferencePipelineRequest request, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _totalRequests);

        var queuedRequest = new QueuedRequest
        {
            Id = Guid.NewGuid().ToString("N"),
            Request = request,
            Priority = DeterminePriority(request),
            QueuedAt = DateTimeOffset.UtcNow,
            CompletionSource = new TaskCompletionSource<object?>(),
            CancellationToken = ct
        };

        _pendingRequests[queuedRequest.Id] = queuedRequest;

        try
        {
            // Route to appropriate queue based on priority
            var channel = queuedRequest.Priority switch
            {
                Priority.High => _highPriorityQueue,
                Priority.Low => _lowPriorityQueue,
                _ => _normalPriorityQueue
            };

            await channel.Writer.WriteAsync(queuedRequest, ct);

            _logger.LogDebug("Request {RequestId} queued with {Priority} priority for {ModelId}",
                queuedRequest.Id, queuedRequest.Priority, request.ModelId);

            // Wait for completion
            using (ct.Register(() => queuedRequest.CompletionSource.TrySetCanceled()))
            {
                var result = await queuedRequest.CompletionSource.Task;
                Interlocked.Increment(ref _completedRequests);
                return (T)result!;
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedRequests);
            _logger.LogError(ex, "Request {RequestId} failed", queuedRequest.Id);
            throw;
        }
        finally
        {
            _pendingRequests.TryRemove(queuedRequest.Id, out _);
        }
    }

    public async Task<BatchResult<T>> ExecuteBatchAsync<T>(
        IEnumerable<InferencePipelineRequest> requests,
        CancellationToken ct = default)
    {
        var requestList = requests.ToList();
        if (!requestList.Any())
        {
            return new BatchResult<T> { Results = new List<T>() };
        }

        // Group by model for efficient batching
        var grouped = requestList.GroupBy(r => r.ModelId);
        var results = new ConcurrentBag<(int index, T result)>();
        var errors = new ConcurrentBag<(int index, Exception error)>();

        await Parallel.ForEachAsync(grouped, ct, async (group, token) =>
        {
            var modelId = group.Key;
            var batch = group.ToList();

            // Check if model supports batching
            if (SupportsBatching(modelId))
            {
                try
                {
                    var batchResult = await ExecuteBatchInternalAsync<T>(modelId, batch, token);
                    for (int i = 0; i < batch.Count; i++)
                    {
                        results.Add((batch[i].Index, batchResult[i]));
                    }
                }
                catch (Exception ex)
                {
                    foreach (var req in batch)
                    {
                        errors.Add((req.Index, ex));
                    }
                }
            }
            else
            {
                // Fall back to sequential processing
                foreach (var req in batch)
                {
                    try
                    {
                        var result = await ExecuteAsync<T>(req, token);
                        results.Add((req.Index, result));
                    }
                    catch (Exception ex)
                    {
                        errors.Add((req.Index, ex));
                    }
                }
            }
        });

        // Sort results by original index
        var sortedResults = results.OrderBy(r => r.index).Select(r => r.result).ToList();
        var sortedErrors = errors.OrderBy(e => e.index).ToDictionary(e => e.index, e => e.error);

        return new BatchResult<T>
        {
            Results = sortedResults,
            Errors = sortedErrors,
            TotalRequests = requestList.Count,
            SuccessCount = results.Count,
            FailureCount = errors.Count
        };
    }

    private async Task ProcessQueuesAsync(CancellationToken ct)
    {
        var batchAccumulator = new List<QueuedRequest>();
        var batchTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(BatchTimeout));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                QueuedRequest? request = null;

                // Try to get from queues in priority order
                if (_highPriorityQueue.Reader.TryRead(out request) ||
                    _normalPriorityQueue.Reader.TryRead(out request) ||
                    _lowPriorityQueue.Reader.TryRead(out request))
                {
                    // Check if we can batch this request
                    if (batchAccumulator.Any() &&
                        CanBatch(batchAccumulator[0], request) &&
                        batchAccumulator.Count < GetMaxBatchSize(request.Request.ModelId))
                    {
                        batchAccumulator.Add(request);

                        // Continue accumulating unless batch is full
                        if (batchAccumulator.Count < GetMaxBatchSize(request.Request.ModelId))
                        {
                            continue;
                        }
                    }

                    // Process accumulated batch if any
                    if (batchAccumulator.Any())
                    {
                        await ProcessBatchAsync(batchAccumulator, ct);
                        batchAccumulator.Clear();
                    }

                    // Start new batch or process single request
                    if (SupportsBatching(request.Request.ModelId))
                    {
                        batchAccumulator.Add(request);
                    }
                    else
                    {
                        await ProcessSingleRequestAsync(request, ct);
                    }
                }
                else
                {
                    // No requests available, process any pending batch
                    if (batchAccumulator.Any() && await batchTimer.WaitForNextTickAsync(ct))
                    {
                        await ProcessBatchAsync(batchAccumulator, ct);
                        batchAccumulator.Clear();
                    }
                    else
                    {
                        // Wait a bit before checking again
                        await Task.Delay(10, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in processing loop");
            }
        }
    }

    private async Task ProcessSingleRequestAsync(QueuedRequest request, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var semaphore = RequiresGpu(request.Request.ModelId) ? _gpuSemaphore : _cpuSemaphore;

        await semaphore.WaitAsync(ct);
        try
        {
            _logger.LogDebug("Processing request {RequestId} for model {ModelId}",
                request.Id, request.Request.ModelId);

            // Update metrics
            UpdateModelMetrics(request.Request.ModelId, m =>
            {
                m.ActiveRequests++;
                m.TotalRequests++;
            });

            // Execute inference
            var result = await _orchestrator.InferAsync(
                request.Request.ModelId,
                request.Request.Input,
                request.CancellationToken);

            // Complete request
            request.CompletionSource.TrySetResult(result.Output);

            // Update metrics
            UpdateModelMetrics(request.Request.ModelId, m =>
            {
                m.ActiveRequests--;
                m.TotalLatency += stopwatch.Elapsed;
                m.AverageLatency = m.TotalLatency / m.TotalRequests;
                m.LastRequestTime = DateTimeOffset.UtcNow;

                if (result is InferenceResult inferResult)
                {
                    m.TotalTokens += inferResult.TokensProcessed;
                    m.AverageTokensPerSecond =
                        (m.AverageTokensPerSecond * (m.TotalRequests - 1) + inferResult.TokensPerSecond)
                        / m.TotalRequests;
                }
            });

            _logger.LogInformation("Request {RequestId} completed in {ElapsedMs}ms",
                request.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            request.CompletionSource.TrySetException(ex);
            UpdateModelMetrics(request.Request.ModelId, m =>
            {
                m.ActiveRequests--;
                m.FailedRequests++;
            });
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task ProcessBatchAsync(List<QueuedRequest> batch, CancellationToken ct)
    {
        if (!batch.Any()) return;

        var modelId = batch[0].Request.ModelId;
        var stopwatch = Stopwatch.StartNew();
        var semaphore = RequiresGpu(modelId) ? _gpuSemaphore : _cpuSemaphore;

        await semaphore.WaitAsync(ct);
        try
        {
            _logger.LogDebug("Processing batch of {Count} requests for model {ModelId}",
                batch.Count, modelId);

            // Update metrics
            UpdateModelMetrics(modelId, m =>
            {
                m.ActiveRequests += batch.Count;
                m.TotalRequests += batch.Count;
                m.BatchCount++;
                m.TotalBatchSize += batch.Count;
                m.AverageBatchSize = m.TotalBatchSize / (double)m.BatchCount;
            });

            // Prepare batch input
            var batchInput = PrepareBatchInput(batch);

            // Execute batch inference
            var result = await _orchestrator.InferAsync(modelId, batchInput, ct);

            // Distribute results
            DistributeBatchResults(batch, result);

            // Update metrics
            UpdateModelMetrics(modelId, m =>
            {
                m.ActiveRequests -= batch.Count;
                m.TotalLatency += stopwatch.Elapsed;
                m.AverageLatency = m.TotalLatency / m.TotalRequests;
                m.LastRequestTime = DateTimeOffset.UtcNow;
            });

            _logger.LogInformation("Batch of {Count} requests completed in {ElapsedMs}ms ({PerRequest}ms per request)",
                batch.Count, stopwatch.ElapsedMilliseconds, stopwatch.ElapsedMilliseconds / batch.Count);
        }
        catch (Exception ex)
        {
            // Fail all requests in batch
            foreach (var request in batch)
            {
                request.CompletionSource.TrySetException(ex);
            }

            UpdateModelMetrics(modelId, m =>
            {
                m.ActiveRequests -= batch.Count;
                m.FailedRequests += batch.Count;
            });
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private Priority DeterminePriority(InferencePipelineRequest request)
    {
        // Evidence processing gets highest priority
        if (request.Tags?.Contains("evidence") == true)
            return Priority.High;

        // Real-time transcription gets high priority
        if (request.ModelId.Contains("whisper", StringComparison.OrdinalIgnoreCase) &&
            request.Tags?.Contains("realtime") == true)
            return Priority.High;

        // Background analysis gets low priority
        if (request.Tags?.Contains("background") == true)
            return Priority.Low;

        // Default to normal
        return Priority.Normal;
    }

    private bool SupportsBatching(string modelId)
    {
        // LLMs and embedding models typically support batching
        return modelId.Contains("embed", StringComparison.OrdinalIgnoreCase) ||
               modelId.Contains("llama", StringComparison.OrdinalIgnoreCase) ||
               modelId.Contains("mistral", StringComparison.OrdinalIgnoreCase);
    }

    private bool CanBatch(QueuedRequest first, QueuedRequest second)
    {
        return first.Request.ModelId == second.Request.ModelId &&
               first.Request.Parameters?.GetValueOrDefault("temperature") ==
               second.Request.Parameters?.GetValueOrDefault("temperature") &&
               first.Request.Parameters?.GetValueOrDefault("max_tokens") ==
               second.Request.Parameters?.GetValueOrDefault("max_tokens");
    }

    private int GetMaxBatchSize(string modelId)
    {
        // Model-specific batch sizes
        if (modelId.Contains("embed", StringComparison.OrdinalIgnoreCase))
            return 32; // Embedding models can handle larger batches
        if (modelId.Contains("whisper", StringComparison.OrdinalIgnoreCase))
            return 4;  // Audio processing is memory intensive
        return 8;      // Default for LLMs
    }

    private bool RequiresGpu(string modelId)
    {
        // Small models can run on CPU
        return !modelId.Contains("tiny", StringComparison.OrdinalIgnoreCase) &&
               !modelId.Contains("small", StringComparison.OrdinalIgnoreCase);
    }

    private object PrepareBatchInput(List<QueuedRequest> batch)
    {
        // Combine inputs based on model type
        var modelId = batch[0].Request.ModelId;

        if (modelId.Contains("embed", StringComparison.OrdinalIgnoreCase))
        {
            // For embedding models, combine texts
            return new
            {
                texts = batch.Select(r => r.Request.Input).ToList()
            };
        }

        // For LLMs, prepare batch prompts
        return new
        {
            prompts = batch.Select(r => r.Request.Input).ToList(),
            parameters = batch[0].Request.Parameters
        };
    }

    private void DistributeBatchResults(List<QueuedRequest> batch, object result)
    {
        if (result is IList<object> results)
        {
            for (int i = 0; i < Math.Min(batch.Count, results.Count); i++)
            {
                batch[i].CompletionSource.TrySetResult(results[i]);
            }
        }
        else
        {
            // Single result for all (shouldn't happen in proper batch processing)
            foreach (var request in batch)
            {
                request.CompletionSource.TrySetResult(result);
            }
        }
    }

    private void UpdateModelMetrics(string modelId, Action<ModelMetrics> update)
    {
        _modelMetrics.AddOrUpdate(modelId,
            _ =>
            {
                var metrics = new ModelMetrics { ModelId = modelId };
                update(metrics);
                return metrics;
            },
            (_, existing) =>
            {
                update(existing);
                return existing;
            });
    }

    public InferencePipelineStats GetStats()
    {
        return new InferencePipelineStats
        {
            TotalRequests = _totalRequests,
            CompletedRequests = _completedRequests,
            FailedRequests = _failedRequests,
            PendingRequests = _pendingRequests.Count,
            HighPriorityQueueDepth = _highPriorityQueue.Reader.Count,
            NormalPriorityQueueDepth = _normalPriorityQueue.Reader.Count,
            LowPriorityQueueDepth = _lowPriorityQueue.Reader.Count,
            ModelMetrics = _modelMetrics.Values.ToList(),
            GpuSlotsAvailable = _gpuSemaphore.CurrentCount,
            CpuSlotsAvailable = _cpuSemaphore.CurrentCount
        };
    }

    public void Dispose()
    {
        _highPriorityQueue.Writer.TryComplete();
        _normalPriorityQueue.Writer.TryComplete();
        _lowPriorityQueue.Writer.TryComplete();

        // Cancel all pending requests
        foreach (var request in _pendingRequests.Values)
        {
            request.CompletionSource.TrySetCanceled();
        }

        _gpuSemaphore?.Dispose();
        _cpuSemaphore?.Dispose();
    }
}

// Supporting types
public sealed class InferencePipelineRequest
{
    public required string ModelId { get; init; }
    public required object Input { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
    public HashSet<string>? Tags { get; init; }
    public int Index { get; init; } // For batch processing
}

public sealed class QueuedRequest
{
    public required string Id { get; init; }
    public required InferencePipelineRequest Request { get; init; }
    public required Priority Priority { get; init; }
    public DateTimeOffset QueuedAt { get; init; }
    public TaskCompletionSource<object?> CompletionSource { get; init; } = new();
    public CancellationToken CancellationToken { get; init; }
}

public sealed class BatchResult<T>
{
    public required List<T> Results { get; init; }
    public Dictionary<int, Exception> Errors { get; init; } = new();
    public int TotalRequests { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
}

public sealed class InferencePipelineStats
{
    public long TotalRequests { get; init; }
    public long CompletedRequests { get; init; }
    public long FailedRequests { get; init; }
    public int PendingRequests { get; init; }
    public int HighPriorityQueueDepth { get; init; }
    public int NormalPriorityQueueDepth { get; init; }
    public int LowPriorityQueueDepth { get; init; }
    public List<ModelMetrics> ModelMetrics { get; init; } = new();
    public int GpuSlotsAvailable { get; init; }
    public int CpuSlotsAvailable { get; init; }
}

public sealed class ModelMetrics
{
    public required string ModelId { get; init; }
    public int ActiveRequests { get; set; }
    public long TotalRequests { get; set; }
    public long FailedRequests { get; set; }
    public TimeSpan TotalLatency { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public long TotalTokens { get; set; }
    public double AverageTokensPerSecond { get; set; }
    public int BatchCount { get; set; }
    public int TotalBatchSize { get; set; }
    public double AverageBatchSize { get; set; }
    public DateTimeOffset LastRequestTime { get; set; }
}

public enum Priority
{
    Low = 0,
    Normal = 1,
    High = 2
}