// ═══════════════════════════════════════════════════════════════════════════════
// MARKDOWN STRUCTURE PARSER
// ═══════════════════════════════════════════════════════════════════════════════

using System.Text.RegularExpressions;
using IIM.Ingestion.Chunking.Models;

namespace IIM.Ingestion.Chunking.Utilities;

/// <summary>
/// Parses markdown text to extract structural elements like headers, code blocks,
/// tables, and lists. Does not modify the text, only identifies boundaries.
/// </summary>
public static partial class MarkdownParser
{
    // ──────────────────────────────────────────────────────────────────────────
    // COMPILED REGEX PATTERNS
    // ──────────────────────────────────────────────────────────────────────────

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^```[\s\S]*?^```", RegexOptions.Multiline)]
    private static partial Regex FencedCodeBlockRegex();

    [GeneratedRegex(@"^(\|.+\|[\r\n]+)+", RegexOptions.Multiline)]
    private static partial Regex TableRegex();

    [GeneratedRegex(@"^(?:[-*+]|\d+\.)\s+.+(?:\n(?:[-*+]|\d+\.)\s+.+)*", RegexOptions.Multiline)]
    private static partial Regex ListRegex();

    [GeneratedRegex(@"^\[[\d\-:T.Z]+\]|\b\d{4}-\d{2}-\d{2}[T\s]\d{2}:\d{2}:\d{2}", RegexOptions.Multiline)]
    private static partial Regex LogLineRegex();

