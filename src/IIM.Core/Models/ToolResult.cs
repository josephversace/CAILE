using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Core.Models;


public class ToolResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ToolName { get; set; } = string.Empty;
    public ToolStatus Status { get; set; }
    public object? Data { get; set; }
    public List<Visualization> Visualizations { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan ExecutionTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ToolExecution
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public ToolResult? Result { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
}

public class Visualization
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = string.Empty; // chart, graph, timeline, map, table, etc.
    public string? Title { get; set; }
    public string? Description { get; set; }
    public object Data { get; set; } = new { };
    public Dictionary<string, object> Options { get; set; } = new();
    public string? RenderFormat { get; set; } // html, svg, canvas, etc.
}

public enum ToolStatus
{
    Pending,
    Running,
    Success,
    PartialSuccess,
    Failed,
    Cancelled
}