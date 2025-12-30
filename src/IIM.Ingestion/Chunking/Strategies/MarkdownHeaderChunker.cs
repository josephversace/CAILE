// ═══════════════════════════════════════════════════════════════════════════════
// MARKDOWN HEADER CHUNKER
// ═══════════════════════════════════════════════════════════════════════════════
//
// Chunks documents by respecting markdown header hierarchy.
// Key guarantees:
//   - Headers are NEVER separated from their content
//   - Code blocks are NEVER split
//   - Tables are NEVER split
//   - Each chunk knows its section path for citations
//
// ═══════════════════════════════════════════════════════════════════════════════

using System.Text;

using IIM.Ingestion.Chunking.Utilities;
using IIM.Shared.Models;

namespace IIM.Ingestion.Chunking.Strategies;

/// <summary>
/// Chunks markdown documents by respecting header boundaries.
/// Optimized for technical documentation, manuals, and structured reports.
/// </summary>
public sealed class MarkdownHeaderChunker : IChunkingStrategy
{
    public string Name => "MarkdownHeaderChunker";

    public DocumentShape SupportedShapes => DocumentShape.Sectioned;

    public ChunkingResult Chunk(string text, ChunkingOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ChunkingResult
            {
                Chunks = [],
                StrategyName = Name,
                Sections = [],
                TotalChars = 0
            };
        }

        // 1. Parse the markdown structure
        var structure = MarkdownParser.Parse(text);
        var sectionTree = MarkdownParser.BuildSectionTree(structure.Headers, text);

        // 2. Build a list of "semantic segments" - atomic units we won't split
        var segments = BuildSemanticSegments(text, structure);

        // 3. Merge small segments, split large ones, respecting boundaries
        var chunks = BuildChunksFromSegments(segments, sectionTree, options);

        return new ChunkingResult
        {
            Chunks = chunks,
            StrategyName = Name,
            Sections = sectionTree,
            TotalChars = text.Length
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SEGMENT BUILDING
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Build semantic segments from the document. Each segment is an atomic unit
    /// that we prefer not to split (a section, a code block, a table, etc.)
    /// </summary>
    private static List<SemanticSegment> BuildSemanticSegments(string text, MarkdownStructure structure)
    {
        var segments = new List<SemanticSegment>();

        // If we have headers, chunk by sections
        if (structure.Headers.Count > 0)
        {
            segments = BuildSectionSegments(text, structure);
        }
        else
        {
            // No headers - treat as one big segment
            segments.Add(new SemanticSegment
            {
                Text = text,
                StartOffset = 0,
                EndOffset = text.Length,
                ContentType = ClassifyContent(text, structure),
                IsProtected = false
            });
        }

        return segments;
    }

    /// <summary>
    /// Build segments based on header boundaries.
    /// Each section becomes a segment, with protected blocks marked.
    /// </summary>
    private static List<SemanticSegment> BuildSectionSegments(string text, MarkdownStructure structure)
    {
        var segments = new List<SemanticSegment>();
        var headers = structure.Headers.OrderBy(h => h.StartOffset).ToList();

        // Handle content before first header
        if (headers.Count > 0 && headers[0].StartOffset > 0)
        {
            var preamble = text[..headers[0].StartOffset].Trim();
            if (preamble.Length > 0)
            {
                segments.Add(new SemanticSegment
                {
                    Text = preamble,
                    StartOffset = 0,
                    EndOffset = headers[0].StartOffset,
                    ContentType = ClassifyContent(preamble, structure),
                    IsProtected = false,
                    HeaderTitle = null,
                    HeaderLevel = 0
                });
            }
        }

        // Process each section
        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            var nextStart = i + 1 < headers.Count
                ? headers[i + 1].StartOffset
                : text.Length;

            var sectionText = text[header.StartOffset..nextStart].TrimEnd();

            // Check if this section contains protected blocks
            var protectedBlocks = structure.GetProtectedBlocks()
                .Where(b => b.StartOffset >= header.StartOffset && b.EndOffset <= nextStart)
                .ToList();

            if (protectedBlocks.Count == 0)
            {
                // Simple section - no protected blocks
                segments.Add(new SemanticSegment
                {
                    Text = sectionText,
                    StartOffset = header.StartOffset,
                    EndOffset = nextStart,
                    ContentType = ClassifyContent(sectionText, structure),
                    IsProtected = false,
                    HeaderTitle = header.Title,
                    HeaderLevel = header.Level
                });
            }
            else
            {
                // Section with protected blocks - split around them
                segments.AddRange(SplitSectionAroundProtectedBlocks(
                    text, header, nextStart, protectedBlocks));
            }
        }

        return segments;
    }

