using IIM.Shared.Enums;
using System;
using System.Collections.Generic;

namespace IIM.Shared.Models;

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool Success => ExitCode == 0;
}

    public class ProcessingResult
    {
        public string ProcessingId { get; set; }
        public string EvidenceId { get; set; }
        public ProcessingStatus Status { get; set; }
        public string ProcessingType { get; set; }
        public Dictionary<string, object>? Results { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }
    }



