// ═══════════════════════════════════════════════════════════════════════════════
// LOG/TIMELINE CHUNKER
// ═══════════════════════════════════════════════════════════════════════════════
//
// Chunks log files and timestamped content by time windows.
// Designed for:
//   - Application logs
//   - Chat exports
//   - Event logs
//   - Timeline data
//
// Key features:
//   - Groups entries by time proximity
//   - Preserves timestamp-to-content association
//   - Handles multiple timestamp formats
//   - Maintains chronological order
//
// ═══════════════════════════════════════════════════════════════════════════════

using System.Text;
using System.Text.RegularExpressions;
using IIM.Shared.Models;

namespace IIM.Ingestion.Chunking.Strategies;

/// <summary>
/// Chunks log files and timestamped content by time windows.
/// </summary>
public sealed partial class TimeWindowChunker : IChunkingStrategy
{
    // ──────────────────────────────────────────────────────────────────────────
    // TIMESTAMP PATTERNS
    // ──────────────────────────────────────────────────────────────────────────

    // ISO 8601: 2024-01-15T14:30:00Z or 2024-01-15 14:30:00
    [GeneratedRegex(@"^\[?(\d{4}-\d{2}-\d{2}[T\s]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?)\]?")]
    private static partial Regex IsoTimestampRegex();

    // Common log format: [2024-01-15 14:30:00] or 2024/01/15 14:30:00
    [GeneratedRegex(@"^\[?(\d{4}[-/]\d{2}[-/]\d{2}\s+\d{2}:\d{2}:\d{2})\]?")]
    private static partial Regex CommonLogTimestampRegex();

    // Syslog style: Jan 15 14:30:00
    [GeneratedRegex(@"^([A-Z][a-z]{2}\s+\d{1,2}\s+\d{2}:\d{2}:\d{2})")]
    private static partial Regex SyslogTimestampRegex();

    // Time only: 14:30:00 or [14:30:00]
    [GeneratedRegex(@"^\[?(\d{2}:\d{2}:\d{2}(?:\.\d+)?)\]?")]
    private static partial Regex TimeOnlyRegex();

    // Chat timestamp: [12:34 PM] or 12:34 PM
    [GeneratedRegex(@"^\[?(\d{1,2}:\d{2}(?:\s*[AP]M)?)\]?", RegexOptions.IgnoreCase)]
    private static partial Regex ChatTimestampRegex();

    public string Name => "TimeWindowChunker";

    public DocumentShape SupportedShapes => DocumentShape.LogLike | DocumentShape.Chronological;

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

        // 1. Parse lines and extract timestamps
        var entries = ParseLogEntries(text);

        // 2. Group entries into time-based chunks
        var chunks = BuildChunksFromEntries(entries, options);

        return new ChunkingResult
        {
            Chunks = chunks,
            StrategyName = Name,
            Sections = [],
            TotalChars = text.Length
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LOG ENTRY PARSING
    // ──────────────────────────────────────────────────────────────────────────

    private static List<LogEntry> ParseLogEntries(string text)
    {
        var entries = new List<LogEntry>();
        var lines = text.Split('\n');
        var currentEntry = new StringBuilder();
        var currentOffset = 0;
        var entryStartOffset = 0;
        DateTime? currentTimestamp = null;
        string? currentTimestampStr = null;

        foreach (var line in lines)
        {
            var (timestamp, timestampStr) = TryParseTimestamp(line);

            if (timestamp.HasValue)
            {
                // New entry starts - flush previous if exists
                if (currentEntry.Length > 0)
                {
                    entries.Add(new LogEntry
                    {
                        Text = currentEntry.ToString().TrimEnd(),
                        StartOffset = entryStartOffset,
                        EndOffset = currentOffset,
                        Timestamp = currentTimestamp,
                        TimestampString = currentTimestampStr
                    });
                    currentEntry.Clear();
                }

                entryStartOffset = currentOffset;
                currentTimestamp = timestamp;
                currentTimestampStr = timestampStr;
                currentEntry.AppendLine(line);
            }
            else if (currentEntry.Length > 0)
            {
                // Continuation of current entry (multi-line log)
                currentEntry.AppendLine(line);
            }
            else
            {
                // Line without timestamp and no current entry - start new entry
                if (!string.IsNullOrWhiteSpace(line))
                {
                    entryStartOffset = currentOffset;
                    currentEntry.AppendLine(line);
                }
            }

            currentOffset += line.Length + 1; // +1 for newline
        }

        // Flush final entry
        if (currentEntry.Length > 0)
        {
            entries.Add(new LogEntry
            {
                Text = currentEntry.ToString().TrimEnd(),
                StartOffset = entryStartOffset,
                EndOffset = currentOffset,
                Timestamp = currentTimestamp,
                TimestampString = currentTimestampStr
            });
        }

        return entries;
    }

    private static (DateTime? timestamp, string? raw) TryParseTimestamp(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return (null, null);

        var trimmed = line.TrimStart();

        // Try ISO format first
        var match = IsoTimestampRegex().Match(trimmed);
        if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var isoTime))
            return (isoTime, match.Groups[1].Value);

