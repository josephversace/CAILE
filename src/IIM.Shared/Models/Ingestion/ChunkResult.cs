// ═══════════════════════════════════════════════════════════════════════════════
// CHUNK RESULT MODELS
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;

namespace IIM.Shared.Models;

/// <summary>
/// Result of chunking a document. Contains all chunks plus document-level metadata.
/// </summary>
public sealed class ChunkingResult
{
    /// <summary>
    /// The individual chunks produced by the chunking strategy.
    /// </summary>
    public required IReadOnlyList<DocumentChunk> Chunks { get; init; }

    /// <summary>
    /// Which chunking strategy was used.
    /// </summary>
    public required string StrategyName { get; init; }

    /// <summary>
    /// Hierarchical section structure (for sectioned documents).
    /// Empty for non-sectioned documents.
    /// </summary>
    public IReadOnlyList<SectionNode> Sections { get; init; } = [];

    /// <summary>
    /// Total characters in source document.
    /// </summary>
    public int TotalChars { get; init; }

    /// <summary>
    /// Estimated token count (chars / 4).
    /// </summary>
    public int EstimatedTokens => TotalChars / 4;
}

/// <summary>
/// A single chunk of text with rich metadata for RAG and citations.
/// </summary>
public sealed class DocumentChunk
{
    /// <summary>
    /// Zero-based index of this chunk within the document.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// The actual text content of this chunk.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Character offset where this chunk starts in the original document.
    /// </summary>
    public required int StartOffset { get; init; }

    /// <summary>
    /// Character offset where this chunk ends in the original document.
    /// </summary>
    public required int EndOffset { get; init; }

    /// <summary>
    /// What type of content this chunk primarily contains.
    /// </summary>
    public required ChunkContentType ContentType { get; init; }

    /// <summary>
    /// The section path for citation support (e.g., "Library Reference > ComputeHash > Parameters").
    /// Null for documents without clear section structure.
    /// </summary>
    public string? SectionPath { get; init; }

    /// <summary>
    /// The immediate parent section header, if any.
    /// </summary>
    public string? ParentSection { get; init; }

    /// <summary>
    /// Heading level of parent section (1-6), or 0 if no parent section.
    /// </summary>
    public int ParentSectionLevel { get; init; }

    /// <summary>
    /// Estimated token count for this chunk.
    /// </summary>
    public int EstimatedTokens => Text.Length / 4;

    /// <summary>
    /// Overlap text from the previous chunk (for context continuity).
    /// </summary>
    public string? OverlapPrefix { get; init; }

    /// <summary>
    /// Additional metadata specific to the content type.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Classification of chunk content for query-time filtering and strategy selection.
/// </summary>
public enum ChunkContentType
{
    /// <summary>
    /// Regular prose/paragraph text.
    /// </summary>
    Prose,

    /// <summary>
    /// A section or document header.
    /// </summary>
    Header,

    /// <summary>
    /// Tabular data (markdown table, CSV-like content).
    /// </summary>
    Table,

    /// <summary>
    /// Code block or code snippet.
    /// </summary>
    Code,

    /// <summary>
    /// Bullet or numbered list.
    /// </summary>
    List,

    /// <summary>
    /// Log entries or timestamped records.
    /// </summary>
    LogEntry,

    /// <summary>
    /// Mixed content that couldn't be cleanly classified.
    /// </summary>
    Mixed,

    /// <summary>
    /// Short content (e.g., captions, labels).
    /// </summary>
    Short
}

/// <summary>
/// Represents a section in the document hierarchy.
/// </summary>
public sealed class SectionNode
{
    /// <summary>
    /// Unique identifier for this section (typically the header text, slugified).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The header text as it appears in the document.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Heading level (1-6 for markdown ## headers).
    /// </summary>
    public required int Level { get; init; }

    /// <summary>
    /// Character offset where this section starts.
    /// </summary>
    public required int StartOffset { get; init; }

    /// <summary>
    /// Character offset where this section ends.
    /// </summary>
    public required int EndOffset { get; init; }

    /// <summary>
    /// Child sections (subsections).
    /// </summary>
    public List<SectionNode> Children { get; init; } = [];

    /// <summary>
    /// Full path from root (e.g., "Library Reference > ComputeHash").
    /// </summary>
    public string? Path { get; set; }
}
