using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class InferenceMetrics
    {
        public string ModelId { get; set; } = string.Empty;
        public long QueueTimeMs { get; set; }
        public long InferenceTimeMs { get; set; }
        public long TotalTimeMs { get; set; }
        public int TokensGenerated { get; set; }
        public double TokensPerSecond { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }

    public class MetricsSummary
    {
        public int TotalRequests { get; set; }
        public double AverageQueueTimeMs { get; set; }
        public double AverageInferenceTimeMs { get; set; }
        public double AverageTotalTimeMs { get; set; }
        public double P95TotalTimeMs { get; set; }
        public double P99TotalTimeMs { get; set; }
        public double AverageTokensPerSecond { get; set; }
        public Dictionary<string, int> RequestsByModel { get; set; } = new();
    }

    public class ModelMetrics
    {
        public string ModelId { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public double AverageLatencyMs { get; set; }
        public double AverageTokensPerSecond { get; set; }
        public int TotalTokensGenerated { get; set; }
    }
}
