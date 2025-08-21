namespace IIM.Api.Configuration
{
    /// <summary>
    /// Model template configuration for server mode
    /// </summary>
    public class ModelTemplateConfiguration
    {
        public string ActiveTemplateId { get; set; } = "default";
        public Dictionary<string, ModelTemplate> Templates { get; set; } = new();
    }
    
    public class ModelTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string LLMModel { get; set; }
        public string VisionModel { get; set; }
        public string OCRModel { get; set; }
        public string EmbeddingModel { get; set; }
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
