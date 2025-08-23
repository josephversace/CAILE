using IIM.Core.Mediator;
using IIM.Shared.DTOs;
using System;
using System.Collections.Generic;
using IIM.Shared.Models;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Notification when an investigation query starts processing
    /// </summary>
    public class InvestigationQueryStartedNotification : INotification
    {
        public string SessionId { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when an investigation query completes
    /// </summary>
    public class InvestigationQueryCompletedNotification : INotification
    {
        public string SessionId { get; set; } = string.Empty;
        public string ResponseId { get; set; } = string.Empty;
        public long ProcessingTimeMs { get; set; }
        public List<string> ToolsUsed { get; set; } = new();
        public int CitationCount { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when an investigation query fails
    /// </summary>
    public class InvestigationQueryFailedNotification : INotification
    {
        public string SessionId { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when a session is created
    /// </summary>
    public class SessionCreatedNotification : INotification
    {
        public string SessionId { get; set; } = string.Empty;
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}