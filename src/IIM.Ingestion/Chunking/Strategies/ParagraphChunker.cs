// ═══════════════════════════════════════════════════════════════════════════════
// PARAGRAPH CHUNKER
// ═══════════════════════════════════════════════════════════════════════════════
//
// Chunks documents by paragraph boundaries with smart sentence awareness.
// Used for narrative documents without clear header structure.
//
// Key features:
//   - Respects paragraph boundaries
//   - Never splits mid-sentence (uses abbreviation-aware splitting)
//   - Preserves code blocks and tables when encountered
//   - Adds overlap for context continuity
//
// ═══════════════════════════════════════════════════════════════════════════════

using System.Text;
using System.Text.RegularExpressions;
using IIM.Ingestion.Chunking.Utilities;
using IIM.Shared.Models;

namespace IIM.Ingestion.Chunking.Strategies;

/// <summary>
/// Chunks narrative documents by paragraph boundaries.
/// Optimized for reports, articles, and unstructured text.
/// </summary>
public sealed partial class ParagraphChunker : IChunkingStrategy
{
    // Common abbreviations that don't end sentences
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "mr", "mrs", "ms", "dr", "prof", "sr", "jr",
        "vs", "etc", "inc", "ltd", "corp", "co",
        "jan", "feb", "mar", "apr", "jun", "jul", "aug", "sep", "oct", "nov", "dec",
        "st", "ave", "blvd", "rd", "apt",
        "fig", "no", "vol", "pp", "ed", "eds",
        "i.e", "e.g", "cf", "al", "approx"
    };

    [GeneratedRegex(@"^```[\s\S]*?^```", RegexOptions.Multiline)]
    private static partial Regex FencedCodeBlockRegex();

    public string Name => "ParagraphChunker";

    public DocumentShape SupportedShapes => DocumentShape.Narrative;

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

        // 1. Identify protected regions (code blocks, tables)
        var structure = MarkdownParser.Parse(text);
        var protectedRegions = structure.GetProtectedBlocks().ToList();

        // 2. Split text into paragraphs, respecting protected regions
        var paragraphs = SplitIntoParagraphs(text, protectedRegions);

        // 3. Build chunks from paragraphs
        var chunks = BuildChunksFromParagraphs(paragraphs, options);

        // 4. Add overlap
        if (options.OverlapSize > 0 && chunks.Count > 1)
        {
            chunks = AddOverlap(chunks, options.OverlapSize);
        }

        return new ChunkingResult
        {
            Chunks = chunks,
            StrategyName = Name,
            Sections = [],
            TotalChars = text.Length
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PARAGRAPH EXTRACTION
    // ──────────────────────────────────────────────────────────────────────────

    private static List<ParagraphSegment> SplitIntoParagraphs(string text, List<BlockInfo> protectedRegions)
    {
        var segments = new List<ParagraphSegment>();
        var currentPos = 0;

        // Sort protected regions by position
        var sortedProtected = protectedRegions.OrderBy(r => r.StartOffset).ToList();
        var protectedIndex = 0;

        while (currentPos < text.Length)
        {
            // Check if we're entering a protected region
            if (protectedIndex < sortedProtected.Count &&
                currentPos >= sortedProtected[protectedIndex].StartOffset)
            {
                var protected_ = sortedProtected[protectedIndex];

                // Add the protected block as a single segment
                segments.Add(new ParagraphSegment
                {
                    Text = protected_.Content,
                    StartOffset = protected_.StartOffset,
                    EndOffset = protected_.EndOffset,
                    IsProtected = true,
                    ContentType = protected_.Type switch
                    {
                        BlockType.Code => ChunkContentType.Code,
                        BlockType.Table => ChunkContentType.Table,
                        _ => ChunkContentType.Mixed
                    }
                });

                currentPos = protected_.EndOffset;
                protectedIndex++;
                continue;
            }

            // Find the next protected region (or end of text)
            var nextProtectedStart = protectedIndex < sortedProtected.Count
                ? sortedProtected[protectedIndex].StartOffset
                : text.Length;

            // Extract paragraphs between current position and next protected region
            var regionText = text[currentPos..nextProtectedStart];
            var regionParagraphs = ExtractParagraphsFromRegion(regionText, currentPos);
            segments.AddRange(regionParagraphs);

            currentPos = nextProtectedStart;
        }

        return segments;
    }

    private static List<ParagraphSegment> ExtractParagraphsFromRegion(string text, int baseOffset)
    {
        var segments = new List<ParagraphSegment>();

        // Split on double newlines (paragraph boundaries)
        var parts = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.None);
        var currentOffset = baseOffset;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                // Detect content type
                var contentType = DetectContentType(trimmed);

                segments.Add(new ParagraphSegment
                {
                    Text = trimmed,
                    StartOffset = currentOffset,
                    EndOffset = currentOffset + part.Length,
                    IsProtected = false,
                    ContentType = contentType
                });
            }

            currentOffset += part.Length + 2; // +2 for the \n\n separator
        }

        return segments;
    }

    private static ChunkContentType DetectContentType(string text)
    {
        var trimmed = text.TrimStart();

        // List detection
        if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ ") ||
            (trimmed.Length > 2 && char.IsDigit(trimmed[0]) && trimmed[1] == '.'))
        {
            return ChunkContentType.List;
        }

        // Short content
        if (text.Length < 100)
        {
            return ChunkContentType.Short;
        }

        return ChunkContentType.Prose;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CHUNK BUILDING
    // ──────────────────────────────────────────────────────────────────────────

    private static List<DocumentChunk> BuildChunksFromParagraphs(
        List<ParagraphSegment> paragraphs,
        ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var buffer = new StringBuilder();
        var bufferStart = 0;
        var bufferEnd = 0;
        var bufferContentType = ChunkContentType.Prose;
        var chunkIndex = 0;

        foreach (var para in paragraphs)
        {
            // Protected blocks get their own chunk
            if (para.IsProtected)
            {
                // Flush buffer first
                if (buffer.Length > 0)
                {
                    chunks.Add(new DocumentChunk
                    {
                        Index = chunkIndex++,
                        Text = buffer.ToString(),
                        StartOffset = bufferStart,
                        EndOffset = bufferEnd,
                        ContentType = bufferContentType
                    });
                    buffer.Clear();
                }

                // Add protected block as its own chunk (or split if too large)
                if (para.Text.Length <= options.MaxChunkSize)
                {
                    chunks.Add(new DocumentChunk
                    {
                        Index = chunkIndex++,
                        Text = para.Text,
                        StartOffset = para.StartOffset,
                        EndOffset = para.EndOffset,
                        ContentType = para.ContentType
                    });
                }
                else
                {
                    // Split large protected block (shouldn't happen often)
                    chunks.AddRange(SplitLargeBlock(para, ref chunkIndex, options));
                }
                continue;
            }

            // Check if adding this paragraph would exceed target
            var newLength = buffer.Length > 0
                ? buffer.Length + 2 + para.Text.Length  // +2 for \n\n separator
                : para.Text.Length;

            if (newLength <= options.TargetChunkSize)
            {
                // Add to buffer
                if (buffer.Length > 0)
                {
                    buffer.Append("\n\n");
                }
                else
                {
                    bufferStart = para.StartOffset;
                    bufferContentType = para.ContentType;
                }
                buffer.Append(para.Text);
                bufferEnd = para.EndOffset;
            }
            else if (buffer.Length >= options.MinChunkSize)
            {
                // Flush buffer
                chunks.Add(new DocumentChunk
                {
                    Index = chunkIndex++,
                    Text = buffer.ToString(),
                    StartOffset = bufferStart,
                    EndOffset = bufferEnd,
                    ContentType = bufferContentType
                });
                buffer.Clear();

                // Start new buffer with current paragraph
                if (para.Text.Length <= options.MaxChunkSize)
                {
                    buffer.Append(para.Text);
                    bufferStart = para.StartOffset;
                    bufferEnd = para.EndOffset;
                    bufferContentType = para.ContentType;
                }
                else
                {
                    // Paragraph itself is too large - split it
                    chunks.AddRange(SplitLargeParagraph(para, ref chunkIndex, options));
                }
            }
            else
            {
                // Buffer too small but adding would exceed target
                // Try to add anyway up to max
                if (newLength <= options.MaxChunkSize)
                {
                    if (buffer.Length > 0) buffer.Append("\n\n");
                    else bufferStart = para.StartOffset;
                    buffer.Append(para.Text);
                    bufferEnd = para.EndOffset;
                }
                else
                {
                    // Must flush and handle separately
                    if (buffer.Length > 0)
                    {
                        chunks.Add(new DocumentChunk
                        {
                            Index = chunkIndex++,
                            Text = buffer.ToString(),
                            StartOffset = bufferStart,
                            EndOffset = bufferEnd,
                            ContentType = bufferContentType
                        });
                        buffer.Clear();
                    }

                    if (para.Text.Length <= options.MaxChunkSize)
                    {
                        buffer.Append(para.Text);
                        bufferStart = para.StartOffset;
                        bufferEnd = para.EndOffset;
                        bufferContentType = para.ContentType;
                    }
                    else
                    {
                        chunks.AddRange(SplitLargeParagraph(para, ref chunkIndex, options));
                    }
                }
            }
        }

        // Flush remaining buffer
        if (buffer.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                Index = chunkIndex++,
                Text = buffer.ToString(),
                StartOffset = bufferStart,
                EndOffset = bufferEnd,
                ContentType = bufferContentType
            });
        }

        return chunks;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SPLITTING LARGE CONTENT
    // ──────────────────────────────────────────────────────────────────────────

    private static List<DocumentChunk> SplitLargeParagraph(
        ParagraphSegment para,
        ref int chunkIndex,
        ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var sentences = SplitIntoSentences(para.Text);

        var buffer = new StringBuilder();
        var currentOffset = para.StartOffset;

        foreach (var sentence in sentences)
        {
            if (buffer.Length + sentence.Length + 1 <= options.TargetChunkSize)
            {
                if (buffer.Length > 0) buffer.Append(' ');
                buffer.Append(sentence);
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
                        StartOffset = currentOffset,
                        EndOffset = currentOffset + buffer.Length,
                        ContentType = para.ContentType
                    });
                    currentOffset += buffer.Length + 1;
                    buffer.Clear();
                }

                // Handle sentence that might be too large
                if (sentence.Length > options.MaxChunkSize)
                {
                    // Split by words as last resort
                    chunks.AddRange(SplitByWords(sentence, para.ContentType, ref chunkIndex, currentOffset, options));
                    currentOffset += sentence.Length + 1;
                }
                else
                {
                    buffer.Append(sentence);
                }
            }
        }

        if (buffer.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                Index = chunkIndex++,
                Text = buffer.ToString(),
                StartOffset = currentOffset,
                EndOffset = currentOffset + buffer.Length,
                ContentType = para.ContentType
            });
        }

        return chunks;
    }

    private static List<DocumentChunk> SplitLargeBlock(
        ParagraphSegment block,
        ref int chunkIndex,
        ChunkingOptions options)
    {
        // For code/tables that are too large, split on line boundaries
        var chunks = new List<DocumentChunk>();
        var lines = block.Text.Split('\n');
        var buffer = new StringBuilder();
        var currentOffset = block.StartOffset;

        foreach (var line in lines)
        {
            if (buffer.Length + line.Length + 1 <= options.TargetChunkSize)
            {
                if (buffer.Length > 0) buffer.Append('\n');
                buffer.Append(line);
            }
            else
            {
                if (buffer.Length > 0)
                {
                    chunks.Add(new DocumentChunk
                    {
                        Index = chunkIndex++,
                        Text = buffer.ToString(),
                        StartOffset = currentOffset,
                        EndOffset = currentOffset + buffer.Length,
                        ContentType = block.ContentType
                    });
                    currentOffset += buffer.Length + 1;
                    buffer.Clear();
                }
                buffer.Append(line);
            }
        }

        if (buffer.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                Index = chunkIndex++,
                Text = buffer.ToString(),
                StartOffset = currentOffset,
                EndOffset = currentOffset + buffer.Length,
                ContentType = block.ContentType
            });
        }

        return chunks;
    }

    private static List<DocumentChunk> SplitByWords(
        string text,
        ChunkContentType contentType,
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
                    ContentType = contentType
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
                ContentType = contentType
            });
        }

        return chunks;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SENTENCE SPLITTING
    // ──────────────────────────────────────────────────────────────────────────

    private static List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var current = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            current.Append(c);

            if (c == '.' || c == '!' || c == '?')
            {
                // Check if this is actually end of sentence
                var wordBefore = GetWordBefore(text, i);

                // Not end if it's an abbreviation
                if (Abbreviations.Contains(wordBefore.TrimEnd('.')))
                    continue;

                // Not end if followed by lowercase (likely decimal or abbreviation)
                if (i + 2 < text.Length && char.IsLower(text[i + 2]))
                    continue;

                // Likely end of sentence
                var sentence = current.ToString().Trim();
                if (sentence.Length > 0)
                {
                    sentences.Add(sentence);
                }
                current.Clear();
            }
        }

        // Don't forget remaining text
        var remaining = current.ToString().Trim();
        if (remaining.Length > 0)
        {
            sentences.Add(remaining);
        }

        return sentences;
    }

    private static string GetWordBefore(string text, int position)
    {
        var end = position;
        var start = position;

        while (start > 0 && text[start - 1] != ' ' && text[start - 1] != '\n')
        {
            start--;
        }

        return text[start..(end + 1)];
    }

    // ──────────────────────────────────────────────────────────────────────────
    // OVERLAP
    // ──────────────────────────────────────────────────────────────────────────

    private static List<DocumentChunk> AddOverlap(List<DocumentChunk> chunks, int overlapSize)
    {
        var result = new List<DocumentChunk> { chunks[0] };

        for (int i = 1; i < chunks.Count; i++)
        {
            var prev = chunks[i - 1];
            var curr = chunks[i];

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

        var startPos = text.Length - targetChars;
        var spacePos = text.IndexOf(' ', startPos);

        if (spacePos > 0 && spacePos < text.Length)
            return text[(spacePos + 1)..];

        return text[startPos..];
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// INTERNAL TYPES
// ──────────────────────────────────────────────────────────────────────────────

internal sealed class ParagraphSegment
{
    public required string Text { get; init; }
    public required int StartOffset { get; init; }
    public required int EndOffset { get; init; }
    public required bool IsProtected { get; init; }
    public required ChunkContentType ContentType { get; init; }
}
