namespace IIM.Api.Configuration
{
    /// <summary>
    /// Core deployment configuration that determines how the application runs
    /// </summary>
    public class DeploymentConfiguration
    {
        public DeploymentMode Mode { get; set; } = DeploymentMode.Standalone;
        public bool CanChangeMode { get; set; } = true;
        public bool RequireAuth { get; set; } = true;
        public bool IsDevelopment { get; set; } = false;
        public string ApiUrl { get; set; } = "http://localhost:5000";
        public string AdminEmail { get; set; } = "admin@iim.local";
        
        // Mode helpers
        public bool IsStandalone => Mode == DeploymentMode.Standalone;
        public bool IsServer => Mode == DeploymentMode.Server;
        public bool IsClient => Mode == DeploymentMode.Client;
        
        // Feature flags based on mode
        public bool EnableDynamicModels => IsStandalone;
        public bool EnableModelTemplates => IsServer;
        public bool EnableAdminInterface => IsServer;
        public bool EnableBackgroundServices => !IsClient;
    }
    
    public enum DeploymentMode
    {
        Standalone,  // Full stack, single user
        Client,      // UI only, connects to server
        Server       // API server for multiple clients
    }
}
