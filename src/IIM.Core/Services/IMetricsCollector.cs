using System;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Core.Services
{
    /// <summary>
    /// Collects and aggregates inference metrics
    /// </summary>
    public interface IMetricsCollector
    {
        void RecordInferenceMetrics(InferenceMetrics metrics);
        MetricsSummary GetSummary(TimeSpan window);
        Dictionary<string, ModelMetrics> GetModelMetrics();
    }

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

    /// <summary>
    /// In-memory metrics collector
    /// </summary>
    public class InMemoryMetricsCollector : IMetricsCollector
    {
        private readonly List<InferenceMetrics> _metrics = new();
        private readonly object _lock = new();

        public void RecordInferenceMetrics(InferenceMetrics metrics)
        {
            lock (_lock)
            {
                _metrics.Add(metrics);

                // Keep only last hour of metrics
                var cutoff = DateTimeOffset.UtcNow.AddHours(-1);
                _metrics.RemoveAll(m => m.Timestamp < cutoff);
            }
        }

        public MetricsSummary GetSummary(TimeSpan window)
        {
            lock (_lock)
            {
                var cutoff = DateTimeOffset.UtcNow.Subtract(window);
                var windowMetrics = _metrics.Where(m => m.Timestamp > cutoff).ToList();

                if (!windowMetrics.Any())
                {
                    return new MetricsSummary();
                }

                var totalTimes = windowMetrics.Select(m => m.TotalTimeMs).OrderBy(t => t).ToArray();

                return new MetricsSummary
                {
                    TotalRequests = windowMetrics.Count,
                    AverageQueueTimeMs = windowMetrics.Average(m => m.QueueTimeMs),
                    AverageInferenceTimeMs = windowMetrics.Average(m => m.InferenceTimeMs),
                    AverageTotalTimeMs = windowMetrics.Average(m => m.TotalTimeMs),
                    P95TotalTimeMs = GetPercentile(totalTimes, 0.95),
                    P99TotalTimeMs = GetPercentile(totalTimes, 0.99),
                    AverageTokensPerSecond = windowMetrics.Where(m => m.TokensPerSecond > 0).DefaultIfEmpty().Average(m => m?.TokensPerSecond ?? 0),
                    RequestsByModel = windowMetrics.GroupBy(m => m.ModelId).ToDictionary(g => g.Key, g => g.Count())
                };
            }
        }

        public Dictionary<string, ModelMetrics> GetModelMetrics()
        {
            lock (_lock)
            {
                return _metrics
                    .GroupBy(m => m.ModelId)
                    .ToDictionary(
                        g => g.Key,
                        g => new ModelMetrics
                        {
                            ModelId = g.Key,
                            RequestCount = g.Count(),
                            AverageLatencyMs = g.Average(m => m.TotalTimeMs),
                            AverageTokensPerSecond = g.Where(m => m.TokensPerSecond > 0).DefaultIfEmpty().Average(m => m?.TokensPerSecond ?? 0),
                            TotalTokensGenerated = g.Sum(m => m.TokensGenerated)
                        });
            }
        }

        private double GetPercentile(long[] sortedArray, double percentile)
        {
            if (sortedArray.Length == 0) return 0;

            var index = (int)Math.Ceiling(percentile * sortedArray.Length) - 1;
            return sortedArray[Math.Max(0, Math.Min(index, sortedArray.Length - 1))];
        }
    }
}