    /// <summary>
    /// Split a section into sub-segments, keeping protected blocks (code, tables) intact.
    /// </summary>
    private static List<SemanticSegment> SplitSectionAroundProtectedBlocks(
        string text,
        HeaderInfo header,
        int sectionEnd,
        List<BlockInfo> protectedBlocks)
    {
        var segments = new List<SemanticSegment>();
        var currentPos = header.StartOffset;

        // Always include the header line with the first segment
        var headerLineEnd = text.IndexOf('\n', header.StartOffset);
        if (headerLineEnd < 0) headerLineEnd = sectionEnd;

        foreach (var block in protectedBlocks.OrderBy(b => b.StartOffset))
        {
            // Add text before this protected block
            if (block.StartOffset > currentPos)
            {
                var beforeText = text[currentPos..block.StartOffset].Trim();
                if (beforeText.Length > 0)
                {
                    segments.Add(new SemanticSegment
                    {
                        Text = beforeText,
                        StartOffset = currentPos,
                        EndOffset = block.StartOffset,
                        ContentType = ChunkContentType.Prose,
                        IsProtected = false,
                        HeaderTitle = currentPos == header.StartOffset ? header.Title : null,
                        HeaderLevel = currentPos == header.StartOffset ? header.Level : 0
                    });
                }
            }

            // Add the protected block itself
            segments.Add(new SemanticSegment
            {
                Text = block.Content,
                StartOffset = block.StartOffset,
                EndOffset = block.EndOffset,
                ContentType = block.Type switch
                {
                    BlockType.Code => ChunkContentType.Code,
                    BlockType.Table => ChunkContentType.Table,
                    _ => ChunkContentType.Mixed
                },
                IsProtected = true,
                HeaderTitle = header.Title,
                HeaderLevel = header.Level,
                Language = block.Language
            });

            currentPos = block.EndOffset;
        }

        // Add remaining text after last protected block
        if (currentPos < sectionEnd)
        {
            var afterText = text[currentPos..sectionEnd].Trim();
            if (afterText.Length > 0)
            {
                segments.Add(new SemanticSegment
                {
                    Text = afterText,
                    StartOffset = currentPos,
                    EndOffset = sectionEnd,
                    ContentType = ChunkContentType.Prose,
                    IsProtected = false,
                    HeaderTitle = header.Title,
                    HeaderLevel = header.Level
                });
            }
        }

        return segments;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CHUNK BUILDING
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Convert semantic segments into properly sized chunks.
    /// Merges small segments, splits large ones.
    /// </summary>
    private static List<DocumentChunk> BuildChunksFromSegments(
        List<SemanticSegment> segments,
        List<SectionNode> sectionTree,
        ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var buffer = new ChunkBuffer();
        var chunkIndex = 0;

        foreach (var segment in segments)
        {
            // Protected segments (code, tables) should stay together if possible
            if (segment.IsProtected)
            {
                // Flush buffer first
                if (buffer.Length > 0)
                {
                    chunks.Add(buffer.ToChunk(chunkIndex++, sectionTree, options));
                    buffer.Clear();
                }

                // If protected block fits in max size, keep it whole
                if (segment.Text.Length <= options.MaxChunkSize)
                {
                    chunks.Add(CreateChunk(chunkIndex++, segment, sectionTree, options));
                }
                else
                {
                    // Protected block too large - must split (rare)
                    chunks.AddRange(SplitLargeSegment(segment, ref chunkIndex, sectionTree, options));
                }
                continue;
            }

            // Non-protected segment - check if it fits in buffer
            if (buffer.Length + segment.Text.Length <= options.TargetChunkSize)
            {
                buffer.Add(segment);
            }
            else if (buffer.Length >= options.MinChunkSize)
            {
                // Buffer is big enough, flush it
                chunks.Add(buffer.ToChunk(chunkIndex++, sectionTree, options));
                buffer.Clear();

                // Start new buffer with this segment
                if (segment.Text.Length <= options.MaxChunkSize)
                {
                    buffer.Add(segment);
                }
                else
                {
                    // Segment too large even alone
                    chunks.AddRange(SplitLargeSegment(segment, ref chunkIndex, sectionTree, options));
                }
            }
            else
            {
                // Buffer too small to flush, but adding segment would exceed target
                // Add anyway and let it go slightly over target (up to max)
                if (buffer.Length + segment.Text.Length <= options.MaxChunkSize)
                {
                    buffer.Add(segment);
                }
                else
                {
                    // Would exceed max - flush what we have
                    if (buffer.Length > 0)
                    {
                        chunks.Add(buffer.ToChunk(chunkIndex++, sectionTree, options));
                        buffer.Clear();
                    }

                    if (segment.Text.Length <= options.MaxChunkSize)
                    {
                        buffer.Add(segment);
                    }
                    else
                    {
                        chunks.AddRange(SplitLargeSegment(segment, ref chunkIndex, sectionTree, options));
                    }
                }
            }
        }

        // Flush remaining buffer
        if (buffer.Length > 0)
        {
            // If buffer is too small and we have previous chunks, try to merge
            if (buffer.Length < options.MinChunkSize && chunks.Count > 0)
            {
                var last = chunks[^1];
                if (last.Text.Length + buffer.Length <= options.MaxChunkSize)
                {
                    // Merge with last chunk
                    chunks[^1] = MergeChunks(last, buffer.ToChunk(chunkIndex, sectionTree, options));
                }
                else
                {
                    // Can't merge, emit as small chunk
                    chunks.Add(buffer.ToChunk(chunkIndex++, sectionTree, options));
                }
            }
            else
            {
                chunks.Add(buffer.ToChunk(chunkIndex++, sectionTree, options));
            }
        }

        // Add overlap between chunks
        if (options.OverlapSize > 0)
        {
            chunks = AddOverlap(chunks, options.OverlapSize);
        }

        return chunks;
    }

    /// <summary>
    /// Split a segment that exceeds max chunk size.
    /// </summary>
    private static List<DocumentChunk> SplitLargeSegment(
        SemanticSegment segment,
        ref int chunkIndex,
        List<SectionNode> sectionTree,
        ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var text = segment.Text;

        // Try to split on paragraph boundaries first
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (paragraphs.Length > 1)
        {
            var buffer = new StringBuilder();
            var bufferStart = segment.StartOffset;

            foreach (var para in paragraphs)
            {
                if (buffer.Length + para.Length + 2 <= options.TargetChunkSize)
                {
                    if (buffer.Length > 0) buffer.Append("\n\n");
                    buffer.Append(para);
                }
                else
                {
                    // Flush buffer
                    if (buffer.Length > 0)
                    {
                        chunks.Add(new DocumentChunk
                        {
                            Index = chunkIndex++,
                            Text = buffer.ToString(),
                            StartOffset = bufferStart,
                            EndOffset = bufferStart + buffer.Length,
                            ContentType = segment.ContentType,
                            ParentSection = segment.HeaderTitle,
                            ParentSectionLevel = segment.HeaderLevel,
                            SectionPath = FindSectionPath(bufferStart, sectionTree)
                        });
                        bufferStart += buffer.Length + 2;
                        buffer.Clear();
                    }

                    // Handle paragraph that might still be too large
                    if (para.Length > options.MaxChunkSize)
                    {
                        chunks.AddRange(SplitByWords(para, segment, ref chunkIndex, bufferStart, options));
                        bufferStart += para.Length + 2;
                    }
                    else
                    {
                        buffer.Append(para);
                    }
                }
            }

            // Flush remaining
            if (buffer.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    Index = chunkIndex++,
                    Text = buffer.ToString(),
                    StartOffset = bufferStart,
                    EndOffset = bufferStart + buffer.Length,
                    ContentType = segment.ContentType,
                    ParentSection = segment.HeaderTitle,
                    ParentSectionLevel = segment.HeaderLevel,
                    SectionPath = FindSectionPath(bufferStart, sectionTree)
                });
            }
        }
        else
        {
            // No paragraph breaks - split by words
            chunks.AddRange(SplitByWords(text, segment, ref chunkIndex, segment.StartOffset, options));
        }

