using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Tool execution result
    /// </summary>
    public record ToolResult(
        string Id,
        string ToolName,
        string Status,
        object? Data,
        List<Visualization>? Visualizations = null,
        List<string>? Recommendations = null,
        DateTimeOffset ExecutedAt = default,
        TimeSpan ExecutionTime = default,
        string? ErrorMessage = null,
        Dictionary<string, object>? Metadata = null
    );

    /// <summary>
    /// Citation reference
    /// </summary>
    public record Citation(
        string Id,
        string SourceId,
        string SourceType,
        string Text,
        int? PageNumber,
        string? Location,
        double Relevance,
        string? Source = null,
        string? Url = null,
        string? Author = null,
        DateTimeOffset? PublishedAt = null
    );

    /// <summary>
    /// Visualization configuration
    /// </summary>
    public record Visualization(
        string Id,
        string Type,
        string? Title,
        string? Description,
        object Data,
        Dictionary<string, object>? Options = null,
        string? RenderFormat = null
    );

    /// <summary>
    /// Export options configuration
    /// </summary>
    public record ExportOptions(
        string Format = "pdf",
        bool IncludeMetadata = true,
        bool IncludeTimestamps = true,
        bool CompressOutput = false,
        Dictionary<string, object>? CustomOptions = null
    );

    /// <summary>
    /// Pagination request
    /// </summary>
    public record PaginationRequest(
        int Page = 1,
        int PageSize = 20,
        string? SortBy = null,
        bool SortDescending = false
    );

    /// <summary>
    /// API error response
    /// </summary>
    public record ErrorResponse(
        string ErrorCode,
        string Message,
        string? Details = null,
        Dictionary<string, object>? Context = null
    )
    {
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// API success response wrapper
    /// </summary>
    public record ApiResponse<T>(
        bool Success,
        T? Data,
        string? Message = null,
        Dictionary<string, object>? Metadata = null
    );

    /// <summary>
    /// Batch operation result
    /// </summary>
    public record BatchOperationResult(
        int TotalItems,
        int SuccessCount,
        int FailureCount,
        List<string> FailedIds,
        Dictionary<string, string> Errors,
        TimeSpan Duration
    );
}