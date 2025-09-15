using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
    /// <summary>
    /// Represents a single chat-based investigation session within a workspace.
    /// </summary>
    public class InvestigationSession
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public List<Message> Messages { get; set; } = new();
    }

    /// <summary>
    /// Represents a single message within an investigation session.
    /// </summary>
    public class Message
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string Role { get; set; } = string.Empty; // "User", "Assistant", "System"
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public List<ToolCall> ToolCalls { get; set; } = new();
        public List<Citation> Citations { get; set; } = new();
    }

    /// <summary>
    /// Represents a citation or source for information provided by the AI.
    /// </summary>
    public class Citation
    {
        public Guid Id { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public double Relevance { get; set; }
    }

    /// <summary>
    /// Represents a tool call made by the AI during a reasoning step.
    /// </summary>
    public class ToolCall
    {
        public Guid Id { get; set; }
        public string ToolName { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public bool Succeeded { get; set; }
    }
}
