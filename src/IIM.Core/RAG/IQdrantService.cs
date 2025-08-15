// ============================================
// File: src/IIM.Core/RAG/IQdrantService.cs
// Purpose: Vector database interface for RAG operations
// ============================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.RAG
{
    /// <summary>
    /// Service interface for Qdrant vector database operations.
    /// Handles embeddings, similarity search, and collection management.
    /// </summary>
    public interface IQdrantService
    {
        // Health & Connection
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
        Task<QdrantInfo> GetInfoAsync(CancellationToken cancellationToken = default);
        Task<bool> PingAsync(CancellationToken cancellationToken = default);

        // Collection Management
        Task<bool> CreateCollectionAsync(string collectionName, VectorConfig config, CancellationToken cancellationToken = default);
        Task<bool> DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default);
        Task<bool> CollectionExistsAsync(string collectionName, CancellationToken cancellationToken = default);
        Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default);
        Task<CollectionInfo> GetCollectionInfoAsync(string collectionName, CancellationToken cancellationToken = default);

        // Point Operations (CRUD for vectors)
        Task<bool> UpsertPointsAsync(string collectionName, List<VectorPoint> points, CancellationToken cancellationToken = default);
        Task<bool> DeletePointsAsync(string collectionName, List<string> ids, CancellationToken cancellationToken = default);
        Task<VectorPoint?> GetPointAsync(string collectionName, string id, CancellationToken cancellationToken = default);
        Task<List<VectorPoint>> GetPointsAsync(string collectionName, List<string> ids, CancellationToken cancellationToken = default);

        // Search Operations
        Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector, int limit = 10, float scoreThreshold = 0.0f, SearchFilter? filter = null, CancellationToken cancellationToken = default);
        Task<List<SearchResult>> SearchBatchAsync(string collectionName, List<float[]> vectors, int limit = 10, CancellationToken cancellationToken = default);
        Task<List<SearchResult>> SearchByTextAsync(string collectionName, string text, int limit = 10, float scoreThreshold = 0.0f, CancellationToken cancellationToken = default);

        // Case-Specific Operations
        Task<bool> CreateCaseCollectionAsync(string caseId, CancellationToken cancellationToken = default);
        Task<bool> IndexCaseDocumentAsync(string caseId, string documentId, string content, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
        Task<List<SearchResult>> SearchCaseAsync(string caseId, string query, int limit = 10, TimeRange? timeRange = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteCaseCollectionAsync(string caseId, CancellationToken cancellationToken = default);

        // Clustering & Analysis
        Task<List<Cluster>> GetClustersAsync(string collectionName, int numClusters, CancellationToken cancellationToken = default);
        Task<List<string>> FindSimilarPointsAsync(string collectionName, string pointId, int limit = 10, CancellationToken cancellationToken = default);

        // Backup & Maintenance
        Task<bool> CreateSnapshotAsync(string collectionName, CancellationToken cancellationToken = default);
        Task<bool> OptimizeCollectionAsync(string collectionName, CancellationToken cancellationToken = default);
        Task<StorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default);
    }

    // DTOs for Qdrant operations
    public class QdrantInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Collections { get; set; }
        public long TotalPoints { get; set; }
        public long StorageUsedBytes { get; set; }
    }

    public class VectorConfig
    {
        public int Dimensions { get; set; } = 768; // Default for nomic-embed-text
        public string Distance { get; set; } = "Cosine"; // Cosine, Euclidean, Dot
        public bool OnDisk { get; set; } = false;
        public int? QuantizationConfig { get; set; } // Optional quantization bits
    }

    public class CollectionInfo
    {
        public string Name { get; set; } = string.Empty;
        public VectorConfig Config { get; set; } = new();
        public long PointsCount { get; set; }
        public long SegmentsCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class VectorPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public DateTimeOffset? Timestamp { get; set; }
    }

    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public float[] Vector { get; set; } = Array.Empty<float>();
        public Dictionary<string, object> Payload { get; set; } = new();
        public float? Distance { get; set; }
    }

    public class SearchFilter
    {
        public Dictionary<string, object>? Must { get; set; }
        public Dictionary<string, object>? Should { get; set; }
        public Dictionary<string, object>? MustNot { get; set; }
        public TimeRange? TimeRange { get; set; }
        public List<string>? Tags { get; set; }
    }

    public class TimeRange
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }

    public class Cluster
    {
        public int Id { get; set; }
        public float[] Centroid { get; set; } = Array.Empty<float>();
        public List<string> PointIds { get; set; } = new();
        public int Size { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class StorageInfo
    {
        public long TotalBytes { get; set; }
        public long UsedBytes { get; set; }
        public long AvailableBytes { get; set; }
        public int CollectionsCount { get; set; }
        public Dictionary<string, long> CollectionSizes { get; set; } = new();
    }
}