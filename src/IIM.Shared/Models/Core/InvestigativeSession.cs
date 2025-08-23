using System;
using System.Collections.Generic;
using System.Linq;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Domain model representing an investigation session
    /// </summary>
    public class InvestigationSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = "New Investigation";
        public string Icon { get; set; } = "🔍";
        public InvestigationType Type { get; set; } = InvestigationType.GeneralInquiry;
        public InvestigationStatus Status { get; set; } = InvestigationStatus.Active;

        // Session Configuration
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, ModelConfiguration> Models { get; set; } = new();

        // Messages and Results
        public List<InvestigationMessage> Messages { get; set; } = new();
        public List<Finding> Findings { get; set; } = new();

        // Metadata
        public string CreatedBy { get; set; } = Environment.UserName;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAt { get; set; }

        // Session State
        public Dictionary<string, object> Context { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        // Business Methods

        /// <summary>
        /// Adds a message to the session
        /// </summary>
        public void AddMessage(InvestigationMessage message)
        {
            if (Status == InvestigationStatus.Completed || Status == InvestigationStatus.Archived)
                throw new InvalidOperationException("Cannot add messages to completed or archived sessions");

            message.SessionId = Id;
            Messages.Add(message);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Enables a tool for the session
        /// </summary>
        public bool EnableTool(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                return false;

            if (!EnabledTools.Contains(toolName))
            {
                EnabledTools.Add(toolName);
                UpdatedAt = DateTimeOffset.UtcNow;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Disables a tool for the session
        /// </summary>
        public bool DisableTool(string toolName)
        {
            var removed = EnabledTools.Remove(toolName);
            if (removed)
                UpdatedAt = DateTimeOffset.UtcNow;
            return removed;
        }

        /// <summary>
        /// Adds or updates a model configuration
        /// </summary>
        public void ConfigureModel(string modelId, ModelConfiguration configuration)
        {
            Models[modelId] = configuration;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Adds a finding to the session
        /// </summary>
        public void AddFinding(Finding finding)
        {
            finding.SessionId = Id;
            Findings.Add(finding);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Completes the session
        /// </summary>
        public void Complete()
        {
            if (Status == InvestigationStatus.Completed)
                return;

            Status = InvestigationStatus.Completed;
            CompletedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Archives the session
        /// </summary>
        public void Archive()
        {
            if (Status != InvestigationStatus.Completed)
                throw new InvalidOperationException("Only completed sessions can be archived");

            Status = InvestigationStatus.Archived;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the last message in the session
        /// </summary>
        public InvestigationMessage? GetLastMessage()
        {
            return Messages.LastOrDefault();
        }

        /// <summary>
        /// Gets messages by role
        /// </summary>
        public IEnumerable<InvestigationMessage> GetMessagesByRole(MessageRole role)
        {
            return Messages.Where(m => m.Role == role);
        }

        /// <summary>
        /// Calculates session duration
        /// </summary>
        public TimeSpan GetDuration()
        {
            var endTime = CompletedAt ?? DateTimeOffset.UtcNow;
            return endTime - CreatedAt;
        }

        /// <summary>
        /// Gets session statistics
        /// </summary>
        public SessionStatistics GetStatistics()
        {
            return new SessionStatistics
            {
                TotalMessages = Messages.Count,
                UserMessages = Messages.Count(m => m.Role == MessageRole.User),
                AssistantMessages = Messages.Count(m => m.Role == MessageRole.Assistant),
                ToolCalls = Messages.Count(m => m.Role == MessageRole.Tool),
                FindingsCount = Findings.Count,
                Duration = GetDuration(),
                EnabledToolsCount = EnabledTools.Count,
                ModelsUsed = Messages
                    .Where(m => !string.IsNullOrEmpty(m.ModelUsed))
                    .Select(m => m.ModelUsed!)
                    .Distinct()
                    .ToList()
            };
        }

        /// <summary>
        /// Checks if the session is active
        /// </summary>
        public bool IsActive()
        {
            return Status == InvestigationStatus.Active;
        }

        /// <summary>
        /// Updates the session context
        /// </summary>
        public void UpdateContext(string key, object value)
        {
            Context[key] = value;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Session statistics
    /// </summary>
    public class SessionStatistics
    {
        public int TotalMessages { get; set; }
        public int UserMessages { get; set; }
        public int AssistantMessages { get; set; }
        public int ToolCalls { get; set; }
        public int FindingsCount { get; set; }
        public TimeSpan Duration { get; set; }
        public int EnabledToolsCount { get; set; }
        public List<string> ModelsUsed { get; set; } = new();
    }
}