using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using IIM.Core.Models;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Core.Inference;

/// <summary>
/// Interface for managing inference request pipeline and queueing
/// </summary>
public interface IInferencePipeline
{
    Task<T> ExecuteAsync<T>(InferencePipelineRequest request, CancellationToken ct = default);
    Task<BatchResult<T>> ExecuteBatchAsync<T>(IEnumerable<InferencePipelineRequest> requests, CancellationToken ct = default);
    InferencePipelineStats GetStats();
}

/// <summary>
/// Manages queueing and prioritization of inference requests
/// Handles batching for improved throughput and resource management
/// </summary>
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

    // Metrics tracking
    private long _totalRequests;
    private long _completedRequests;
    private long _failedRequests;

    // Configuration constants
    private const int MaxGpuConcurrency = 2;
    private const int MaxCpuConcurrency = 4;
    private const int QueueCapacity = 1000;

    /// <summary>
    /// Initializes the inference pipeline with queue management
    /// </summary>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="orchestrator">Model orchestrator for actual inference</param>
    public InferencePipeline(ILogger<InferencePipeline> logger, IModelOrchestrator orchestrator)
    {
        _logger = logger;
        _orchestrator = orchestrator;

        // Create priority channels for request queueing
        var options = new BoundedChannelOptions(QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _highPriorityQueue = Channel.CreateBounded<QueuedRequest>(options);
        _normalPriorityQueue = Channel.CreateBounded<QueuedRequest>(options);
        _lowPriorityQueue = Channel.CreateBounded<QueuedRequest>(options);

        // Resource semaphores to limit concurrent operations
        _gpuSemaphore = new SemaphoreSlim(MaxGpuConcurrency, MaxGpuConcurrency);
        _cpuSemaphore = new SemaphoreSlim(MaxCpuConcurrency, MaxCpuConcurrency);

        // Start processing loops
        for (int i = 0; i < MaxGpuConcurrency + MaxCpuConcurrency; i++)
        {
            _ = Task.Run(() => ProcessQueuesAsync(CancellationToken.None));
        }
    }

    /// <summary>
    /// Executes a single inference request with priority queueing
    /// </summary>
    /// <typeparam name="T">Expected result type</typeparam>
    /// <param name="request">Inference request with input and parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Inference result of type T</returns>
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
            // Route to appropriate priority queue
            var channel = queuedRequest.Priority switch
            {
                Priority.High => _highPriorityQueue,
                Priority.Low => _lowPriorityQueue,
                _ => _normalPriorityQueue
            };

            await channel.Writer.WriteAsync(queuedRequest, ct);

            _logger.LogDebug("Request {RequestId} queued with {Priority} priority",
                queuedRequest.Id, queuedRequest.Priority);

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

    /// <summary>
    /// Executes multiple inference requests as a batch for improved efficiency
    /// </summary>
    /// <typeparam name="T">Expected result type</typeparam>
    /// <param name="requests">Collection of inference requests</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Batch results with success/failure information</returns>
    public async Task<BatchResult<T>> ExecuteBatchAsync<T>(
       IEnumerable<InferencePipelineRequest> requests,
       CancellationToken ct = default)
    {
        var requestList = requests.ToList();
        if (!requestList.Any())
        {
            return new BatchResult<T> { Results = new List<T>() };
        }

        // Assign indices if not present
        for (int i = 0; i < requestList.Count; i++)
        {
            if (!requestList[i].Index.HasValue)
            {
                requestList[i].Index = i;
            }
        }

        // Now all requests have indices, proceed as before
        var grouped = requestList.GroupBy(r => r.ModelId);
        var results = new ConcurrentBag<(int index, T result)>();
        var errors = new ConcurrentBag<(int index, Exception error)>();

        await Parallel.ForEachAsync(grouped, ct, async (group, token) =>
        {
            var modelId = group.Key;
            var batch = group.ToList();

            if (SupportsBatching(modelId))
            {
                try
                {
                    var batchResults = new T[batch.Count];
                    for (int i = 0; i < batch.Count; i++)
                    {
                        batchResults[i] = await ExecuteAsync<T>(batch[i], token);
                    }

                    for (int i = 0; i < batch.Count; i++)
                    {
                        // Now we know Index is not null because we assigned it above
                        results.Add((batch[i].Index!.Value, batchResults[i]));
                    }
                }
                catch (Exception ex)
                {
                    foreach (var req in batch)
                    {
                        errors.Add((req.Index!.Value, ex));
                    }
                }
            }
            else
            {
                foreach (var req in batch)
                {
                    try
                    {
                        var result = await ExecuteAsync<T>(req, token);
                        results.Add((req.Index!.Value, result));
                    }
                    catch (Exception ex)
                    {
                        errors.Add((req.Index!.Value, ex));
                    }
                }
            }
        });

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


    /// <summary>
    /// Gets current pipeline statistics including queue depths and throughput
    /// </summary>
    /// <returns>Current pipeline statistics</returns>
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
            GpuSlotsAvailable = _gpuSemaphore.CurrentCount,
            CpuSlotsAvailable = _cpuSemaphore.CurrentCount
        };
    }

    /// <summary>
    /// Internal batch execution for models that support batching
    /// </summary>
    private async Task<T[]> ExecuteBatchInternalAsync<T>(
        string modelId,
        List<InferencePipelineRequest> batch,
        CancellationToken ct)
    {
        var results = new T[batch.Count];

        // For now, process sequentially
        // In production, this would actually batch to the model
        for (int i = 0; i < batch.Count; i++)
        {
            results[i] = await ExecuteAsync<T>(batch[i], ct);
        }

        return results;
    }

    /// <summary>
    /// Main queue processing loop that pulls from priority queues
    /// </summary>
    private async Task ProcessQueuesAsync(CancellationToken ct)
    {
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
                    await ProcessSingleRequestAsync(request, ct);
                }
                else
                {
                    // No requests available, wait a bit
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in processing loop");
            }
        }
    }

    /// <summary>
    /// Processes a single queued request
    /// </summary>
    private async Task ProcessSingleRequestAsync(QueuedRequest request, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var semaphore = RequiresGpu(request.Request.ModelId) ? _gpuSemaphore : _cpuSemaphore;

        await semaphore.WaitAsync(ct);
        try
        {
            _logger.LogDebug("Processing request {RequestId}", request.Id);

            // Execute inference via orchestrator
            var result = await _orchestrator.InferAsync(
                request.Request.ModelId,
                request.Request.Input,
                request.CancellationToken);

            // Complete request
            request.CompletionSource.TrySetResult(result.Output);

            _logger.LogInformation("Request {RequestId} completed in {ElapsedMs}ms",
                request.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            request.CompletionSource.TrySetException(ex);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Determines priority based on request tags and model type
    /// </summary>
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

        return Priority.Normal;
    }

    /// <summary>
    /// Checks if a model supports batch processing
    /// </summary>
    private bool SupportsBatching(string modelId)
    {
        return modelId.Contains("embed", StringComparison.OrdinalIgnoreCase) ||
               modelId.Contains("llama", StringComparison.OrdinalIgnoreCase) ||
               modelId.Contains("mistral", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a model requires GPU resources
    /// </summary>
    private bool RequiresGpu(string modelId)
    {
        return !modelId.Contains("tiny", StringComparison.OrdinalIgnoreCase) &&
               !modelId.Contains("small", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Disposes resources and completes all channels
    /// </summary>
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
    public int GpuSlotsAvailable { get; init; }
    public int CpuSlotsAvailable { get; init; }
}

