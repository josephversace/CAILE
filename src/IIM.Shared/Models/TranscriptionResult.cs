using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Audio transcription result with extended properties.
    /// </summary>
    /// <summary>
    /// Audio transcription result with extended properties.
    /// </summary>
    public class TranscriptionResult
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public double Confidence { get; set; }

        // New optional properties
        public TimeSpan? Duration { get; set; }  // Audio duration
        public List<TranscriptionSegment>? Segments { get; set; }  // Time-aligned segments
        public Dictionary<string, object>? Metadata { get; set; }
        public string? AudioFileId { get; set; }  // Source audio file
        public DateTimeOffset? ProcessedAt { get; set; }  // When transcribed
        public string? ModelUsed { get; set; }  // Which model was used
    }

    /// <summary>
    /// Time-aligned transcription segment.
    /// </summary>
    public class TranscriptionSegment
    {
        public int Start { get; set; }  // Start time in milliseconds
        public int End { get; set; }  // End time in milliseconds
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string? Speaker { get; set; }  // Speaker diarization
    }
}
