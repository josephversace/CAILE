// ═══════════════════════════════════════════════════════════════════════════════
// HYBRID CHUNKER
// ═══════════════════════════════════════════════════════════════════════════════
//
// Intelligent fallback chunker that analyzes content and applies the most
// appropriate strategy for each section of the document.
//
// ═══════════════════════════════════════════════════════════════════════════════

using System.Text;
using System.Text.RegularExpressions;
using IIM.Ingestion.Chunking.Utilities;
using IIM.Shared.Models;

namespace IIM.Ingestion.Chunking.Strategies;

public sealed partial class HybridChunker : IChunkingStrategy
{
    [GeneratedRegex(@"^#{1,6}\s+.+$", RegexOptions.Multiline)]
    private static partial Regex MarkdownHeaderRegex();

    [GeneratedRegex(@"^\d{4}[-/]\d{2}[-/]\d{2}|\[\d{2}:\d{2}:\d{2}\]", RegexOptions.Multiline)]
    private static partial Regex TimestampLineRegex();

    [GeneratedRegex(@"^[\s]*[-*+]\s+|\d+\.\s+", RegexOptions.Multiline)]
    private static partial Regex ListItemRegex();

    public string Name => "HybridChunker";
    public DocumentShape SupportedShapes => DocumentShape.None;

    public ChunkingResult Chunk(string text, ChunkingOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ChunkingResult { Chunks = [], StrategyName = Name, Sections = [], TotalChars = 0 };

        var regions = IdentifyRegions(text);
        var chunks = ChunkRegions(regions, text, options);
        var structure = MarkdownParser.Parse(text);
        var sections = structure.Headers.Count > 0
            ? MarkdownParser.BuildSectionTree(structure.Headers, text)
            : [];

        return new ChunkingResult
        {
            Chunks = chunks,
            StrategyName = Name,
            Sections = sections,
            TotalChars = text.Length
        };
    }

    private static List<ContentRegion> IdentifyRegions(string text)
    {
        var regions = new List<ContentRegion>();
        var structure = MarkdownParser.Parse(text);
        var anchors = new List<(int start, int end, RegionType type, string content)>();

        foreach (var code in structure.CodeBlocks)
            anchors.Add((code.StartOffset, code.EndOffset, RegionType.Code, code.Content));

        foreach (var table in structure.Tables)
            anchors.Add((table.StartOffset, table.EndOffset, RegionType.Table, table.Content));

        anchors = anchors.OrderBy(a => a.start).ToList();
        var currentPos = 0;

        foreach (var anchor in anchors)
        {
            if (anchor.start > currentPos)
            {
                var gapText = text[currentPos..anchor.start];
                regions.AddRange(AnalyzeGapRegion(gapText, currentPos));
            }

            regions.Add(new ContentRegion
            {
                Type = anchor.type,
                StartOffset = anchor.start,
                EndOffset = anchor.end,
                Text = anchor.content,
                IsProtected = true
            });
            currentPos = anchor.end;
        }

        if (currentPos < text.Length)
        {
            var remainingText = text[currentPos..];
            regions.AddRange(AnalyzeGapRegion(remainingText, currentPos));
        }

        return regions;
    }

    private static List<ContentRegion> AnalyzeGapRegion(string text, int baseOffset)
    {
        var regions = new List<ContentRegion>();
        var trimmed = text.Trim();
        if (trimmed.Length == 0) return regions;

        regions.Add(new ContentRegion
        {
            Type = DetectRegionType(trimmed),
            StartOffset = baseOffset,
            EndOffset = baseOffset + text.Length,
            Text = trimmed,
            IsProtected = false
        });
        return regions;
    }

    private static RegionType DetectRegionType(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return RegionType.Prose;

        int headerLines = 0, listLines = 0, timestampLines = 0;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (MarkdownHeaderRegex().IsMatch(trimmed)) headerLines++;
            else if (ListItemRegex().IsMatch(trimmed)) listLines++;
            else if (TimestampLineRegex().IsMatch(trimmed)) timestampLines++;
        }

