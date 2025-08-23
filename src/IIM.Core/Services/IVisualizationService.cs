using IIM.Core.Models;
using IIM.Shared.Enums;


namespace IIM.Core.Services;

public interface IVisualizationService
{
    ResponseDisplayType DetermineDisplayType(VisualizationType vizType);
    ResponseDisplayType DetermineDisplayTypeFromToolResult(ToolResult result);
    VisualizationType InferVisualizationType(object data, string? hint = null);
    bool ShouldUseAdvancedDisplay(ToolResult result);
}

public class VisualizationService : IVisualizationService
{
    // Mapping between visualization types and display types
    private static readonly Dictionary<VisualizationType, ResponseDisplayType> TypeMapping = new()
    {
        { VisualizationType.Table, ResponseDisplayType.Table },
        { VisualizationType.Timeline, ResponseDisplayType.Timeline },
        { VisualizationType.Map, ResponseDisplayType.Geospatial },
        { VisualizationType.Chart, ResponseDisplayType.Structured },
        { VisualizationType.Graph, ResponseDisplayType.Structured },
        { VisualizationType.Custom, ResponseDisplayType.Structured },
        { VisualizationType.Auto, ResponseDisplayType.Auto }
    };

    public ResponseDisplayType DetermineDisplayType(VisualizationType vizType)
    {
        return TypeMapping.TryGetValue(vizType, out var displayType)
            ? displayType
            : ResponseDisplayType.Text;
    }

    public ResponseDisplayType DetermineDisplayTypeFromToolResult(ToolResult result)
    {
        // Priority 1: Use explicit preference
        if (result.PreferredDisplayType.HasValue && result.PreferredDisplayType.Value != ResponseDisplayType.Auto)
        {
            return result.PreferredDisplayType.Value;
        }

        // Priority 2: Infer from visualizations
        if (result.Visualizations?.Any() == true)
        {
            return DetermineDisplayType(result.Visualizations.First().Type);
        }

        // Priority 3: Infer from tool name
        var toolNameLower = result.ToolName.ToLowerInvariant();
        if (toolNameLower.Contains("table")) return ResponseDisplayType.Table;
        if (toolNameLower.Contains("image") || toolNameLower.Contains("photo")) return ResponseDisplayType.Image;
        if (toolNameLower.Contains("timeline")) return ResponseDisplayType.Timeline;
        if (toolNameLower.Contains("map") || toolNameLower.Contains("geo")) return ResponseDisplayType.Geospatial;

        // Priority 4: Analyze data structure
        if (result.Data != null)
        {
            var inferredVizType = InferVisualizationType(result.Data);
            return DetermineDisplayType(inferredVizType);
        }

        return ResponseDisplayType.Text;
    }

    public VisualizationType InferVisualizationType(object data, string? hint = null)
    {
        // Use hint if provided
        if (!string.IsNullOrEmpty(hint))
        {
            var hintLower = hint.ToLowerInvariant();
            if (hintLower.Contains("chart")) return VisualizationType.Chart;
            if (hintLower.Contains("table") || hintLower.Contains("grid")) return VisualizationType.Table;
            if (hintLower.Contains("timeline")) return VisualizationType.Timeline;
            if (hintLower.Contains("map")) return VisualizationType.Map;
            if (hintLower.Contains("graph") || hintLower.Contains("network")) return VisualizationType.Graph;
        }

        // Analyze data structure
        if (data is System.Text.Json.JsonElement json)
        {
            if (json.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                // Array of objects suggests table
                if (json.GetArrayLength() > 0)
                {
                    var first = json[0];
                    if (first.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        // Check for timeline indicators
                        if (first.TryGetProperty("timestamp", out _) ||
                            first.TryGetProperty("date", out _) ||
                            first.TryGetProperty("time", out _))
                        {
                            return VisualizationType.Timeline;
                        }

                        // Check for geographic indicators
                        if (first.TryGetProperty("lat", out _) ||
                            first.TryGetProperty("latitude", out _) ||
                            first.TryGetProperty("coordinates", out _))
                        {
                            return VisualizationType.Map;
                        }

                        return VisualizationType.Table;
                    }
                }
            }
        }

        return VisualizationType.Auto;
    }

    public bool ShouldUseAdvancedDisplay(ToolResult result)
    {
        // Use advanced display if we have visualizations
        if (result.Visualizations?.Any() == true)
            return true;

        // Use advanced display for non-text display types
        if (result.PreferredDisplayType.HasValue &&
            result.PreferredDisplayType.Value != ResponseDisplayType.Text)
            return true;

        // Check data complexity
        if (result.Data is System.Text.Json.JsonElement json)
        {
            if (json.ValueKind == System.Text.Json.JsonValueKind.Array && json.GetArrayLength() > 5)
                return true;
            if (json.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var propCount = json.EnumerateObject().Count();
                if (propCount > 10) return true;
            }
        }

        return false;
    }
}
