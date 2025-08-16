using System;
using System.Collections.Generic;

namespace IIM.Core.Models
{
    /// <summary>
    /// Result from audio transcription using Whisper model
    /// </summary>
    public class MockTranscriptionResult
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public float Confidence { get; set; }
        public TimeSpan Duration { get; set; }
        public Word[] Words { get; set; } = Array.Empty<Word>();
        public TimeSpan ProcessingTime { get; set; }
        public string DeviceUsed { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual word with timing information
    /// </summary>
    public class Word
    {
        public string Text { get; set; } = string.Empty;
        public float Start { get; set; }
        public float End { get; set; }
    }

    /// <summary>
    /// Results from CLIP image search
    /// </summary>
    public class ImageSearchResults
    {
        public List<ImageMatch> Matches { get; set; } = new();
        public TimeSpan QueryProcessingTime { get; set; }
        public int TotalImagesSearched { get; set; }
    }

    /// <summary>
    /// Individual image match result
    /// </summary>
    public class ImageMatch
    {
        public string ImagePath { get; set; } = string.Empty;
        public float Score { get; set; }
        public MockBoundingBox? BoundingBox { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Bounding box for detected objects
    /// </summary>
    public class MockBoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    /// <summary>
    /// Response from RAG (Retrieval Augmented Generation) query
    /// </summary>
    public class RagResponse
    {
        public string Answer { get; set; } = string.Empty;
        public Source[] Sources { get; set; } = Array.Empty<Source>();
        public float Confidence { get; set; }
        public int TokensUsed { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Document source for RAG response
    /// </summary>
    public class Source
    {
        public string Document { get; set; } = string.Empty;
        public int Page { get; set; }
        public float Relevance { get; set; }
    }

    /// <summary>
    /// GPU/Device information
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public long MemoryAvailable { get; set; }
        public long MemoryTotal { get; set; }
        public bool SupportsDirectML { get; set; }
        public bool SupportsROCm { get; set; }
    }

    public sealed class InferenceResult
    {
        public required string ModelId { get; init; }
        public required object Output { get; init; }
        public TimeSpan InferenceTime { get; init; }
        public long TokensProcessed { get; init; }
        public double TokensPerSecond { get; init; }
    }
}

