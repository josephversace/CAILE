using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Request DTO for RAG query
    /// </summary>
    public record RAGQueryRequest(
        string Query,
        int TopK = 5,
        double MinRelevance = 0.7,
        List<string>? FilterCaseIds = null,
        List<string>? FilterEvidenceTypes = null,
        Dictionary<string, object>? Parameters = null
    );

    /// <summary>
    /// RAG search result
    /// </summary>
    public record RAGSearchResult(
        string Query,
        List<RAGDocument> Documents,
        double TotalRelevance,
        TimeSpan SearchTime,
        Dictionary<string, object>? Metadata
    );

    /// <summary>
    /// RAG document result
    /// </summary>
    public record RAGDocument(
        string Id,
        string Content,
        double Relevance,
        string SourceType,
        string SourceId,
        Dictionary<string, object>? Metadata,
        List<float>? Embedding
    );

    /// <summary>
    /// Request DTO for embedding generation
    /// </summary>
    public record GenerateEmbeddingRequest(
        string Text,
        string ModelName = "default",
        Dictionary<string, object>? Options = null
    );

    /// <summary>
    /// Response DTO for embedding
    /// </summary>
    public record EmbeddingResponse(
        List<float> Embedding,
        int Dimensions,
        string ModelUsed,
        TimeSpan GenerationTime
    );

    /// <summary>
    /// Request DTO for vector store operations
    /// </summary>
    public record VectorStoreRequest(
        string Operation,
        string CollectionName,
        List<VectorDocument>? Documents = null,
        string? Query = null,
        int? TopK = null
    );

    /// <summary>
    /// Vector document for storage
    /// </summary>
    public record VectorDocument(
        string Id,
        List<float> Vector,
        Dictionary<string, object> Payload
    );

    /// <summary>
    /// RAG upload response
    /// </summary>
    public record RagUploadResponse(
        string DocumentId,
        string FileName,
        int ChunkCount,
        int VectorCount,
        TimeSpan ProcessingTime,
        string CollectionName,
        string Status
    );

    /// <summary>
    /// RAG collection info
    /// </summary>
    public record RagCollectionDto(
        string Name,
        int DocumentCount,
        int VectorCount,
        long SizeBytes,
        DateTimeOffset CreatedAt,
        DateTimeOffset LastModified
    );

    /// <summary>
    /// RAG collections response
    /// </summary>
    public record RagCollectionsResponse(
        List<RagCollectionDto> Collections,
        int TotalCount
    );

    /// <summary>
    /// RAG statistics
    /// </summary>
    public record RagStatisticsDto(
        int TotalDocuments,
        int TotalVectors,
        int TotalCollections,
        long TotalStorageBytes,
        TimeSpan AverageQueryTime,
        int QueriesProcessed,
        int DocumentsProcessedToday
    );
}