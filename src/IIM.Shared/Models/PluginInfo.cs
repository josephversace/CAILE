using System.Collections.Generic;

namespace IIM.Shared.Models;


public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? PackagePath { get; set; }
    public bool IsLoaded { get; set; }

    public List<string> Functions { get; set; } = new();

    public Dictionary<string, object> Metadata { get; set; } = new();
}
