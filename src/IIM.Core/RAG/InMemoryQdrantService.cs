// src/IIM.Core/RAG/InMemoryQdrantService.cs
using IIM.Core.Configuration;
using IIM.Infrastructure.Storage;
using IIM.Shared.DTOs;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Core.RAG
{
    /// <summary>
    /// In-memory implementation of IQdrantService for development and testing
    /// Provides functional vector storage without external dependencies
    /// </summary>
    public class InMemoryQdrantService : IQdrantService, IDisposable
    {
        private readonly ILogger<IQdrantService> _logger;
        private readonly StorageConfiguration _config;
        private readonly Dictionary<string, Collection> _collections = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private class Collection
        {
            public string Name { get; set; } = string.Empty;
            public VectorConfig Config { get; set; } = new();
            public Dictionary<string, VectorPoint> Points { get; set; } = new();
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        public InMemoryQdrantService(ILogger<IQdrantService> logger, StorageConfiguration config)
        {
            _logger = logger;
            _config = config;
            _config.EnsureDirectoriesExist();
            LoadCollections();
        }

        // ===== Health & Connection =====

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> PingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<QdrantInfo> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            var totalPoints = _collections.Values.Sum(c => c.Points.Count);
            return Task.FromResult(new QdrantInfo
            {
                Version = "InMemory-1.0",
                Status = "Healthy",
                Collections = _collections.Count,
                TotalPoints = totalPoints,
                StorageUsedBytes = totalPoints * 1536 // Estimate: 384 floats * 4 bytes
            });
        }

        // ===== Collection Management =====

        public async Task<bool> CreateCollectionAsync(string collectionName, VectorConfig config,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_collections.ContainsKey(collectionName))
                    return false;

                _collections[collectionName] = new Collection
                {
                    Name = collectionName,
                    Config = config,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                SaveCollection(collectionName);
                _logger.LogInformation("Created collection {Collection}", collectionName);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeleteCollectionAsync(string collectionName,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_collections.Remove(collectionName))
                {
                    var filePath = GetCollectionPath(collectionName);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    return true;
                }
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<bool> CollectionExistsAsync(string collectionName,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_collections.ContainsKey(collectionName));

        public Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_collections.Keys.ToList());

        public Task<CollectionInfo> GetCollectionInfoAsync(string collectionName,
            CancellationToken cancellationToken = default)
        {
            if (!_collections.TryGetValue(collectionName, out var collection))
                throw new KeyNotFoundException($"Collection {collectionName} not found");

            return Task.FromResult(new CollectionInfo
            {
                Name = collection.Name,
                Config = collection.Config,
                PointsCount = collection.Points.Count,
                SegmentsCount = 1,
                Status = "Ready"
            });
        }

        // ===== Point Operations =====

        public async Task<bool> UpsertPointsAsync(string collectionName, List<VectorPoint> points,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_collections.TryGetValue(collectionName, out var collection))
                    return false;

                foreach (var point in points)
                    collection.Points[point.Id] = point;

                collection.UpdatedAt = DateTime.UtcNow;
                SaveCollection(collectionName);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeletePointsAsync(string collectionName, List<string> ids,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_collections.TryGetValue(collectionName, out var collection))
                    return false;

                var deleted = ids.Count(id => collection.Points.Remove(id));
                if (deleted > 0)
                {
                    collection.UpdatedAt = DateTime.UtcNow;
                    SaveCollection(collectionName);
                }
                return deleted > 0;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // In InMemoryQdrantService.cs - Fix GetPointAsync method
        public Task<VectorPoint?> GetPointAsync(string collectionName, string id,
            CancellationToken cancellationToken = default)
        {
            if (_collections.TryGetValue(collectionName, out var collection))
            {
                collection.Points.TryGetValue(id, out var point);
                return Task.FromResult(point); // point can be null, that's ok
            }

            return Task.FromResult<VectorPoint?>(null);
        }

        public Task<List<VectorPoint>> GetPointsAsync(string collectionName, List<string> ids,
            CancellationToken cancellationToken = default)
        {
            var points = new List<VectorPoint>();
            if (_collections.TryGetValue(collectionName, out var collection))
            {
                foreach (var id in ids)
                {
                    if (collection.Points.TryGetValue(id, out var point))
                        points.Add(point);
                }
            }
            return Task.FromResult(points);
        }

        // ===== Search Operations =====

        public async Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector,
            int limit = 10, float scoreThreshold = 0, SearchFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_collections.TryGetValue(collectionName, out var collection))
                    return new List<SearchResult>();

                var results = collection.Points.Values
                    .Where(p => filter == null || PassesFilter(p, filter))
                    .Select(p => new SearchResult
                    {
                        Id = p.Id,
                        Score = CosineSimilarity(vector, p.Vector),
                        Payload = p.Payload,
                        Vector = p.Vector
                    })
                    .Where(r => r.Score >= scoreThreshold)
                    .OrderByDescending(r => r.Score)
                    .Take(limit)
                    .ToList();

                return results;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<SearchResult>> SearchBatchAsync(string collectionName,
            List<float[]> vectors, int limit = 10, CancellationToken cancellationToken = default)
        {
            var allResults = new List<SearchResult>();
            foreach (var vector in vectors)
            {
                var results = await SearchAsync(collectionName, vector, limit, 0, null, cancellationToken);
                allResults.AddRange(results);
            }

            return allResults
                .GroupBy(r => r.Id)
                .Select(g => g.OrderByDescending(r => r.Score).First())
                .OrderByDescending(r => r.Score)
                .Take(limit)
                .ToList();
        }

        public Task<List<SearchResult>> SearchByTextAsync(string collectionName, string text,
            int limit = 10, float scoreThreshold = 0, CancellationToken cancellationToken = default)
        {
            // This would require an embedding service - return empty for now
            _logger.LogWarning("SearchByTextAsync requires embedding service");
            return Task.FromResult(new List<SearchResult>());
        }

        // ===== Case-Specific Operations =====

        public Task<bool> CreateCaseCollectionAsync(string caseId,
            CancellationToken cancellationToken = default)
        {
            return CreateCollectionAsync($"case_{caseId}", new VectorConfig
            {
                Dimensions = 384,
                Distance = "Cosine"
            }, cancellationToken);
        }

        public async Task<bool> IndexCaseDocumentAsync(string caseId, string documentId,
            string content, Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            var collectionName = $"case_{caseId}";
            if (!await CollectionExistsAsync(collectionName, cancellationToken))
                await CreateCaseCollectionAsync(caseId, cancellationToken);

            // Create a simple hash-based vector for testing
            var vector = CreateMockVector(content);
            var point = new VectorPoint
            {
                Id = documentId,
                Vector = vector,
                Payload = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTimeOffset.UtcNow
            };
            point.Payload["content"] = content;
            point.Payload["case_id"] = caseId;

            return await UpsertPointsAsync(collectionName, new List<VectorPoint> { point }, cancellationToken);
        }

        public Task<List<SearchResult>> SearchCaseAsync(string caseId, string query,
            int limit = 10, TimeRange? timeRange = null, CancellationToken cancellationToken = default)
        {
            // Would need embedding service for real implementation
            return Task.FromResult(new List<SearchResult>());
        }

        public Task<bool> DeleteCaseCollectionAsync(string caseId,
            CancellationToken cancellationToken = default)
            => DeleteCollectionAsync($"case_{caseId}", cancellationToken);

        // ===== Analysis Operations (Simplified) =====

        public Task<List<Cluster>> GetClustersAsync(string collectionName, int numClusters,
            CancellationToken cancellationToken = default)
        {
            if (!_collections.TryGetValue(collectionName, out var collection))
                return Task.FromResult(new List<Cluster>());

            // Simple clustering - divide points equally
            var clusters = new List<Cluster>();
            var pointsList = collection.Points.Values.ToList();
            var pointsPerCluster = Math.Max(1, pointsList.Count / numClusters);

            for (int i = 0; i < numClusters && i * pointsPerCluster < pointsList.Count; i++)
            {
                var clusterPoints = pointsList.Skip(i * pointsPerCluster).Take(pointsPerCluster).ToList();
                if (clusterPoints.Any())
                {
                    clusters.Add(new Cluster
                    {
                        Id = i,
                        PointIds = clusterPoints.Select(p => p.Id).ToList(),
                        Size = clusterPoints.Count,
                        Centroid = CalculateCentroid(clusterPoints.Select(p => p.Vector).ToList())
                    });
                }
            }
            return Task.FromResult(clusters);
        }

        public async Task<List<string>> FindSimilarPointsAsync(string collectionName, string pointId,
            int limit = 10, CancellationToken cancellationToken = default)
        {
            var point = await GetPointAsync(collectionName, pointId, cancellationToken);
            if (point == null)
                return new List<string>();

            var results = await SearchAsync(collectionName, point.Vector, limit + 1, 0, null, cancellationToken);
            return results.Where(r => r.Id != pointId).Take(limit).Select(r => r.Id).ToList();
        }

        // ===== Maintenance =====

        public Task<bool> CreateSnapshotAsync(string collectionName,
            CancellationToken cancellationToken = default)
        {
            if (!_collections.TryGetValue(collectionName, out var collection))
                return Task.FromResult(false);

            var snapshotDir = Path.Combine(_config.VectorStorePath, "snapshots");
            Directory.CreateDirectory(snapshotDir);

            var snapshotPath = Path.Combine(snapshotDir,
                $"{collectionName}_{DateTime.UtcNow:yyyyMMddHHmmss}.snapshot");

            var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(snapshotPath, json);

            return Task.FromResult(true);
        }

        public Task<bool> OptimizeCollectionAsync(string collectionName,
            CancellationToken cancellationToken = default)
        {
            // No optimization needed for in-memory implementation
            return Task.FromResult(_collections.ContainsKey(collectionName));
        }

        public Task<StorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default)
        {
            var collectionSizes = _collections.ToDictionary(
                kvp => kvp.Key,
                kvp => (long)(kvp.Value.Points.Count * kvp.Value.Config.Dimensions * 4));

            return Task.FromResult(new StorageInfo
            {
                TotalBytes = 100L * 1024 * 1024 * 1024, // 100GB available
                UsedBytes = collectionSizes.Values.Sum(),
                AvailableBytes = (100L * 1024 * 1024 * 1024) - collectionSizes.Values.Sum(),
                CollectionsCount = _collections.Count,
                CollectionSizes = collectionSizes
            });
        }

        // ===== Private Helper Methods =====

        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) return 0;

            float dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            return (normA == 0 || normB == 0) ? 0 : dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private bool PassesFilter(VectorPoint point, SearchFilter filter)
        {
            if (filter.Must != null)
            {
                foreach (var kvp in filter.Must)
                {
                    if (!point.Payload.TryGetValue(kvp.Key, out var value) || !value.Equals(kvp.Value))
                        return false;
                }
            }

            if (filter.MustNot != null)
            {
                foreach (var kvp in filter.MustNot)
                {
                    if (point.Payload.TryGetValue(kvp.Key, out var value) && value.Equals(kvp.Value))
                        return false;
                }
            }

            return true;
        }

        private float[] CalculateCentroid(List<float[]> vectors)
        {
            if (!vectors.Any()) return Array.Empty<float>();

            var dimensions = vectors[0].Length;
            var centroid = new float[dimensions];

            for (int d = 0; d < dimensions; d++)
                centroid[d] = vectors.Average(v => v[d]);

            return centroid;
        }

        private float[] CreateMockVector(string content)
        {
            // Create a deterministic mock vector based on content hash
            var hash = content.GetHashCode();
            var random = new Random(hash);
            var vector = new float[384];
            for (int i = 0; i < vector.Length; i++)
                vector[i] = (float)random.NextDouble();
            return vector;
        }

        private void SaveCollection(string collectionName)
        {
            if (!_collections.TryGetValue(collectionName, out var collection))
                return;

            var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetCollectionPath(collectionName), json);
        }

        private string GetCollectionPath(string collectionName)
            => Path.Combine(_config.VectorStorePath, $"{collectionName}.json");

        private void LoadCollections()
        {
            if (!Directory.Exists(_config.VectorStorePath))
                return;

            foreach (var file in Directory.GetFiles(_config.VectorStorePath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var collection = JsonSerializer.Deserialize<Collection>(json);
                    if (collection != null)
                        _collections[collection.Name] = collection;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load collection from {File}", file);
                }
            }
        }

        public void Dispose() => _semaphore?.Dispose();
    }
}