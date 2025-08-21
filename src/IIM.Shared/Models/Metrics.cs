using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    // Service Management
    public class ServiceStatus
    {
        public string Name { get; set; } = string.Empty;
        public ServiceState State { get; set; }
        public bool IsHealthy { get; set; }
        public string? Endpoint { get; set; }
        public string? Version { get; set; }
        public long MemoryUsage { get; set; }
        public double CpuUsage { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public string? Message { get; set; }
    }

    public class ServiceConfig
    {
        public string Name { get; set; } = string.Empty;
        public ServiceType Type { get; set; }
        public string? DockerImage { get; set; }
        public int Port { get; set; }
        public string? HealthEndpoint { get; set; }
        public string StartupCommand { get; set; } = string.Empty;
        public string? WorkingDirectory { get; set; }
        public long RequiredMemoryMb { get; set; }
        public ServicePriority Priority { get; set; }
        public Dictionary<string, string> Environment { get; set; } = new();
    }

    /// <summary>
    /// Performance metrics for models, inference, and system monitoring
    /// </summary>
    public class PerformanceMetrics
    {

        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkLatency { get; set; }
        public int ActiveConnections { get; set; }
        public int RequestsPerSecond { get; set; }
        public double AverageResponseTime { get; set; }

        // Model Performance Metrics
        public double TokensPerSecond { get; set; }
        public double AverageLatencyMs { get; set; }
        public double P50LatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public double P99LatencyMs { get; set; }
        public double Throughput { get; set; }

        // Quality Metrics
        public double Accuracy { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1Score { get; set; }
        public double Confidence { get; set; }

        // Resource Utilization
        public long VramUsageBytes { get; set; }
        public long RamUsageBytes { get; set; }
        public double GpuUtilization { get; set; }
        public double GpuTemperature { get; set; }
        public double PowerConsumptionWatts { get; set; }

        // Inference Metrics
        public long TotalInferences { get; set; }
        public long SuccessfulInferences { get; set; }
        public long FailedInferences { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }

        // Queue Metrics
        public int QueueLength { get; set; }
        public int ActiveRequests { get; set; }
        public int PendingRequests { get; set; }
        public double QueueWaitTimeMs { get; set; }

        // Model-Specific Metrics
        public string ModelId { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public int ContextLength { get; set; }
        public int BatchSize { get; set; }
        public int MaxTokens { get; set; }

        // Timestamp
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public TimeSpan MeasurementPeriod { get; set; } = TimeSpan.FromMinutes(1);

        // Additional Metrics Dictionary for extensibility
        public Dictionary<string, object> CustomMetrics { get; set; } = new();

        // Helper Methods
        public double GetEfficiency()
        {
            return SuccessfulInferences > 0
                ? (double)SuccessfulInferences / TotalInferences * 100
                : 0;
        }

        public double GetAverageTokensPerInference()
        {
            return TotalInferences > 0
                ? TokensPerSecond * AverageProcessingTime.TotalSeconds
                : 0;
        }

        public bool IsHealthy()
        {
            return CpuUsage < 90
                && MemoryUsage < 90
                && GpuUtilization < 95
                && SuccessRate > 95
                && AverageLatencyMs < 5000;
        }

        public string GetHealthStatus()
        {
            if (!IsHealthy()) return "Unhealthy";
            if (CpuUsage > 70 || MemoryUsage > 70) return "Warning";
            return "Healthy";
        }
    }


    /// <summary>
    /// Aggregated performance metrics over time
    /// </summary>
    public class AggregatedPerformanceMetrics
    {
        public string MetricId { get; set; } = Guid.NewGuid().ToString("N");
        public string ModelId { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        // Aggregated values
        public double MinTokensPerSecond { get; set; }
        public double MaxTokensPerSecond { get; set; }
        public double AvgTokensPerSecond { get; set; }

        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public double AvgLatencyMs { get; set; }

        public double MinAccuracy { get; set; }
        public double MaxAccuracy { get; set; }
        public double AvgAccuracy { get; set; }

        public long TotalRequests { get; set; }
        public long TotalTokensProcessed { get; set; }
        public double TotalGpuHours { get; set; }

        public List<PerformanceMetrics> Samples { get; set; } = new();
    }

    /// <summary>
    /// Performance benchmark result
    /// </summary>
    public class PerformanceBenchmark
    {
        public string BenchmarkId { get; set; } = Guid.NewGuid().ToString("N");
        public string ModelId { get; set; } = string.Empty;
        public string BenchmarkName { get; set; } = string.Empty;
        public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;

        // Test parameters
        public int NumberOfRuns { get; set; }
        public int BatchSize { get; set; }
        public int SequenceLength { get; set; }
        public string TestDataset { get; set; } = string.Empty;

        // Results
        public double TokensPerSecond { get; set; }
        public double AverageLatencyMs { get; set; }
        public double Accuracy { get; set; }
        public double MemoryUsageGb { get; set; }
        public double PowerEfficiency { get; set; } // Tokens per watt

        public Dictionary<string, object> DetailedResults { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }
}
