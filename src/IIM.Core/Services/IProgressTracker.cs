using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.Services
{
    /// <summary>
    /// Tracks progress of inference requests
    /// </summary>
    public interface IProgressTracker
    {
        void UpdateProgress(string requestId, InferenceProgressUpdate update);
        InferenceProgressUpdate? GetProgress(string requestId);
        Dictionary<string, InferenceProgressUpdate> GetAllProgress();
        void RemoveProgress(string requestId);
    }

    public class InferenceProgressUpdate
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int PercentComplete { get; set; }
        public long? QueueTimeMs { get; set; }
        public long? ProcessingTimeMs { get; set; }
        public bool IsError { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// In-memory progress tracker implementation
    /// </summary>
    public class InMemoryProgressTracker : IProgressTracker
    {
        private readonly ConcurrentDictionary<string, InferenceProgressUpdate> _progress = new();
        private readonly Timer _cleanupTimer;

        public InMemoryProgressTracker()
        {
            // Clean up old progress entries every minute
            _cleanupTimer = new Timer(
                _ => CleanupOldEntries(),
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1));
        }

        public void UpdateProgress(string requestId, InferenceProgressUpdate update)
        {
            _progress[requestId] = update;
        }

        public InferenceProgressUpdate? GetProgress(string requestId)
        {
            return _progress.TryGetValue(requestId, out var progress) ? progress : null;
        }

        public Dictionary<string, InferenceProgressUpdate> GetAllProgress()
        {
            return new Dictionary<string, InferenceProgressUpdate>(_progress);
        }

        public void RemoveProgress(string requestId)
        {
            _progress.TryRemove(requestId, out _);
        }

        private void CleanupOldEntries()
        {
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-5);
            var toRemove = _progress
                .Where(kvp => kvp.Value.Timestamp < cutoff && kvp.Value.PercentComplete == 100)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _progress.TryRemove(key, out _);
            }
        }
    }
}