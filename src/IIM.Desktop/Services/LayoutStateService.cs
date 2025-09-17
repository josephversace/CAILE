using IIM.Shared.Models.Core;

namespace IIM.Desktop.Services
{
    /// <summary>
    /// Service for managing global layout state and communication between components
    /// </summary>
    public class LayoutStateService
    {
        // Events for cross-component communication
        public event Action<string>? SearchQueryChanged;
        public event Action<VirtualFile>? FileIngested;
        public event Action<List<VirtualFile>>? FilesExported;
        public event Action<string>? WorkspaceChanged;
        public event Action<List<AIInsight>>? AIInsightsUpdated;

        // Current state
        public string CurrentSearchQuery { get; private set; } = string.Empty;
        public string CurrentWorkspaceId { get; private set; } = string.Empty;
        public List<AIInsight> CurrentAIInsights { get; private set; } = new();

        /// <summary>
        /// Update the global search query
        /// </summary>
        public void UpdateSearchQuery(string searchQuery)
        {
            if (CurrentSearchQuery != searchQuery)
            {
                CurrentSearchQuery = searchQuery;
                SearchQueryChanged?.Invoke(searchQuery);
            }
        }

        /// <summary>
        /// Update the current workspace
        /// </summary>
        public void UpdateWorkspace(string workspaceId)
        {
            if (CurrentWorkspaceId != workspaceId)
            {
                CurrentWorkspaceId = workspaceId;
                WorkspaceChanged?.Invoke(workspaceId);
            }
        }

        /// <summary>
        /// Notify that a file has been ingested
        /// </summary>
        public void NotifyFileIngested(VirtualFile file)
        {
            FileIngested?.Invoke(file);
        }

        /// <summary>
        /// Notify that files have been exported
        /// </summary>
        public void NotifyFilesExported(List<VirtualFile> files)
        {
            FilesExported?.Invoke(files);
        }

        /// <summary>
        /// Update AI insights
        /// </summary>
        public void UpdateAIInsights(List<AIInsight> insights)
        {
            CurrentAIInsights = insights;
            AIInsightsUpdated?.Invoke(insights);
        }

        /// <summary>
        /// Add a new AI insight
        /// </summary>
        public void AddAIInsight(AIInsight insight)
        {
            CurrentAIInsights.Add(insight);
            AIInsightsUpdated?.Invoke(CurrentAIInsights);
        }

        /// <summary>
        /// Clear all AI insights
        /// </summary>
        public void ClearAIInsights()
        {
            CurrentAIInsights.Clear();
            AIInsightsUpdated?.Invoke(CurrentAIInsights);
        }
    }

    /// <summary>
    /// AI insight record
    /// </summary>
    public record AIInsight(string Text, int Confidence, DateTimeOffset Timestamp)
    {
        public AIInsight(string text, int confidence) : this(text, confidence, DateTimeOffset.Now) { }
    }
}