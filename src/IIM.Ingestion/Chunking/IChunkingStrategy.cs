// ═══════════════════════════════════════════════════════════════════════════════
// CHUNKING STRATEGY INTERFACE
// ═══════════════════════════════════════════════════════════════════════════════


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
