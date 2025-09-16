using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
    /// <summary>
    /// Contains information about a discovered plugin.
    /// </summary>
    public class PluginInfo
    {
        public string Id { get; set; } = string.Empty;
        public string PackagePath { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string EntryPoint { get; set; } = string.Empty;
        public bool IsLoaded { get; set; }
        public Dictionary<string, string> Functions { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }



    public class PluginStatus
    {
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
