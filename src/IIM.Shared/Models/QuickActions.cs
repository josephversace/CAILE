using System;
using System.Collections.Generic;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Represents a quick action available in the UI
    /// </summary>
    public class QuickAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Icon { get; set; } = "fa-bolt";
        public string? Category { get; set; }
        public string Command { get; set; } = string.Empty;
        public string? Prompt { get; set; }
        public string? Shortcut { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? Badge { get; set; }
    }

    /// <summary>
    /// Template for predefined investigation actions
    /// </summary>
    public class ActionTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
    }

    /// <summary>
    /// Result from selecting a quick action
    /// </summary>
    public class QuickActionResult
    {
        public string ActionId { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string? Prompt { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    /// <summary>
    /// Request for executing a message action
    /// </summary>
    public class MessageActionRequest
    {
        public string MessageId { get; set; } = string.Empty;
        public MessageActionType Action { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }
}