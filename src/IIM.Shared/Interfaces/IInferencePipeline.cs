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
    /// Interface for managing inference request pipeline and queueing
    /// </summary>
    public interface IInferencePipeline
    {
        Task<T> ExecuteAsync<T>(InferencePipelineRequest request, CancellationToken ct = default);
        Task<BatchResult<T>> ExecuteBatchAsync<T>(IEnumerable<InferencePipelineRequest> requests, CancellationToken ct = default);
        InferencePipelineStats GetStats();
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default);
    }

}
