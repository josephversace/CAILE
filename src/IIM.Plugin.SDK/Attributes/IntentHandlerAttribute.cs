namespace IIM.Plugin.SDK;

/// <summary>
/// Marks a method as a handler for a specific intent
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class IntentHandlerAttribute : Attribute
{
    /// <summary>
    /// The intent this method handles
    /// </summary>
    public string Intent { get; }
    
    /// <summary>
    /// Description of what this handler does
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Example usage of this intent
    /// </summary>
    public string? Example { get; set; }
    
    /// <summary>
    /// Create a new intent handler attribute
    /// </summary>
    public IntentHandlerAttribute(string intent)
    {
        Intent = intent;
    }
}
