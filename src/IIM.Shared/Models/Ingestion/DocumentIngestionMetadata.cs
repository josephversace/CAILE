// ═══════════════════════════════════════════════════════════════════════════════
// DOCUMENT INGESTION METADATA
// ═══════════════════════════════════════════════════════════════════════════════
//
// Enhanced metadata stored with each processed document.
// Supports:
//   - Query-time context budget decisions
//   - Section-level citations
//   - Re-ingestion tracking
//
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using IIM.Shared.Models;

namespace IIM.Shared.Models;

/// <summary>
/// Comprehensive metadata stored for each processed document.
/// Serialized to ProcessedFile.MetadataJson.
/// </summary>
public sealed class DocumentIngestionMetadata
{
    // ──────────────────────────────────────────────────────────────────────────
    // SIZE METRICS (for context budget decisions)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Total characters in the extracted text.
    /// </summary>
    [JsonPropertyName("total_chars")]
    public int TotalChars { get; init; }

    /// <summary>
    /// Estimated token count (chars / 4).
    /// </summary>
    [JsonPropertyName("estimated_tokens")]
    public int EstimatedTokens { get; init; }

    /// <summary>
    /// Number of lines in the extracted text.
    /// </summary>
    [JsonPropertyName("line_count")]
    public int LineCount { get; init; }

    /// <summary>
    /// Number of chunks created during ingestion.
    /// </summary>
    [JsonPropertyName("chunk_count")]
    public int ChunkCount { get; init; }

    // ──────────────────────────────────────────────────────────────────────────
    // CHUNKING INFO
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Which chunking strategy was used.
    /// </summary>
    [JsonPropertyName("chunking_strategy")]
    public string? ChunkingStrategy { get; init; }

    /// <summary>
    /// Chunking options that were applied.
    /// </summary>
    [JsonPropertyName("chunking_options")]
    public ChunkingOptionsSnapshot? ChunkingOptions { get; init; }

    // ──────────────────────────────────────────────────────────────────────────
    // SHAPE DETECTION
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detected document shape flags.
    /// </summary>
    [JsonPropertyName("shape_flags")]
    public string? ShapeFlags { get; init; }

    /// <summary>
    /// Confidence in shape detection (0-1).
    /// </summary>
    [JsonPropertyName("shape_confidence")]
    public float ShapeConfidence { get; init; }

    /// <summary>
    /// Whether document has numeric section headers (1.1, 1.2, etc.)
    /// </summary>
    [JsonPropertyName("has_numeric_headers")]
    public bool HasNumericHeaders { get; init; }

    /// <summary>
    /// Whether document has bullet lists.
    /// </summary>
    [JsonPropertyName("has_bullet_lists")]
    public bool HasBulletLists { get; init; }

    /// <summary>
    /// Whether document has dates.
    /// </summary>
    [JsonPropertyName("has_dates")]
    public bool HasDates { get; init; }

    /// <summary>
    /// Whether document has timestamps.
    /// </summary>
    [JsonPropertyName("has_timestamps")]
    public bool HasTimestamps { get; init; }

    /// <summary>
    /// Evidence counts from shape detection.
    /// </summary>
    [JsonPropertyName("shape_evidence")]
    public Dictionary<string, int>? ShapeEvidence { get; init; }

    // ──────────────────────────────────────────────────────────────────────────
    // SECTION STRUCTURE (for citations)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hierarchical section structure.
    /// </summary>
    [JsonPropertyName("sections")]
    public List<SectionMetadata>? Sections { get; init; }

    // ──────────────────────────────────────────────────────────────────────────
    // PREVIEW (for UI and fallback)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Text preview (first N chars for UI display).
    /// </summary>
    [JsonPropertyName("preview")]
    public string? Preview { get; init; }

    /// <summary>
    /// Maximum length of preview stored.
    /// </summary>
    public const int PreviewMaxLength = 10000;

    // ──────────────────────────────────────────────────────────────────────────
    // PROCESSING INFO
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extraction engine used.
    /// </summary>
    [JsonPropertyName("extraction_engine")]
    public string? ExtractionEngine { get; init; }

    /// <summary>
    /// Processing timestamp.
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTimeOffset ProcessedAt { get; init; }

    /// <summary>
    /// Version of the ingestion pipeline.
    /// </summary>
    [JsonPropertyName("pipeline_version")]
    public string PipelineVersion { get; init; } = "2.0";
}

/// <summary>
/// Snapshot of chunking options used (for reproducibility).
/// </summary>
public sealed class ChunkingOptionsSnapshot
{
    [JsonPropertyName("target_size")]
    public int TargetSize { get; init; }

    [JsonPropertyName("max_size")]
    public int MaxSize { get; init; }

    [JsonPropertyName("min_size")]
    public int MinSize { get; init; }

    [JsonPropertyName("overlap")]
    public int Overlap { get; init; }
}

/// <summary>
/// Section metadata for citations.
/// </summary>
public sealed class SectionMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("level")]
    public int Level { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("start")]
    public int StartOffset { get; init; }

    [JsonPropertyName("end")]
    public int EndOffset { get; init; }

    [JsonPropertyName("children")]
    public List<SectionMetadata>? Children { get; init; }
}

/// <summary>
/// Extension methods to convert between chunking types and metadata types.
/// </summary>
public static class MetadataExtensions
{
    public static ChunkingOptionsSnapshot ToSnapshot(this ChunkingOptions options)
    {
        return new ChunkingOptionsSnapshot
        {
            TargetSize = options.TargetChunkSize,
            MaxSize = options.MaxChunkSize,
            MinSize = options.MinChunkSize,
            Overlap = options.OverlapSize
        };
    }

    public static List<SectionMetadata> ToMetadata(this IReadOnlyList<SectionNode> sections)
    {
        return sections.Select(ToMetadata).ToList();
    }

    private static SectionMetadata ToMetadata(SectionNode node)
    {
        return new SectionMetadata
        {
            Id = node.Id,
            Title = node.Title,
            Level = node.Level,
            Path = node.Path,
            StartOffset = node.StartOffset,
            EndOffset = node.EndOffset,
            Children = node.Children.Count > 0 ? node.Children.Select(ToMetadata).ToList() : null
        };
    }

    public static DocumentIngestionMetadata CreateMetadata(
        string extractedText,
        DocumentShapeResult shapeResult,
        ChunkingResult chunkingResult,
        ChunkingOptions options,
        string extractionEngine)
    {
        return new DocumentIngestionMetadata
        {
            TotalChars = extractedText.Length,
            EstimatedTokens = extractedText.Length / 4,
            LineCount = extractedText.Count(c => c == '\n') + 1,
            ChunkCount = chunkingResult.Chunks.Count,

            ChunkingStrategy = chunkingResult.StrategyName,
            ChunkingOptions = options.ToSnapshot(),

            ShapeFlags = shapeResult.Shapes.ToString(),
            ShapeConfidence = shapeResult.Confidence,
            HasNumericHeaders = shapeResult.HasNumericHeaders,
            HasBulletLists = shapeResult.HasBulletLists,
            HasDates = shapeResult.HasDates,
            HasTimestamps = shapeResult.HasTimestamps,
            ShapeEvidence = shapeResult.EvidenceCounts.ToDictionary(kv => kv.Key, kv => kv.Value),

            Sections = chunkingResult.Sections.ToMetadata(),

            Preview = extractedText.Length > DocumentIngestionMetadata.PreviewMaxLength
                ? extractedText[..DocumentIngestionMetadata.PreviewMaxLength]
                : extractedText,

            ExtractionEngine = extractionEngine,
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }
}
