namespace IIM.Api.Configuration
{
    /// <summary>
    /// Configuration for WSL2 integration
    /// </summary>
    public class WslConfiguration
    {
        public string DefaultDistro { get; set; } = "IIM-Ubuntu";
        public bool AutoStart { get; set; } = true;
        public int ServiceCheckIntervalSeconds { get; set; } = 30;
        public int StartupTimeoutSeconds { get; set; } = 60;
        public List<string> AutoStartServices { get; set; } = new()
        {
            "docker",
            "qdrant",
            "embeddings"
        };
    }
}
