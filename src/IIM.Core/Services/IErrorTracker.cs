using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Core.Services
{
    /// <summary>
    /// Tracks errors for pattern detection and alerting
    /// </summary>
    public interface IErrorTracker
    {
        void TrackError(ErrorEntry error);
        ErrorSummary GetSummary(TimeSpan window);
        List<ErrorPattern> DetectPatterns();
    }

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

    /// <summary>
    /// In-memory error tracker
    /// </summary>
    public class InMemoryErrorTracker : IErrorTracker
    {
        private readonly ConcurrentBag<ErrorEntry> _errors = new();

        public void TrackError(ErrorEntry error)
        {
            _errors.Add(error);

            // Clean old errors
            var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
            var toKeep = _errors.Where(e => e.Timestamp > cutoff).ToList();
            _errors.Clear();
            foreach (var e in toKeep)
            {
                _errors.Add(e);
            }
        }

        public ErrorSummary GetSummary(TimeSpan window)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(window);
            var windowErrors = _errors.Where(e => e.Timestamp > cutoff).ToList();

            return new ErrorSummary
            {
                TotalErrors = windowErrors.Count,
                ErrorsByType = windowErrors.GroupBy(e => e.ErrorType).ToDictionary(g => g.Key, g => g.Count()),
                ErrorsByModel = windowErrors.GroupBy(e => e.ModelId).ToDictionary(g => g.Key, g => g.Count()),
                RecentErrors = windowErrors.OrderByDescending(e => e.Timestamp).Take(10).ToList()
            };
        }

        public List<ErrorPattern> DetectPatterns()
        {
            var patterns = new List<ErrorPattern>();
            var recentErrors = _errors.Where(e => e.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-30)).ToList();

            // Check for memory issues
            var memoryErrors = recentErrors.Count(e => e.ErrorType.Contains("Memory"));
            if (memoryErrors > 5)
            {
                patterns.Add(new ErrorPattern
                {
                    Pattern = "Frequent memory errors",
                    Occurrences = memoryErrors,
                    SuggestedAction = "Consider unloading unused models or scaling resources"
                });
            }

            // Check for model-specific issues
            var modelGroups = recentErrors.GroupBy(e => e.ModelId);
            foreach (var group in modelGroups.Where(g => g.Count() > 10))
            {
                patterns.Add(new ErrorPattern
                {
                    Pattern = $"High error rate for model {group.Key}",
                    Occurrences = group.Count(),
                    SuggestedAction = $"Check model {group.Key} configuration and health"
                });
            }

            return patterns;
        }
    }
}
