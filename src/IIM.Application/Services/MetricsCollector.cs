using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Application.Services
{


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