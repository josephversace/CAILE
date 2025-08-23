using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Vector database configuration
    /// </summary>
    public class VectorConfig
    {
        public int Dimension { get; set; }
        public string Distance { get; set; } = "Cosine"; // Cosine, Euclidean, Dot
        public Dictionary<string, object>? IndexConfig { get; set; }
        public Dictionary<string, object>? OptimizerConfig { get; set; }
        public bool OnDisk { get; set; } = false;
    }

    /// <summary>
    /// Vector point in database
    /// </summary>
    public class VectorPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public DateTimeOffset? Timestamp { get; set; }
    }

    /// <summary>
    /// Search result from vector database
    /// </summary>
    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public int? Version { get; set; }
    }

    /// <summary>
    /// Search filter for vector queries
    /// </summary>
    public class SearchFilter
    {
        public Dictionary<string, object>? Must { get; set; }
        public Dictionary<string, object>? Should { get; set; }
        public Dictionary<string, object>? MustNot { get; set; }
        public TimeRange? TimeRange { get; set; }
        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// Time range filter
    /// </summary>
    public class TimeRange
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public string? Field { get; set; } = "timestamp";
    }

    /// <summary>
    /// Qdrant cluster information
    /// </summary>
    public class Cluster
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Nodes { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? Config { get; set; }
    }

    /// <summary>
    /// Collection information in Qdrant
    /// </summary>
    public class CollectionInfo
    {
        public string Name { get; set; } = string.Empty;
        public long PointsCount { get; set; }
        public long IndexedVectorsCount { get; set; }
        public VectorConfig Config { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Qdrant service information
    /// </summary>
    public class QdrantInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<CollectionInfo> Collections { get; set; } = new();
        public StorageInfo Storage { get; set; } = new();
        public Dictionary<string, object>? Metrics { get; set; }
    }

    /// <summary>
    /// Storage information for Qdrant
    /// </summary>
    public class StorageInfo
    {
        public long TotalSpace { get; set; }
        public long UsedSpace { get; set; }
        public long FreeSpace { get; set; }
        public double UsagePercentage => TotalSpace > 0 ? (double)UsedSpace / TotalSpace * 100 : 0;
        public Dictionary<string, long> CollectionSizes { get; set; } = new();
    }
}