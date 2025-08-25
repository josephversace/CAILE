using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Shared.Interfaces
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



}