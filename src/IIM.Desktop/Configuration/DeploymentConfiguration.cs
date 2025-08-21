using IIM.Shared.Enums;

/// <summary>
/// Configuration class for deployment mode settings.
/// Determines how the desktop client connects to the API.
/// </summary>
public class DeploymentConfiguration
{
    /// <summary>
    /// Gets or sets the deployment mode.
    /// Determines whether to use local or remote API.
    /// </summary>
    public DeploymentMode Mode { get; set; } = DeploymentMode.Standalone;

    /// <summary>
    /// Gets or sets the API server URL.
    /// Used when Mode is Client to connect to remote API.
    /// </summary>
    public string ApiUrl { get; set; } = "http://localhost:5080";

    /// <summary>
    /// Gets or sets whether authentication is required.
    /// When true, user must login before using the application.
    /// </summary>
    public bool RequireAuth { get; set; } = false;

    /// <summary>
    /// Gets or sets whether initial configuration has been completed.
    /// Used to show setup wizard on first run.
    /// </summary>
    public bool IsConfigured { get; set; } = false;
}