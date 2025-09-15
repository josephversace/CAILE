using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
    /// <summary>
    /// Contains information about a discovered plugin.
    /// </summary>
    public class PluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string EntryPoint { get; set; } = string.Empty;
        public bool IsLoaded { get; set; }
        public Dictionary<string, string> Functions { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a request sent to a plugin for execution.
    /// </summary>
    public class PluginRequest
    {
        public string FunctionName { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    /// <summary>
    /// Represents the result returned from a plugin execution.
    /// </summary>
    public class PluginResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PluginStatus
    {
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
