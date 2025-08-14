namespace IIM.Plugin.SDK;

/// <summary>
/// Result returned from plugin execution
/// </summary>
public class PluginResult
{
    /// <summary>
    /// Whether the plugin executed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The actual data/results from the plugin
    /// </summary>
    public object? Data { get; set; }
    
    /// <summary>
    /// Error message if Success is false
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Citations for any external data sources used
    /// </summary>
    public string[] Citations { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Metadata about the execution (timing, sources, etc.)
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Hash of the result for chain of custody
    /// </summary>
    public string? Hash { get; set; }
    
    /// <summary>
    /// Suggestions for follow-up actions
    /// </summary>
    public string[] SuggestedActions { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    public static PluginResult CreateSuccess(object data, params string[] citations)
    {
        return new PluginResult
        {
            Success = true,
            Data = data,
            Citations = citations
        };
    }
    
    /// <summary>
    /// Create an error result
    /// </summary>
    public static PluginResult CreateError(string error)
    {
        return new PluginResult
        {
            Success = false,
            Error = error
        };
    }
}
