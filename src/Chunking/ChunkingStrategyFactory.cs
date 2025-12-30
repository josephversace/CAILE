// ═══════════════════════════════════════════════════════════════════════════════
// CHUNKING STRATEGY FACTORY
// ═══════════════════════════════════════════════════════════════════════════════
//
// Routes document shapes to appropriate chunking strategies.
// This is the main entry point for the chunking subsystem.
//
// ═══════════════════════════════════════════════════════════════════════════════

using IIM.Ingestion.Chunking.Models;
using IIM.Ingestion.Chunking.Strategies;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Chunking;

/// <summary>
/// Factory that selects and applies the appropriate chunking strategy
/// based on document shape and content characteristics.
/// </summary>
public sealed class ChunkingStrategyFactory
{
    private readonly ILogger<ChunkingStrategyFactory> _logger;

    // Strategy instances (stateless, can be reused)
    private readonly MarkdownHeaderChunker _markdownChunker = new();
    private readonly ParagraphChunker _paragraphChunker = new();
    private readonly TimeWindowChunker _timeWindowChunker = new();
    private readonly HybridChunker _hybridChunker = new();

    public ChunkingStrategyFactory(ILogger<ChunkingStrategyFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Chunk a document using the most appropriate strategy for its shape.
    /// </summary>
    /// <param name="text">The document text to chunk.</param>
    /// <param name="shapeResult">The detected document shape from DocumentShapeDetector.</param>
    /// <param name="options">Chunking options (or null for defaults).</param>
    /// <returns>Chunking result with all chunks and metadata.</returns>
    public ChunkingResult Chunk(
        string text,
        DocumentShapeResult shapeResult,
        ChunkingOptions? options = null)
    {
        options ??= SelectOptionsForShape(shapeResult);

        var strategy = SelectStrategy(shapeResult);

        _logger.LogDebug(
            "Chunking document with {Strategy} (shape={Shape}, confidence={Confidence:F2})",
            strategy.Name,
            shapeResult.Shapes,
            shapeResult.Confidence);

        var result = strategy.Chunk(text, options);

        _logger.LogInformation(
            "Chunked document into {ChunkCount} chunks using {Strategy}",
            result.Chunks.Count,
            result.StrategyName);

        return result;
    }

    /// <summary>
    /// Select the best chunking strategy for the given document shape.
    /// </summary>
    public IChunkingStrategy SelectStrategy(DocumentShapeResult shapeResult)
    {
        var shapes = shapeResult.Shapes;

        // Priority order for strategy selection:

        // 1. Log-like content gets time-window chunking
        if (shapes.HasFlag(DocumentShape.LogLike))
        {
            return _timeWindowChunker;
        }

        // 2. Sectioned documents (with headers) get markdown-aware chunking
        if (shapes.HasFlag(DocumentShape.Sectioned))
        {
            return _markdownChunker;
        }

        // 3. Chronological content (dates but not log-like) gets time-window chunking
        if (shapes.HasFlag(DocumentShape.Chronological))
        {
            return _timeWindowChunker;
        }

        // 4. Pure narrative gets paragraph chunking
        if (shapes == DocumentShape.Narrative)
        {
            return _paragraphChunker;
        }

        // 5. List-based documents - use markdown chunker if high confidence, else hybrid
        if (shapes.HasFlag(DocumentShape.ListBased))
        {
            return shapeResult.Confidence >= 0.3f ? _markdownChunker : _hybridChunker;
        }

        // 6. Low confidence or mixed - use hybrid
        if (shapeResult.Confidence < 0.2f)
        {
            return _hybridChunker;
        }

        // 7. Default fallback
        return _paragraphChunker;
    }

    /// <summary>
    /// Select appropriate chunking options based on document shape.
    /// </summary>
    public static ChunkingOptions SelectOptionsForShape(DocumentShapeResult shapeResult)
    {
        var shapes = shapeResult.Shapes;

        // Technical documentation with headers and code
        if (shapes.HasFlag(DocumentShape.Sectioned) && shapeResult.HasNumericHeaders)
        {
            return ChunkingOptions.TechnicalDocs;
        }

        // Log content
        if (shapes.HasFlag(DocumentShape.LogLike) || shapes.HasFlag(DocumentShape.Chronological))
        {
            return ChunkingOptions.LogContent;
        }

        // Long narrative
        if (shapes == DocumentShape.Narrative)
        {
            return ChunkingOptions.LargeNarrative;
        }

        return ChunkingOptions.Default;
    }

    /// <summary>
    /// Get all available strategies (for testing/diagnostics).
    /// </summary>
    public IReadOnlyList<IChunkingStrategy> GetAllStrategies()
    {
        return [_markdownChunker, _paragraphChunker, _timeWindowChunker, _hybridChunker];
    }
}