    // ──────────────────────────────────────────────────────────────────────────
    // STRUCTURE EXTRACTION
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extract all structural elements from markdown text.
    /// </summary>
    public static MarkdownStructure Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new MarkdownStructure
            {
                Headers = [],
                CodeBlocks = [],
                Tables = [],
                Lists = [],
                TotalLength = 0
            };
        }

        return new MarkdownStructure
        {
            Headers = ExtractHeaders(text),
            CodeBlocks = ExtractCodeBlocks(text),
            Tables = ExtractTables(text),
            Lists = ExtractLists(text),
            TotalLength = text.Length
        };
    }

    /// <summary>
    /// Extract headers with their levels and positions.
    /// </summary>
    public static List<HeaderInfo> ExtractHeaders(string text)
    {
        var headers = new List<HeaderInfo>();
        var matches = HeaderRegex().Matches(text);

        foreach (Match match in matches)
        {
            var level = match.Groups[1].Value.Length;
            var title = match.Groups[2].Value.Trim();

            headers.Add(new HeaderInfo
            {
                Level = level,
                Title = title,
                StartOffset = match.Index,
                EndOffset = match.Index + match.Length,
                Id = Slugify(title)
            });
        }

        return headers;
    }

    /// <summary>
    /// Extract fenced code blocks (``` ... ```).
    /// </summary>
    public static List<BlockInfo> ExtractCodeBlocks(string text)
    {
        var blocks = new List<BlockInfo>();
        var matches = FencedCodeBlockRegex().Matches(text);

        foreach (Match match in matches)
        {
            // Extract language hint if present
            var firstLine = match.Value.Split('\n')[0];
            var language = firstLine.Length > 3 ? firstLine[3..].Trim() : null;

            blocks.Add(new BlockInfo
            {
                Type = BlockType.Code,
                StartOffset = match.Index,
                EndOffset = match.Index + match.Length,
                Content = match.Value,
                Language = language
            });
        }

        return blocks;
    }

    /// <summary>
    /// Extract markdown tables.
    /// </summary>
    public static List<BlockInfo> ExtractTables(string text)
    {
        var tables = new List<BlockInfo>();

        // More robust table detection: find lines starting with |
        var lines = text.Split('\n');
        int tableStart = -1;
        int currentPos = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimStart();
            var isTableLine = line.StartsWith('|') && line.Contains('|', 1);

            if (isTableLine && tableStart < 0)
            {
                // Start of a table
                tableStart = currentPos;
            }
            else if (!isTableLine && tableStart >= 0)
            {
                // End of a table
                tables.Add(new BlockInfo
                {
                    Type = BlockType.Table,
                    StartOffset = tableStart,
                    EndOffset = currentPos,
                    Content = text[tableStart..currentPos].TrimEnd()
                });
                tableStart = -1;
            }

            currentPos += lines[i].Length + 1; // +1 for newline
        }

        // Handle table at end of document
        if (tableStart >= 0)
        {
            tables.Add(new BlockInfo
            {
                Type = BlockType.Table,
                StartOffset = tableStart,
                EndOffset = text.Length,
                Content = text[tableStart..].TrimEnd()
            });
        }

        return tables;
    }

    /// <summary>
    /// Extract bullet/numbered lists.
    /// </summary>
    public static List<BlockInfo> ExtractLists(string text)
    {
        var lists = new List<BlockInfo>();
        var lines = text.Split('\n');
        int listStart = -1;
        int currentPos = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            var isListItem = IsListItem(trimmed);

            if (isListItem && listStart < 0)
            {
                listStart = currentPos;
            }
            else if (!isListItem && !string.IsNullOrWhiteSpace(lines[i]) && listStart >= 0)
            {
                // Non-empty, non-list line ends the list
                lists.Add(new BlockInfo
                {
                    Type = BlockType.List,
                    StartOffset = listStart,
                    EndOffset = currentPos,
                    Content = text[listStart..currentPos].TrimEnd()
                });
                listStart = -1;
            }

            currentPos += lines[i].Length + 1;
        }

        // Handle list at end of document
        if (listStart >= 0)
        {
            lists.Add(new BlockInfo
            {
                Type = BlockType.List,
                StartOffset = listStart,
                EndOffset = text.Length,
                Content = text[listStart..].TrimEnd()
            });
        }

        return lists;
    }

    /// <summary>
    /// Build a hierarchical section tree from headers.
    /// </summary>
    public static List<SectionNode> BuildSectionTree(List<HeaderInfo> headers, string text)
    {
        if (headers.Count == 0)
            return [];

        var root = new List<SectionNode>();
        var stack = new Stack<SectionNode>();

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            var nextHeader = i + 1 < headers.Count ? headers[i + 1] : null;

            var node = new SectionNode
            {
                Id = header.Id,
                Title = header.Title,
                Level = header.Level,
                StartOffset = header.StartOffset,
                EndOffset = nextHeader?.StartOffset ?? text.Length
            };

            // Pop stack until we find a parent with lower level
            while (stack.Count > 0 && stack.Peek().Level >= header.Level)
            {
                stack.Pop();
            }

            if (stack.Count == 0)
            {
                // Top-level section
                root.Add(node);
                node.Path = node.Title;
            }
            else
            {
                // Child of current stack top
                var parent = stack.Peek();
                parent.Children.Add(node);
                node.Path = $"{parent.Path} > {node.Title}";
            }

            stack.Push(node);
        }

        return root;
    }

    /// <summary>
    /// Find which section a character offset belongs to.
    /// </summary>
    public static SectionNode? FindSectionAtOffset(List<SectionNode> sections, int offset)
    {
        foreach (var section in sections)
        {
            if (offset >= section.StartOffset && offset < section.EndOffset)
            {
                // Check children for more specific match
                var child = FindSectionAtOffset(section.Children, offset);
                return child ?? section;
            }
        }
        return null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────────────────────────

    private static bool IsListItem(string line)
    {
        if (string.IsNullOrEmpty(line))
            return false;

        // Bullet lists: - * +
        if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ "))
            return true;

        // Numbered lists: 1. 2. etc.
        if (line.Length >= 3 && char.IsDigit(line[0]))
        {
            var dotIndex = line.IndexOf('.');
            if (dotIndex > 0 && dotIndex < 4 && dotIndex + 1 < line.Length && line[dotIndex + 1] == ' ')
            {
                return line[..dotIndex].All(char.IsDigit);
            }
        }

        return false;
    }

    private static string Slugify(string text)
    {
        // Convert to lowercase, replace spaces with hyphens, remove special chars
        var slug = text.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("&amp;", "and")
            .Replace("&", "and");

        // Remove non-alphanumeric except hyphens
        return new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// SUPPORTING TYPES
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Complete structural analysis of a markdown document.
/// </summary>
public sealed class MarkdownStructure
{
    public required List<HeaderInfo> Headers { get; init; }
    public required List<BlockInfo> CodeBlocks { get; init; }
    public required List<BlockInfo> Tables { get; init; }
    public required List<BlockInfo> Lists { get; init; }
    public required int TotalLength { get; init; }

    /// <summary>
    /// Get all protected blocks (code, tables) that should not be split.
    /// </summary>
    public IEnumerable<BlockInfo> GetProtectedBlocks()
    {
        return CodeBlocks.Concat(Tables).OrderBy(b => b.StartOffset);
    }

    /// <summary>
    /// Check if an offset falls within a protected block.
    /// </summary>
    public bool IsInProtectedBlock(int offset)
    {
        return CodeBlocks.Any(b => offset >= b.StartOffset && offset < b.EndOffset)
            || Tables.Any(b => offset >= b.StartOffset && offset < b.EndOffset);
    }
}

/// <summary>
/// Information about a header in the document.
/// </summary>
public sealed class HeaderInfo
{
    public required int Level { get; init; }
    public required string Title { get; init; }
    public required int StartOffset { get; init; }
    public required int EndOffset { get; init; }
    public required string Id { get; init; }
}

/// <summary>
/// Information about a structural block (code, table, list).
/// </summary>
public sealed class BlockInfo
{
    public required BlockType Type { get; init; }
    public required int StartOffset { get; init; }
    public required int EndOffset { get; init; }
    public required string Content { get; init; }
    public string? Language { get; init; } // For code blocks

    public int Length => EndOffset - StartOffset;
}

public enum BlockType
{
    Code,
    Table,
    List
}