        // Common log format
        match = CommonLogTimestampRegex().Match(trimmed);
        if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var commonTime))
            return (commonTime, match.Groups[1].Value);

        // Syslog format (assume current year)
        match = SyslogTimestampRegex().Match(trimmed);
        if (match.Success)
        {
            var withYear = $"{match.Groups[1].Value} {DateTime.Now.Year}";
            if (DateTime.TryParse(withYear, out var syslogTime))
                return (syslogTime, match.Groups[1].Value);
        }

        // Time only (assume today)
        match = TimeOnlyRegex().Match(trimmed);
        if (match.Success && TimeSpan.TryParse(match.Groups[1].Value, out var timeOnly))
            return (DateTime.Today.Add(timeOnly), match.Groups[1].Value);

        // Chat timestamp
        match = ChatTimestampRegex().Match(trimmed);
        if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var chatTime))
            return (chatTime, match.Groups[1].Value);

        return (null, null);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CHUNK BUILDING
    // ──────────────────────────────────────────────────────────────────────────

    private static List<DocumentChunk> BuildChunksFromEntries(
        List<LogEntry> entries,
        ChunkingOptions options)
    {
        if (entries.Count == 0)
            return [];

        var chunks = new List<DocumentChunk>();
        var buffer = new List<LogEntry>();
        var bufferSize = 0;
        var chunkIndex = 0;

        foreach (var entry in entries)
        {
            var entrySize = entry.Text.Length;

            // Check if adding this entry would exceed target
            if (bufferSize + entrySize > options.TargetChunkSize && buffer.Count > 0)
            {
                // Check if buffer is big enough to flush
                if (bufferSize >= options.MinChunkSize)
                {
                    chunks.Add(CreateChunkFromEntries(buffer, chunkIndex++));
                    buffer.Clear();
                    bufferSize = 0;
                }
                else if (bufferSize + entrySize > options.MaxChunkSize)
                {
                    // Would exceed max - must flush even if small
                    chunks.Add(CreateChunkFromEntries(buffer, chunkIndex++));
                    buffer.Clear();
                    bufferSize = 0;
                }
            }

            // Handle single entry that exceeds max
            if (entrySize > options.MaxChunkSize && buffer.Count == 0)
            {
                // Split the large entry
                chunks.AddRange(SplitLargeEntry(entry, ref chunkIndex, options));
                continue;
            }

            buffer.Add(entry);
            bufferSize += entrySize + 1; // +1 for separator
        }

        // Flush remaining
        if (buffer.Count > 0)
        {
            chunks.Add(CreateChunkFromEntries(buffer, chunkIndex++));
        }

        return chunks;
    }

    private static DocumentChunk CreateChunkFromEntries(List<LogEntry> entries, int index)
    {
        var text = string.Join("\n", entries.Select(e => e.Text));
        var first = entries[0];
        var last = entries[^1];

        // Create timestamp range metadata
        var metadata = new Dictionary<string, string>();

        if (first.Timestamp.HasValue)
            metadata["timestamp_start"] = first.Timestamp.Value.ToString("O");

        if (last.Timestamp.HasValue)
            metadata["timestamp_end"] = last.Timestamp.Value.ToString("O");

        metadata["entry_count"] = entries.Count.ToString();

        return new DocumentChunk
        {
            Index = index,
            Text = text,
            StartOffset = first.StartOffset,
            EndOffset = last.EndOffset,
            ContentType = ChunkContentType.LogEntry,
            Metadata = metadata
        };
    }

    private static List<DocumentChunk> SplitLargeEntry(
        LogEntry entry,
        ref int chunkIndex,
        ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var lines = entry.Text.Split('\n');
        var buffer = new StringBuilder();
        var currentOffset = entry.StartOffset;

        foreach (var line in lines)
        {
            if (buffer.Length + line.Length + 1 > options.TargetChunkSize && buffer.Length > 0)
            {
                var metadata = new Dictionary<string, string>();
                if (entry.Timestamp.HasValue)
                    metadata["timestamp"] = entry.Timestamp.Value.ToString("O");

                chunks.Add(new DocumentChunk
                {
                    Index = chunkIndex++,
                    Text = buffer.ToString().TrimEnd(),
                    StartOffset = currentOffset,
                    EndOffset = currentOffset + buffer.Length,
                    ContentType = ChunkContentType.LogEntry,
                    Metadata = metadata
                });

                currentOffset += buffer.Length;
                buffer.Clear();
            }

            if (buffer.Length > 0) buffer.Append('\n');
            buffer.Append(line);
        }

        if (buffer.Length > 0)
        {
            var metadata = new Dictionary<string, string>();
            if (entry.Timestamp.HasValue)
                metadata["timestamp"] = entry.Timestamp.Value.ToString("O");

            chunks.Add(new DocumentChunk
            {
                Index = chunkIndex++,
                Text = buffer.ToString().TrimEnd(),
                StartOffset = currentOffset,
                EndOffset = currentOffset + buffer.Length,
                ContentType = ChunkContentType.LogEntry,
                Metadata = metadata
            });
        }

        return chunks;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// INTERNAL TYPES
// ──────────────────────────────────────────────────────────────────────────────

internal sealed class LogEntry
{
    public required string Text { get; init; }
    public required int StartOffset { get; init; }
    public required int EndOffset { get; init; }
    public DateTime? Timestamp { get; init; }
    public string? TimestampString { get; init; }
}
