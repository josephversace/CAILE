using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.DTOs; 
using IIM.Shared.Interfaces; 
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.VectorStore
{
    /// <summary>
    /// Production implementation of IQdrantService connecting to Qdrant vector database
    /// This replaces the InMemoryQdrantService with actual HTTP calls to Qdrant
    /// </summary>
    public class QdrantService : IQdrantService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<QdrantService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of QdrantService
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory for creating clients</param>
        /// <param name="logger">Logger for diagnostic output</param>
        public QdrantService(IHttpClientFactory httpClientFactory, ILogger<QdrantService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("qdrant");
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        #region Collection Management

        /// <summary>
        /// Checks if a collection exists in Qdrant
        /// </summary>
        /// <param name="collectionName">Name of the collection to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if collection exists, false otherwise</returns>
        public async Task<bool> CollectionExistsAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/collections/{collectionName}", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if collection {Collection} exists", collectionName);
                return false;
            }
        }

        /// <summary>
        /// Creates a collection specifically for a case with default settings
        /// </summary>
        /// <param name="caseId">Case identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if collection was created successfully</returns>
        public async Task<bool> CreateCaseCollectionAsync(string caseId, CancellationToken cancellationToken = default)
        {
            var collectionName = $"case_{caseId}";
            var config = new VectorConfig
            {
                Dimensions = 384, // Default for all-MiniLM-L6-v2
                Distance = "Cosine"
            };

            return await CreateCollectionAsync(collectionName, config, cancellationToken);
        }

        /// <summary>
        /// Creates a collection with specified vector configuration
        /// </summary>
        /// <param name="collectionName">Name of the collection to create</param>
        /// <param name="config">Vector configuration settings</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if collection was created successfully</returns>
        public async Task<bool> CreateCollectionAsync(string collectionName, VectorConfig config, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    vectors = new
                    {
                        size = config.Dimensions,
                        distance = config.Distance
                    },
                    on_disk = config.OnDisk,
                    quantization_config = config.QuantizationConfig
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"/collections/{collectionName}",
                    request,
                    _jsonOptions,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Created collection {Collection} with {Dimensions}D vectors",
                        collectionName, config.Dimensions);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create collection {Collection}: {Error}", collectionName, error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating collection {Collection}", collectionName);
                return false;
            }
        }

        /// <summary>
        /// Creates a snapshot of a collection for backup purposes
        /// </summary>
        /// <param name="collectionName">Name of the collection to snapshot</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if snapshot was created successfully</returns>
        public async Task<bool> CreateSnapshotAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsync(
                    $"/collections/{collectionName}/snapshots",
                    null,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snapshot for {Collection}", collectionName);
                return false;
            }
        }

        /// <summary>
        /// Deletes a case-specific collection
        /// </summary>
        /// <param name="caseId">Case identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if collection was deleted successfully</returns>
        public async Task<bool> DeleteCaseCollectionAsync(string caseId, CancellationToken cancellationToken = default)
        {
            var collectionName = $"case_{caseId}";
            return await DeleteCollectionAsync(collectionName, cancellationToken);
        }

        /// <summary>
        /// Deletes a collection from Qdrant
        /// </summary>
        /// <param name="collectionName">Name of the collection to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if collection was deleted successfully</returns>
        public async Task<bool> DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/collections/{collectionName}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Deleted collection {Collection}", collectionName);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting collection {Collection}", collectionName);
                return false;
            }
        }

        /// <summary>
        /// Gets detailed information about a collection
        /// </summary>
        /// <param name="collectionName">Name of the collection</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection information or null if not found</returns>
        public async Task<CollectionInfo> GetCollectionInfoAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/collections/{collectionName}", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new CollectionInfo { Name = collectionName, Status = "not_found" };
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonDocument.Parse(json);
                var result = data.RootElement.GetProperty("result");

                var config = result.GetProperty("config").GetProperty("params").GetProperty("vectors");

                return new CollectionInfo
                {
                    Name = collectionName,
                    Config = new VectorConfig
                    {
                        Dimensions = config.GetProperty("size").GetInt32(),
                        Distance = config.TryGetProperty("distance", out var dist) ? dist.GetString() ?? "Cosine" : "Cosine"
                    },
                    PointsCount = result.GetProperty("points_count").GetInt64(),
                    SegmentsCount = result.TryGetProperty("segments_count", out var seg) ? seg.GetInt64() : 0,
                    Status = result.GetProperty("status").GetString() ?? "unknown"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting info for collection {Collection}", collectionName);
                return new CollectionInfo { Name = collectionName, Status = "error" };
            }
        }

        /// <summary>
        /// Lists all collections in Qdrant
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of collection names</returns>
        public async Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("/collections", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<string>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonDocument.Parse(json);
                var collections = data.RootElement.GetProperty("result").GetProperty("collections");

                var names = new List<string>();
                foreach (var collection in collections.EnumerateArray())
                {
                    if (collection.TryGetProperty("name", out var name))
                    {
                        names.Add(name.GetString() ?? "");
                    }
                }

                return names;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing collections");
                return new List<string>();
            }
        }

        /// <summary>
        /// Optimizes a collection for better performance
        /// </summary>
        /// <param name="collectionName">Name of the collection to optimize</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if optimization was successful</returns>
        public async Task<bool> OptimizeCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsync(
                    $"/collections/{collectionName}/index",
                    null,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing collection {Collection}", collectionName);
                return false;
            }
        }

        #endregion

        #region Point Operations

        /// <summary>
        /// Deletes multiple points from a collection
        /// </summary>
        /// <param name="collectionName">Collection containing the points</param>
        /// <param name="ids">List of point IDs to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all points were deleted successfully</returns>
        public async Task<bool> DeletePointsAsync(string collectionName, List<string> ids, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new { points = ids };

                var response = await _httpClient.PostAsJsonAsync(
                    $"/collections/{collectionName}/points/delete",
                    request,
                    _jsonOptions,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting points from {Collection}", collectionName);
                return false;
            }
        }

        /// <summary>
        /// Gets a single point by ID
        /// </summary>
        /// <param name="collectionName">Collection containing the point</param>
        /// <param name="id">Point ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Vector point or null if not found</returns>
        public async Task<VectorPoint?> GetPointAsync(string collectionName, string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/collections/{collectionName}/points/{id}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonDocument.Parse(json);
                var result = data.RootElement.GetProperty("result");

                return new VectorPoint
                {
                    Id = result.GetProperty("id").GetString() ?? "",
                    Vector = ParseVector(result.GetProperty("vector")),
                    Payload = ParsePayload(result.GetProperty("payload"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting point {Id} from {Collection}", id, collectionName);
                return null;
            }
        }

        /// <summary>
        /// Gets multiple points by IDs
        /// </summary>
        /// <param name="collectionName">Collection containing the points</param>
        /// <param name="ids">List of point IDs</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of vector points</returns>
        public async Task<List<VectorPoint>> GetPointsAsync(string collectionName, List<string> ids, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new { ids = ids, with_vector = true, with_payload = true };

                var response = await _httpClient.PostAsJsonAsync(
                    $"/collections/{collectionName}/points",
                    request,
                    _jsonOptions,
                    cancellationToken);

                if (!response.IsSuccessStatusCode) return new List<VectorPoint>();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonDocument.Parse(json);
                var result = data.RootElement.GetProperty("result");

                var points = new List<VectorPoint>();
                foreach (var item in result.EnumerateArray())
                {
                    points.Add(new VectorPoint
                    {
                        Id = item.GetProperty("id").GetString() ?? "",
                        Vector = ParseVector(item.GetProperty("vector")),
                        Payload = ParsePayload(item.GetProperty("payload"))
                    });
                }

                return points;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting points from {Collection}", collectionName);
                return new List<VectorPoint>();
            }
        }

        /// <summary>
        /// Inserts or updates points in a collection
        /// </summary>
        /// <param name="collectionName">Target collection</param>
        /// <param name="points">Points to upsert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all points were upserted successfully</returns>
        public async Task<bool> UpsertPointsAsync(string collectionName, List<VectorPoint> points, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    points = points.Select(p => new
                    {
                        id = p.Id,
                        vector = p.Vector,
                        payload = p.Payload
                    }).ToList()
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"/collections/{collectionName}/points",
                    request,
                    _jsonOptions,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Upserted {Count} points to {Collection}", points.Count, collectionName);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to upsert points: {Error}", error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting points to {Collection}", collectionName);
                return false;
            }
        }

        #endregion

        #region Search Operations

        /// <summary>
        /// Searches for similar vectors using cosine similarity
        /// </summary>
        /// <param name="collectionName">Collection to search</param>
        /// <param name="vector">Query vector</param>
        /// <param name="limit">Maximum results to return</param>
        /// <param name="scoreThreshold">Minimum similarity score</param>
        /// <param name="filter">Optional metadata filters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of search results ordered by similarity</returns>
        public async Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector, int limit = 10, float scoreThreshold = 0, SearchFilter? filter = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    vector = vector,
                    limit = limit,
                    score_threshold = scoreThreshold,
                    with_payload = true,
                    filter = ConvertSearchFilter(filter)
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"/collections/{collectionName}/points/search",
                    request,
                    _jsonOptions,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Search failed: {Error}", error);
                    return new List<SearchResult>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonDocument.Parse(json);
                var results = data.RootElement.GetProperty("result");

                var searchResults = new List<SearchResult>();
                foreach (var item in results.EnumerateArray())
                {
                    searchResults.Add(new SearchResult
                    {
                        Id = item.GetProperty("id").GetString() ?? "",
                        Score = (float)item.GetProperty("score").GetDouble(),
                        Payload = ParsePayload(item.GetProperty("payload"))
                    });
                }

                _logger.LogDebug("Search returned {Count} results from {Collection}", searchResults.Count, collectionName);
                return searchResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching in {Collection}", collectionName);
                return new List<SearchResult>();
            }
        }

        /// <summary>
        /// Performs batch search with multiple query vectors
        /// </summary>
        /// <param name="collectionName">Collection to search</param>
        /// <param name="vectors">List of query vectors</param>
        /// <param name="limit">Maximum results per query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Combined search results</returns>
        public async Task<List<SearchResult>> SearchBatchAsync(string collectionName, List<float[]> vectors, int limit = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var searches = vectors.Select(v => new
                {
                    vector = v,
                    limit = limit,
                    with_payload = true
                }).ToList();

                var request = new { searches = searches };

                var response = await _httpClient.PostAsJsonAsync(
                    $"/collections/{collectionName}/points/search/batch",
                    request,
                    _jsonOptions,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<SearchResult>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonDocument.Parse(json);
                var result = data.RootElement.GetProperty("result");

                var allResults = new List<SearchResult>();
                foreach (var batch in result.EnumerateArray())
                {
                    foreach (var item in batch.EnumerateArray())
                    {
                        allResults.Add(new SearchResult
                        {
                            Id = item.GetProperty("id").GetString() ?? "",
                            Score = (float)item.GetProperty("score").GetDouble(),
                            Payload = ParsePayload(item.GetProperty("payload"))
                        });
                    }
                }

                return allResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch search for {Collection}", collectionName);
                return new List<SearchResult>();
            }
        }

        /// <summary>
        /// Searches by text (requires embedding service to be configured)
        /// </summary>
        /// <param name="collectionName">Collection to search</param>
        /// <param name="text">Query text</param>
        /// <param name="limit">Maximum results</param>
        /// <param name="scoreThreshold">Minimum similarity score</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of search results</returns>
        public Task<List<SearchResult>> SearchByTextAsync(string collectionName, string text, int limit = 10, float scoreThreshold = 0, CancellationToken cancellationToken = default)
        {
            // This requires integration with embedding service
            _logger.LogWarning("SearchByTextAsync requires embedding service integration. Use SearchAsync with pre-computed vectors instead.");
            return Task.FromResult(new List<SearchResult>());
        }

        /// <summary>
        /// Finds points similar to a given point
        /// </summary>
        /// <param name="collectionName">Collection to search</param>
        /// <param name="pointId">Reference point ID</param>
        /// <param name="limit">Maximum results</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of similar point IDs</returns>
        public async Task<List<string>> FindSimilarPointsAsync(string collectionName, string pointId, int limit = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    positive = new[] { pointId },
                    limit = limit
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"/collections/{collectionName}/points/recommend",
                    request,
                    _jsonOptions,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<string>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonDocument.Parse(json);
                var result = data.RootElement.GetProperty("result");

                var similarIds = new List<string>();
                foreach (var item in result.EnumerateArray())
                {
                    var id = item.GetProperty("id").GetString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        similarIds.Add(id);
                    }
                }

                return similarIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar points in {Collection}", collectionName);
                return new List<string>();
            }
        }

        #endregion

        #region Case-Specific Operations

        /// <summary>
        /// Indexes a document for a specific case (requires embedding service)
        /// </summary>
        /// <param name="caseId">Case identifier</param>
        /// <param name="documentId">Document identifier</param>
        /// <param name="content">Document content</param>
        /// <param name="metadata">Additional metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if document was indexed successfully</returns>
        public Task<bool> IndexCaseDocumentAsync(string caseId, string documentId, string content, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        {
            // This requires embedding service to convert content to vectors
            _logger.LogWarning("IndexCaseDocumentAsync requires embedding service integration");
            return Task.FromResult(false);
        }

        /// <summary>
        /// Searches within a specific case (requires embedding service)
        /// </summary>
        /// <param name="caseId">Case identifier</param>
        /// <param name="query">Search query text</param>
        /// <param name="limit">Maximum results</param>
        /// <param name="timeRange">Optional time range filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of search results</returns>
        public Task<List<SearchResult>> SearchCaseAsync(string caseId, string query, int limit = 10, TimeRange? timeRange = null, CancellationToken cancellationToken = default)
        {
            // This requires embedding service to convert query to vector
            _logger.LogWarning("SearchCaseAsync requires embedding service integration");
            return Task.FromResult(new List<SearchResult>());
        }

        #endregion

        #region Clustering Operations

        /// <summary>
        /// Gets clusters of similar vectors (requires additional clustering logic)
        /// </summary>
        /// <param name="collectionName">Collection to analyze</param>
        /// <param name="numClusters">Number of clusters to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of clusters</returns>
        public Task<List<Cluster>> GetClustersAsync(string collectionName, int numClusters, CancellationToken cancellationToken = default)
        {
            // Clustering would require additional implementation
            _logger.LogWarning("Clustering not yet implemented");
            return Task.FromResult(new List<Cluster>());
        }

        #endregion

        #region Service Health & Info

        /// <summary>
        /// Checks if Qdrant service is healthy
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if service is healthy</returns>
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("/health", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Pings the Qdrant service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if service responds</returns>
        public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("/", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets general information about the Qdrant service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Qdrant service information</returns>
        public async Task<QdrantInfo> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var collectionsResponse = await _httpClient.GetAsync("/collections", cancellationToken);

                if (!collectionsResponse.IsSuccessStatusCode)
                {
                    return new QdrantInfo { Status = "error" };
                }

                var json = await collectionsResponse.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonDocument.Parse(json);
                var collections = data.RootElement.GetProperty("result").GetProperty("collections");

                var collectionCount = 0;
                long totalPoints = 0;

                foreach (var collection in collections.EnumerateArray())
                {
                    collectionCount++;
                    // Would need additional calls to get point counts
                }

                return new QdrantInfo
                {
                    Status = "healthy",
                    Collections = collectionCount,
                    TotalPoints = totalPoints,
                    Version = "1.7.0", // Would need telemetry endpoint for actual version
                    StorageUsedBytes = 0 // Would need telemetry endpoint
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Qdrant info");
                return new QdrantInfo { Status = "error" };
            }
        }

        /// <summary>
        /// Gets storage information (not fully implemented in Qdrant API)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage information</returns>
        public Task<StorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default)
        {
            // This would require Qdrant telemetry API
            return Task.FromResult(new StorageInfo
            {
                TotalBytes = 0,
                UsedBytes = 0,
                AvailableBytes = 0
            });
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Converts SearchFilter to Qdrant filter format
        /// </summary>
        private object? ConvertSearchFilter(SearchFilter? filter)
        {
            if (filter == null) return null;

            var qdrantFilter = new Dictionary<string, object>();

            if (filter.Must != null && filter.Must.Any())
            {
                qdrantFilter["must"] = filter.Must.Select(kvp => new Dictionary<string, object>
                {
                    ["key"] = kvp.Key,
                    ["match"] = new { value = kvp.Value }
                }).ToList();
            }

            if (filter.Should != null && filter.Should.Any())
            {
                qdrantFilter["should"] = filter.Should.Select(kvp => new Dictionary<string, object>
                {
                    ["key"] = kvp.Key,
                    ["match"] = new { value = kvp.Value }
                }).ToList();
            }

            if (filter.MustNot != null && filter.MustNot.Any())
            {
                qdrantFilter["must_not"] = filter.MustNot.Select(kvp => new Dictionary<string, object>
                {
                    ["key"] = kvp.Key,
                    ["match"] = new { value = kvp.Value }
                }).ToList();
            }

            return qdrantFilter.Any() ? qdrantFilter : null;
        }

        /// <summary>
        /// Parses JSON vector array to float array
        /// </summary>
        private float[] ParseVector(JsonElement vectorElement)
        {
            var vector = new List<float>();
            foreach (var element in vectorElement.EnumerateArray())
            {
                vector.Add((float)element.GetDouble());
            }
            return vector.ToArray();
        }

        /// <summary>
        /// Parses JSON payload to dictionary
        /// </summary>
        private Dictionary<string, object> ParsePayload(JsonElement payload)
        {
            var result = new Dictionary<string, object>();

            foreach (var prop in payload.EnumerateObject())
            {
                result[prop.Name] = ParseJsonValue(prop.Value);
            }

            return result;
        }

        /// <summary>
        /// Recursively parses JSON values
        /// </summary>
        private object ParseJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray().Select(ParseJsonValue).ToList(),
                JsonValueKind.Object => ParsePayload(element),
                _ => null!
            };
        }

        #endregion
    }
}