using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Inference;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

            if (result.IsHealthy)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Inference pipeline is healthy", result.Stats);
            }

            if (result.Issues.Count > 2)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    $"Inference pipeline has critical issues: {string.Join("; ", result.Issues)}",
                    data: result.Stats);
            }

            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                $"Inference pipeline has issues: {string.Join("; ", result.Issues)}",
                data: result.Stats);
        }
    }
}