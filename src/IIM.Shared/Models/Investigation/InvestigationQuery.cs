using System;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Domain model representing a query in an investigation
    /// </summary>
    public class InvestigationQuery
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        // Query Configuration
        public List<string> EnabledTools { get; set; } = new();
        public List<Attachment> Attachments { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();

        // Timestamps
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ProcessedAt { get; set; }

        // Results
        public InvestigationResponse? Response { get; set; }

        // Business Methods

        /// <summary>
        /// Validates the query
        /// </summary>
        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Text) && !Attachments.Any())
                return false; // Query must have either text or attachments

            if (Text?.Length > 10000) // Max 10k characters
                return false;

            return true;
        }

        /// <summary>
        /// Adds context to the query
        /// </summary>
        public void AddContext(string key, object value)
        {
            Context[key] = value;
        }

        /// <summary>
        /// Adds a parameter to the query
        /// </summary>
        public void AddParameter(string key, object value)
        {
            Parameters[key] = value;
        }

        /// <summary>
        /// Enables a tool for this query
        /// </summary>
        public void EnableTool(string toolName)
        {
            if (!EnabledTools.Contains(toolName))
                EnabledTools.Add(toolName);
        }

        /// <summary>
        /// Adds an attachment to the query
        /// </summary>
        public void AddAttachment(Attachment attachment)
        {
            if (attachment == null)
                throw new ArgumentNullException(nameof(attachment));

            Attachments.Add(attachment);
        }

        /// <summary>
        /// Marks the query as processed
        /// </summary>
        public void MarkAsProcessed()
        {
            ProcessedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the query complexity score
        /// </summary>
        public int GetComplexityScore()
        {
            var score = 0;

            // Text complexity
            if (!string.IsNullOrWhiteSpace(Text))
            {
                score += Text.Length > 500 ? 2 : 1;
                score += Text.Split(' ').Length > 100 ? 1 : 0;
            }

            // Attachments add complexity
            score += Attachments.Count;

            // Each enabled tool adds complexity
            score += EnabledTools.Count;

            // Context and parameters add complexity
            score += Context.Count > 0 ? 1 : 0;
            score += Parameters.Count > 0 ? 1 : 0;

            return score;
        }

        /// <summary>
        /// Determines if this is a complex query
        /// </summary>
        public bool IsComplex()
        {
            return GetComplexityScore() > 5;
        }

        /// <summary>
        /// Creates a follow-up query based on this one
        /// </summary>
        public InvestigationQuery CreateFollowUp(string followUpText)
        {
            return new InvestigationQuery
            {
                SessionId = SessionId,
                Text = followUpText,
                EnabledTools = new List<string>(EnabledTools),
                Context = new Dictionary<string, object>(Context),
                Parameters = new Dictionary<string, object>(Parameters)
            };
        }
    }
}