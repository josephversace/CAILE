using IIM.Core.Inference;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.HealthChecks
{
    /// <summary>
    /// Health check for the inference pipeline
    /// </summary>
    public class InferencePipelineHealthCheck : IHealthCheck
    {
        private readonly IInferencePipeline _pipeline;

        public InferencePipelineHealthCheck(IInferencePipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken ct = default)
        {
            var result = await _pipeline.CheckHealthAsync(ct);

            var healthData = ToHealthData(result.Stats);

            if (result.IsHealthy)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    "Inference pipeline is healthy", healthData);
            }

            if (result.Issues.Count > 2)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    $"Inference pipeline has critical issues: {string.Join("; ", result.Issues)}",
                    data: healthData);
            }

            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                $"Inference pipeline has issues: {string.Join("; ", result.Issues)}",
                data: healthData);
        }

        private static IReadOnlyDictionary<string, object> ToHealthData(InferencePipelineStats stats)
        {
            return new Dictionary<string, object>
            {
                ["TotalRequests"] = stats.TotalRequests,
                ["CompletedRequests"] = stats.CompletedRequests,
                ["FailedRequests"] = stats.FailedRequests,
                ["PendingRequests"] = stats.PendingRequests,
                ["HighPriorityQueueDepth"] = stats.HighPriorityQueueDepth,
                ["NormalPriorityQueueDepth"] = stats.NormalPriorityQueueDepth,
                ["LowPriorityQueueDepth"] = stats.LowPriorityQueueDepth,
                ["GpuSlotsAvailable"] = stats.GpuSlotsAvailable,
                ["CpuSlotsAvailable"] = stats.CpuSlotsAvailable,
                ["AverageLatencyMs"] = stats.AverageLatencyMs,
                ["P95LatencyMs"] = stats.P95LatencyMs,
                ["P99LatencyMs"] = stats.P99LatencyMs
                // Add more fields if you wish
            };
        }

    }
}