        if (timestampLines > lines.Length * 0.3) return RegionType.Log;
        if (headerLines > 0 && headerLines > lines.Length * 0.1) return RegionType.Sectioned;
        if (listLines > lines.Length * 0.5) return RegionType.List;
        return RegionType.Prose;
    }

    private static List<DocumentChunk> ChunkRegions(
        List<ContentRegion> regions, string fullText, ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var chunkIndex = 0;
        var mergedRegions = MergeSmallRegions(regions, options.MinChunkSize);

        foreach (var region in mergedRegions)
        {
            var regionChunks = region.Type switch
            {
                RegionType.Code => ChunkProtectedBlock(region, ref chunkIndex, options),
                RegionType.Table => ChunkProtectedBlock(region, ref chunkIndex, options),
                RegionType.Log => ChunkSimpleRegion(region, ref chunkIndex, options, ChunkContentType.LogEntry),
                RegionType.List => ChunkSimpleRegion(region, ref chunkIndex, options, ChunkContentType.List),
                RegionType.Sectioned => ChunkSimpleRegion(region, ref chunkIndex, options, ChunkContentType.Prose),
                _ => ChunkSimpleRegion(region, ref chunkIndex, options, ChunkContentType.Prose)
            };
            chunks.AddRange(regionChunks);
        }

        return chunks;
    }

    private static List<ContentRegion> MergeSmallRegions(List<ContentRegion> regions, int minSize)
    {
        if (regions.Count < 2) return regions;

        var merged = new List<ContentRegion>();
        ContentRegion? buffer = null;

        foreach (var region in regions)
        {
            if (region.IsProtected)
            {
                if (buffer != null) { merged.Add(buffer); buffer = null; }
                merged.Add(region);
                continue;
            }

            if (buffer == null)
            {
                buffer = region;
            }
            else if (buffer.Type == region.Type && buffer.Text.Length + region.Text.Length < minSize * 3)
            {
                buffer = new ContentRegion
                {
                    Type = buffer.Type,
                    StartOffset = buffer.StartOffset,
                    EndOffset = region.EndOffset,
                    Text = buffer.Text + "\n\n" + region.Text,
                    IsProtected = false
                };
            }
            else
            {
                merged.Add(buffer);
                buffer = region;
            }
        }

        if (buffer != null) merged.Add(buffer);
        return merged;
    }

    private static List<DocumentChunk> ChunkProtectedBlock(
        ContentRegion region, ref int chunkIndex, ChunkingOptions options)
    {
        var contentType = region.Type == RegionType.Code ? ChunkContentType.Code : ChunkContentType.Table;

        if (region.Text.Length <= options.MaxChunkSize)
        {
            return [new DocumentChunk
            {
                Index = chunkIndex++,
                Text = region.Text,
                StartOffset = region.StartOffset,
                EndOffset = region.EndOffset,
                ContentType = contentType
            }];
        }

        // Split large protected blocks by lines
        return SplitByLines(region.Text, region.StartOffset, contentType, ref chunkIndex, options);
    }

    private static List<DocumentChunk> ChunkSimpleRegion(
        ContentRegion region, ref int chunkIndex, ChunkingOptions options, ChunkContentType contentType)
    {
        if (region.Text.Length <= options.TargetChunkSize)
        {
            return [new DocumentChunk
            {
                Index = chunkIndex++,
                Text = region.Text,
                StartOffset = region.StartOffset,
                EndOffset = region.EndOffset,
                ContentType = contentType
            }];
        }

        // Split by paragraphs first
        var paragraphs = region.Text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<DocumentChunk>();
        var buffer = new StringBuilder();
        var bufferStart = region.StartOffset;
        var currentOffset = region.StartOffset;

        foreach (var para in paragraphs)
        {
            var trimmed = para.Trim();
            if (trimmed.Length == 0) { currentOffset += para.Length + 2; continue; }

            if (buffer.Length + trimmed.Length + 2 <= options.TargetChunkSize)
            {
                if (buffer.Length > 0) buffer.Append("\n\n");
                buffer.Append(trimmed);
            }
            else
            {
                if (buffer.Length >= options.MinChunkSize)
                {
                    chunks.Add(new DocumentChunk
                    {
                        Index = chunkIndex++,
                        Text = buffer.ToString(),
                        StartOffset = bufferStart,
                        EndOffset = currentOffset,
                        ContentType = contentType
                    });
                    buffer.Clear();
                    bufferStart = currentOffset;
                }

                if (trimmed.Length > options.MaxChunkSize)
                {
                    if (buffer.Length > 0)
                    {
                        chunks.Add(new DocumentChunk
                        {
                            Index = chunkIndex++,
                            Text = buffer.ToString(),
                            StartOffset = bufferStart,
                            EndOffset = currentOffset,
                            ContentType = contentType
                        });
                        buffer.Clear();
                    }
                    chunks.AddRange(SplitByLines(trimmed, currentOffset, contentType, ref chunkIndex, options));
                    bufferStart = currentOffset + trimmed.Length + 2;
                }
                else
                {
                    buffer.Append(trimmed);
                }
            }
            currentOffset += para.Length + 2;
        }

        if (buffer.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                Index = chunkIndex++,
                Text = buffer.ToString(),
                StartOffset = bufferStart,
                EndOffset = region.EndOffset,
                ContentType = contentType
            });
        }

        return chunks;
    }

    private static List<DocumentChunk> SplitByLines(
        string text, int baseOffset, ChunkContentType contentType, ref int chunkIndex, ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var lines = text.Split('\n');
        var buffer = new StringBuilder();
        var currentOffset = baseOffset;

        foreach (var line in lines)
        {
            if (buffer.Length + line.Length + 1 > options.TargetChunkSize && buffer.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    Index = chunkIndex++,
                    Text = buffer.ToString().TrimEnd(),
                    StartOffset = currentOffset,
                    EndOffset = currentOffset + buffer.Length,
                    ContentType = contentType
                });
                currentOffset += buffer.Length;
                buffer.Clear();
            }
            if (buffer.Length > 0) buffer.Append('\n');
            buffer.Append(line);
        }

        if (buffer.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                Index = chunkIndex++,
                Text = buffer.ToString().TrimEnd(),
                StartOffset = currentOffset,
                EndOffset = baseOffset + text.Length,
                ContentType = contentType
            });
        }

        return chunks;
    }
}

internal sealed class ContentRegion
{
    public required RegionType Type { get; init; }
    public required int StartOffset { get; init; }
    public required int EndOffset { get; init; }
    public required string Text { get; init; }
    public required bool IsProtected { get; init; }
}

internal enum RegionType { Prose, Code, Table, List, Log, Sectioned }
