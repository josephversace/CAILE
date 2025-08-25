using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class ErrorEntry
    {
        public string RequestId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    public class ErrorSummary
    {
        public int TotalErrors { get; set; }
        public Dictionary<string, int> ErrorsByType { get; set; } = new();
        public Dictionary<string, int> ErrorsByModel { get; set; } = new();
        public List<ErrorEntry> RecentErrors { get; set; } = new();
    }

    public class ErrorPattern
    {
        public string Pattern { get; set; } = string.Empty;
        public int Occurrences { get; set; }
        public string SuggestedAction { get; set; } = string.Empty;
    }
}
