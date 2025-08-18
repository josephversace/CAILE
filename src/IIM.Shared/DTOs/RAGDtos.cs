
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs;

// Request DTOs
public record RAGIndexRequest(
    string CaseId,
    string Content,
    string? SourceId = null,
    string? SourceType = null,
    Dictionary<string, object>? Metadata = null,
    bool UseSentenceChunking = true
);

public record RAGSearchRequest(
    string Query,
    string? CaseId = null,
    int TopK = 5,
    double MinRelevance = 0.5,
    TimeRangeDto? TimeRange = null,
    List<string>? FilterTags = null
);

public record RAGGenerateRequest(
    string Query,
    string CaseId,
    string? ModelId = null,
    int MaxTokens = 2048,
    double Temperature = 0.3,
    bool VerifyFactualAccuracy = true,
    string? SystemPrompt = null
);

// Response DTOs
public record RAGSearchResultDto(
    List<RAGDocumentDto> Documents,
    List<EntityDto> Entities,
    List<RelationshipDto> Relationships,
    KnowledgeGraphDto? KnowledgeGraph,
    QueryUnderstandingDto QueryUnderstanding,
    List<string> SuggestedFollowUps,
    Dictionary<string, object>? CaseContext
);

public record RAGDocumentDto(
    string Id,
    string Content,
    double Relevance,
    string? SourceId,
    string? SourceType,
    Dictionary<string, object>? Metadata,
    List<int>? ChunkIndices
);

public record KnowledgeGraphDto(
    List<GraphNodeDto> Nodes,
    List<GraphEdgeDto> Edges,
    Dictionary<string, object>? Properties
);

public record GraphNodeDto(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties
);

public record GraphEdgeDto(
    string Source,
    string Target,
    string Type,
    double Weight,
    Dictionary<string, object>? Properties
);

public record QueryUnderstandingDto(
    List<string> KeyTerms,
    string Intent,
    List<string> RequiredCapabilities,
    double Complexity
);

public record RAGGenerateResponseDto(
    string Answer,
    List<CitationDto> Citations,
    double Confidence,
    List<string> SourceDocumentIds,
    Dictionary<string, object>? Metadata
);
