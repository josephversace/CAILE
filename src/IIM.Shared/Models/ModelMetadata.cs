using System;
using System.Collections.Generic;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Model metadata for routing and resource management
    /// This is in Shared because both Core and Infrastructure need it
    /// </summary>
    public class ModelMetadata
    {
        public string ModelId { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public bool RequiresGpu { get; set; }
        public bool SupportsBatching { get; set; }
        public int MaxBatchSize { get; set; } = 1;
        public long EstimatedMemoryMb { get; set; }
        public int DefaultPriority { get; set; } = 1; // 0=Low, 1=Normal, 2=High
        public string Provider { get; set; } = "cpu";
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }    
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
