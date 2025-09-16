using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
    public class AnalysisStartedEventArgs : EventArgs
    {
        public string FileName { get; set; } = string.Empty;
        public string AnalysisType { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public string? RequestId { get; set; }
        public string? UserId { get; set; }
    }

    public class AnalysisCompletedEventArgs : EventArgs
    {
        public string FileName { get; set; } = string.Empty;
        public string AnalysisType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public string? RequestId { get; set; }
        public Dictionary<string, object>? Results { get; set; }
    }

    public class AnalysisErrorEventArgs : EventArgs
    {
        public string FileName { get; set; } = string.Empty;
        public string AnalysisType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime ErrorAt { get; set; } = DateTime.UtcNow;
        public string? RequestId { get; set; }
    }
}
