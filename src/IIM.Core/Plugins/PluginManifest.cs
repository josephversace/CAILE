using System.Text.Json.Serialization;

namespace IIM.Core.Plugins;

/// <summary>
/// Plugin manifest file structure (plugin.json)
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// Unique plugin identifier
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    /// <summary>
    /// Plugin display name
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// Plugin version (semver)
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }
    
    /// <summary>
    /// Author information
    /// </summary>
    [JsonPropertyName("author")]
    public required PluginAuthor Author { get; set; }
    
    /// <summary>
    /// Required IIM version
    /// </summary>
    [JsonPropertyName("iimVersion")]
    public string IimVersion { get; set; } = "^1.0.0";
    
    /// <summary>
    /// Plugin category
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "general";
    
    /// <summary>
    /// Required permissions
    /// </summary>
    [JsonPropertyName("permissions")]
    public string[] Permissions { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Plugin capabilities
    /// </summary>
    [JsonPropertyName("capabilities")]
    public PluginCapabilitiesManifest Capabilities { get; set; } = new();
    
    /// <summary>
    /// Dependencies on other packages
    /// </summary>
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; } = new();
    
    /// <summary>
    /// Digital signature information
    /// </summary>
    [JsonPropertyName("signature")]
    public PluginSignature? Signature { get; set; }
}

/// <summary>
/// Plugin author information
/// </summary>
public class PluginAuthor
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("organization")]
    public string? Organization { get; set; }
}

/// <summary>
/// Plugin capabilities in manifest
/// </summary>
public class PluginCapabilitiesManifest
{
    [JsonPropertyName("intents")]
    public string[] Intents { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("fileTypes")]
    public string[] FileTypes { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("requiresInternet")]
    public bool RequiresInternet { get; set; }
    
    [JsonPropertyName("requiresGpu")]
    public bool RequiresGpu { get; set; }
}

/// <summary>
/// Plugin signature information
/// </summary>
public class PluginSignature
{
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = "RS256";
    
    [JsonPropertyName("publicKey")]
    public string? PublicKey { get; set; }
    
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}
