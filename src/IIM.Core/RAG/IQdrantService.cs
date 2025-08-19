// src/IIM.Core/RAG/InMemoryQdrantService.cs
using IIM.Shared.Models;


namespace IIM.Core.RAG
{
    public interface IQdrantService
    {
        Task<bool> CollectionExistsAsync(string collectionName, CancellationToken cancellationToken = default);
        Task<bool> CreateCaseCollectionAsync(string caseId, CancellationToken cancellationToken = default);
        Task<bool> CreateCollectionAsync(string collectionName, VectorConfig config, CancellationToken cancellationToken = default);
        Task<bool> CreateSnapshotAsync(string collectionName, CancellationToken cancellationToken = default);
        Task<bool> DeleteCaseCollectionAsync(string caseId, CancellationToken cancellationToken = default);
        Task<bool> DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default);
        Task<bool> DeletePointsAsync(string collectionName, List<string> ids, CancellationToken cancellationToken = default);
        Task<List<string>> FindSimilarPointsAsync(string collectionName, string pointId, int limit = 10, CancellationToken cancellationToken = default);
        Task<List<Cluster>> GetClustersAsync(string collectionName, int numClusters, CancellationToken cancellationToken = default);
        Task<CollectionInfo> GetCollectionInfoAsync(string collectionName, CancellationToken cancellationToken = default);
        Task<QdrantInfo> GetInfoAsync(CancellationToken cancellationToken = default);
        Task<VectorPoint?> GetPointAsync(string collectionName, string id, CancellationToken cancellationToken = default);
        Task<List<VectorPoint>> GetPointsAsync(string collectionName, List<string> ids, CancellationToken cancellationToken = default);
        Task<StorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default);
        Task<bool> IndexCaseDocumentAsync(string caseId, string documentId, string content, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
        Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default);
        Task<bool> OptimizeCollectionAsync(string collectionName, CancellationToken cancellationToken = default);
        Task<bool> PingAsync(CancellationToken cancellationToken = default);
        Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector, int limit = 10, float scoreThreshold = 0, SearchFilter? filter = null, CancellationToken cancellationToken = default);
        Task<List<SearchResult>> SearchBatchAsync(string collectionName, List<float[]> vectors, int limit = 10, CancellationToken cancellationToken = default);
        Task<List<SearchResult>> SearchByTextAsync(string collectionName, string text, int limit = 10, float scoreThreshold = 0, CancellationToken cancellationToken = default);
        Task<List<SearchResult>> SearchCaseAsync(string caseId, string query, int limit = 10, TimeRange? timeRange = null, CancellationToken cancellationToken = default);
        Task<bool> UpsertPointsAsync(string collectionName, List<VectorPoint> points, CancellationToken cancellationToken = default);
    }
}