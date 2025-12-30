// ═══════════════════════════════════════════════════════════════════════════════
// CHUNKING STRATEGY INTERFACE
// ═══════════════════════════════════════════════════════════════════════════════

using IIM.Ingestion.Chunking.Models;
using IIM.Shared.Models;

namespace IIM.Ingestion.Chunking;

/// <summary>
/// Interface for document chunking strategies.
/// Each strategy is optimized for a particular document shape.
/// </summary>
public interface IChunkingStrategy
{
    /// <summary>
    /// The name of this chunking strategy (for metadata/logging).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Which document shapes this strategy is designed to handle.
    /// </summary>
    DocumentShape SupportedShapes { get; }

    /// <summary>
    /// Chunk the document text into semantically meaningful pieces.
    /// </summary>
    /// <param name="text">The full document text.</param>
    /// <param name="options">Chunking configuration options.</param>
    /// <returns>Chunking result with all chunks and metadata.</returns>
    ChunkingResult Chunk(string text, ChunkingOptions options);
}

/// <summary>
/// Configuration options for chunking.
/// </summary>
public sealed class ChunkingOptions
{
    /// <summary>
    /// Target chunk size in characters. Strategies will try to create chunks near this size.
    /// Default: 1500 chars (~375 tokens)
    /// </summary>
    public int TargetChunkSize { get; init; } = 1500;

    /// <summary>
    /// Maximum chunk size in characters. Chunks will never exceed this.
    /// Default: 3000 chars (~750 tokens)
    /// </summary>
    public int MaxChunkSize { get; init; } = 3000;

    /// <summary>
    /// Minimum chunk size in characters. Very small chunks will be merged.
    /// Default: 200 chars (~50 tokens)
    /// </summary>
    public int MinChunkSize { get; init; } = 200;

    /// <summary>
    /// Number of characters to overlap between adjacent chunks for context continuity.
    /// Default: 200 chars (~50 tokens)
    /// </summary>
    public int OverlapSize { get; init; } = 200;

    /// <summary>
    /// Whether to preserve tables as atomic units (never split mid-table).
    /// Default: true
    /// </summary>
    public bool PreserveTables { get; init; } = true;

    /// <summary>
    /// Whether to preserve code blocks as atomic units (never split mid-code).
    /// Default: true
    /// </summary>
    public bool PreserveCodeBlocks { get; init; } = true;

    /// <summary>
    /// Whether to extract and store section hierarchy.
    /// Default: true
    /// </summary>
    public bool ExtractSections { get; init; } = true;

    /// <summary>
    /// The file name (for metadata).
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// The MIME type (for metadata).
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// The BLAKE3 hash of the source file (for metadata).
    /// </summary>
    public string? Blake3Hash { get; init; }

    /// <summary>
    /// Default chunking options suitable for most documents.
    /// </summary>
    public static ChunkingOptions Default => new();

    /// <summary>
    /// Options optimized for technical documentation with many code samples.
    /// </summary>
    public static ChunkingOptions TechnicalDocs => new()
    {
        TargetChunkSize = 2000,
        MaxChunkSize = 4000,
        MinChunkSize = 300,
        OverlapSize = 150,
        PreserveTables = true,
        PreserveCodeBlocks = true
    };

    /// <summary>
    /// Options optimized for chat logs and timestamped content.
    /// </summary>
    public static ChunkingOptions LogContent => new()
    {
        TargetChunkSize = 1000,
        MaxChunkSize = 2000,
        MinChunkSize = 100,
        OverlapSize = 100
    };

    /// <summary>
    /// Options optimized for large narrative documents.
    /// </summary>
    public static ChunkingOptions LargeNarrative => new()
    {
        TargetChunkSize = 2000,
        MaxChunkSize = 3500,
        MinChunkSize = 500,
        OverlapSize = 250
    };
}
