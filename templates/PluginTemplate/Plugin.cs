using IIM.Plugin.SDK;

namespace PluginTemplate;

/// <summary>
/// {PLUGIN_NAME} - An IIM Investigation Plugin
/// </summary>
[PluginMetadata(
    Category = "general",
    Tags = new[] { "example" },
    Author = "{AUTHOR}",
    Version = "1.0.0"
)]
public class Plugin : InvestigationPlugin
{
    /// <summary>
    /// Unique identifier for this plugin
    /// </summary>
    public override string Id => "{PLUGIN_ID}";
    
    /// <summary>
    /// Display name
    /// </summary>
    public override string Name => "{PLUGIN_NAME}";
    
    /// <summary>
    /// Plugin description
    /// </summary>
    public override string Description => 
        "Description of what your plugin does";
    
    /// <summary>
    /// Plugin capabilities
    /// </summary>
    public override PluginCapabilities Capabilities => new()
    {
        RequiresInternet = false,
        RequiresElevation = false,
        RequiredPermissions = new[] { "filesystem.read" },
        SupportedIntents = new[] { "example_intent" },
        SupportedFileTypes = new[] { "*" }
    };
    
    /// <summary>
    /// Plugin initialization
    /// </summary>
    protected override async Task OnInitializeAsync()
    {
        Logger.LogInformation("Initializing {PluginName}", Name);
        // Add any initialization logic here
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Main execution method
    /// </summary>
    public override async Task<PluginResult> ExecuteAsync(
        PluginRequest request, 
        CancellationToken ct = default)
    {
        Logger.LogInformation("Executing {PluginName} with intent: {Intent}", 
            Name, request.Intent);
        
        try
        {
            return request.Intent switch
            {
                "example_intent" => await HandleExampleIntent(request, ct),
                _ => PluginResult.CreateError($"Unknown intent: {request.Intent}")
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing plugin");
            return PluginResult.CreateError($"Plugin execution failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handle the example intent
    /// </summary>
    [IntentHandler("example_intent",
        Description = "An example intent handler",
        Example = "Do something with the example intent")]
    private async Task<PluginResult> HandleExampleIntent(
        PluginRequest request,
        CancellationToken ct)
    {
        // Extract parameters
        if (!request.Parameters.TryGetValue("input", out var input))
        {
            return PluginResult.CreateError("Missing required parameter: input");
        }
        
        // Do some work
        await Task.Delay(100, ct); // Simulate work
        
        // Return results
        var result = new
        {
            Input = input,
            ProcessedAt = DateTime.UtcNow,
            Message = $"Successfully processed: {input}"
        };
        
        return PluginResult.CreateSuccess(result);
    }
}