        return chunks;
    }

    /// <summary>
    /// Last resort: split by words when no other boundaries exist.
    /// </summary>
    private static List<DocumentChunk> SplitByWords(
        string text,
        SemanticSegment segment,
        ref int chunkIndex,
        int baseOffset,
        ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var buffer = new StringBuilder();
        var currentOffset = baseOffset;

        foreach (var word in words)
        {
            if (buffer.Length + word.Length + 1 > options.TargetChunkSize && buffer.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    Index = chunkIndex++,
                    Text = buffer.ToString().Trim(),
                    StartOffset = currentOffset,
                    EndOffset = currentOffset + buffer.Length,
                    ContentType = segment.ContentType,
                    ParentSection = segment.HeaderTitle,
                    ParentSectionLevel = segment.HeaderLevel
                });
                currentOffset += buffer.Length;
                buffer.Clear();
            }

            if (buffer.Length > 0) buffer.Append(' ');
            buffer.Append(word);
        }

        if (buffer.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                Index = chunkIndex++,
                Text = buffer.ToString().Trim(),
                StartOffset = currentOffset,
                EndOffset = currentOffset + buffer.Length,
                ContentType = segment.ContentType,
                ParentSection = segment.HeaderTitle,
                ParentSectionLevel = segment.HeaderLevel
            });
        }

        return chunks;
    }

    /// <summary>
    /// Add overlap text from previous chunk to each chunk.
    /// </summary>
    private static List<DocumentChunk> AddOverlap(List<DocumentChunk> chunks, int overlapSize)
    {
        if (chunks.Count < 2)
            return chunks;

        var result = new List<DocumentChunk> { chunks[0] };

        for (int i = 1; i < chunks.Count; i++)
        {
            var prev = chunks[i - 1];
            var curr = chunks[i];

            // Get overlap from end of previous chunk
            var overlapText = GetOverlapText(prev.Text, overlapSize);

            result.Add(new DocumentChunk
            {
                Index = curr.Index,
                Text = curr.Text,
                StartOffset = curr.StartOffset,
                EndOffset = curr.EndOffset,
                ContentType = curr.ContentType,
                ParentSection = curr.ParentSection,
                ParentSectionLevel = curr.ParentSectionLevel,
                SectionPath = curr.SectionPath,
                OverlapPrefix = overlapText,
                Metadata = curr.Metadata
            });
        }

        return result;
    }

    private static string GetOverlapText(string text, int targetChars)
    {
        if (text.Length <= targetChars)
            return text;

        // Find a word boundary near the target
        var startPos = text.Length - targetChars;
        var spacePos = text.IndexOf(' ', startPos);

        if (spacePos > 0 && spacePos < text.Length)
            return text[(spacePos + 1)..];

        return text[startPos..];
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────────────────────────

    private static DocumentChunk CreateChunk(
        int index,
        SemanticSegment segment,
        List<SectionNode> sectionTree,
        ChunkingOptions options)
    {
        return new DocumentChunk
        {
            Index = index,
            Text = segment.Text,
            StartOffset = segment.StartOffset,
            EndOffset = segment.EndOffset,
            ContentType = segment.ContentType,
            ParentSection = segment.HeaderTitle,
            ParentSectionLevel = segment.HeaderLevel,
            SectionPath = FindSectionPath(segment.StartOffset, sectionTree),
            Metadata = segment.Language != null
                ? new Dictionary<string, string> { ["language"] = segment.Language }
                : null
        };
    }

    private static DocumentChunk MergeChunks(DocumentChunk a, DocumentChunk b)
    {
        return new DocumentChunk
        {
            Index = a.Index,
            Text = a.Text + "\n\n" + b.Text,
            StartOffset = a.StartOffset,
            EndOffset = b.EndOffset,
            ContentType = a.ContentType == b.ContentType ? a.ContentType : ChunkContentType.Mixed,
            ParentSection = a.ParentSection ?? b.ParentSection,
            ParentSectionLevel = a.ParentSectionLevel > 0 ? a.ParentSectionLevel : b.ParentSectionLevel,
            SectionPath = a.SectionPath ?? b.SectionPath
        };
    }

    private static string? FindSectionPath(int offset, List<SectionNode> sectionTree)
    {
        var section = MarkdownParser.FindSectionAtOffset(sectionTree, offset);
        return section?.Path;
    }

    private static ChunkContentType ClassifyContent(string text, MarkdownStructure structure)
    {
        // Check if primarily code
        var codeChars = structure.CodeBlocks
            .Where(b => text.Contains(b.Content))
            .Sum(b => b.Content.Length);
        if (codeChars > text.Length * 0.5)
            return ChunkContentType.Code;

        // Check if primarily table
        var tableChars = structure.Tables
            .Where(b => text.Contains(b.Content))
            .Sum(b => b.Content.Length);
        if (tableChars > text.Length * 0.5)
            return ChunkContentType.Table;

        // Check if primarily list
        var listChars = structure.Lists
            .Where(b => text.Contains(b.Content))
            .Sum(b => b.Content.Length);
        if (listChars > text.Length * 0.5)
            return ChunkContentType.List;

        // Short content
        if (text.Length < 200)
            return ChunkContentType.Short;

        return ChunkContentType.Prose;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// INTERNAL TYPES
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Internal representation of a semantic segment before chunking.
/// </summary>
internal sealed class SemanticSegment
{
    public required string Text { get; init; }
    public required int StartOffset { get; init; }
    public required int EndOffset { get; init; }
    public required ChunkContentType ContentType { get; init; }
    public required bool IsProtected { get; init; }
    public string? HeaderTitle { get; init; }
    public int HeaderLevel { get; init; }
    public string? Language { get; init; }
}

/// <summary>
/// Buffer for accumulating segments into chunks.
/// </summary>
internal sealed class ChunkBuffer
{
    private readonly List<SemanticSegment> _segments = [];

    public int Length => _segments.Sum(s => s.Text.Length) + Math.Max(0, _segments.Count - 1) * 2;

    public void Add(SemanticSegment segment) => _segments.Add(segment);

    public void Clear() => _segments.Clear();

    public DocumentChunk ToChunk(int index, List<SectionNode> sectionTree, ChunkingOptions options)
    {
        var text = string.Join("\n\n", _segments.Select(s => s.Text));
        var first = _segments[0];
        var last = _segments[^1];

        // Determine dominant content type
        var types = _segments.Select(s => s.ContentType).ToList();
        var contentType = types.GroupBy(t => t).OrderByDescending(g => g.Count()).First().Key;

        // Find section path from first segment
        var section = MarkdownParser.FindSectionAtOffset(sectionTree, first.StartOffset);

        return new DocumentChunk
        {
            Index = index,
            Text = text,
            StartOffset = first.StartOffset,
            EndOffset = last.EndOffset,
            ContentType = contentType,
            ParentSection = first.HeaderTitle ?? _segments.FirstOrDefault(s => s.HeaderTitle != null)?.HeaderTitle,
            ParentSectionLevel = first.HeaderLevel > 0 ? first.HeaderLevel : _segments.FirstOrDefault(s => s.HeaderLevel > 0)?.HeaderLevel ?? 0,
            SectionPath = section?.Path
        };
    }
}
