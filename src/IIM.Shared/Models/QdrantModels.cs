
using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Information about the Qdrant service
    /// </summary>
    public class QdrantInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Collections { get; set; }
        public long TotalPoints { get; set; }
        public long StorageUsedBytes { get; set; }
    }

    /// <summary>
    /// Vector collection configuration
    /// </summary>
    public class VectorConfig
    {
        public int Dimensions { get; set; } = 384; // Default for all-MiniLM-L6-v2
        public string Distance { get; set; } = "Cosine"; // Cosine, Euclidean, Dot
        public bool OnDisk { get; set; } = false;
        public int? QuantizationConfig { get; set; }
    }

    /// <summary>
    /// Information about a collection
    /// </summary>
    public class CollectionInfo
    {
        public string Name { get; set; } = string.Empty;
        public VectorConfig Config { get; set; } = new();
        public long PointsCount { get; set; }
        public long SegmentsCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// A vector point in the database
    /// </summary>
    public class VectorPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public DateTimeOffset? Timestamp { get; set; }
    }

    /// <summary>
    /// Search result from vector search
    /// </summary>
    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public float? Distance { get; set; }
    }

    /// <summary>
    /// Filter for search operations
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
    /// Cluster information for analysis
    /// </summary>
    public class Cluster
    {
        public int Id { get; set; }
        public float[] Centroid { get; set; } = Array.Empty<float>();
        public List<string> PointIds { get; set; } = new();
        public int Size { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Storage information
    /// </summary>
    public class StorageInfo
    {
        public long TotalBytes { get; set; }
        public long UsedBytes { get; set; }
        public long AvailableBytes { get; set; }
        public int CollectionsCount { get; set; }
        public Dictionary<string, long> CollectionSizes { get; set; } = new();
    }
}