namespace IIM.Api.Configuration
{
    /// <summary>
    /// Configuration for Qdrant vector database connection
    /// </summary>
    public class QdrantConfiguration
    {
        public string BaseUrl { get; set; } = "http://localhost:6333";
        public string ApiKey { get; set; } = string.Empty;
        public string DefaultCollection { get; set; } = "documents";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
    }
}
