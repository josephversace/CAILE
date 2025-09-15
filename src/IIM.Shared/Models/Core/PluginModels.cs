using System;

namespace IIM.Shared.Models.Core
{
    public class PluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class PluginStatus
    {
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
