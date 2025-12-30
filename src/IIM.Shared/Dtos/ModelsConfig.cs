// IIM.Shared/Models/ModelsConfig.cs
namespace IIM.Shared.Models
{
	public class ModelsConfig
	{
		public ProviderConfig Provider { get; set; } = new();
		public InfrastructureModelsConfig Infrastructure { get; set; } = new();
		public ActiveModelsConfig Active { get; set; } = new();
		public ToolModelsConfig Tools { get; set; } = new();
		public InferenceDefaults Defaults { get; set; } = new();
	}

	public class ProviderConfig
	{
		public string Type { get; set; } = "Ollama";
		public string Endpoint { get; set; } = "http://localhost:11434";
		public string? ApiKey { get; set; }
	}


	public class InfrastructureModelsConfig
	{
		public EmbeddingModelConfig Embedding { get; set; } = new();
		public LocalModelConfig? NER { get; set; }
		public LocalModelConfig? Audio { get; set; }
		public ModelConfig? Vision { get; set; }
		public LocalModelConfig? Intent { get; set; }  
	}

	public class ActiveModelsConfig
	{
		public ActiveModelConfig Primary { get; set; } = new();
		public ActiveModelConfig? Secondary { get; set; }
	}

	public class ToolModelsConfig
	{
		public ModelConfig? FunctionCalling { get; set; }
		public ModelConfig? Intent { get; set; }
	}

	public class ModelConfig
	{
		public string ModelId { get; set; } = "";
		public double? Temperature { get; set; }
		public int? MaxTokens { get; set; }
		public double? TopP { get; set; }
	}

	public class ActiveModelConfig : ModelConfig
	{
		public string? SystemPrompt { get; set; }
		public bool SupportsVision { get; set; } = false;
	}

	public class EmbeddingModelConfig
	{
		public string ModelId { get; set; } = "";
		public int Dimensions { get; set; } = 768;
		public int MaxInputTokens { get; set; } = 8192;
	}

	public class LocalModelConfig
	{
		public string Backend { get; set; } = "ONNX";
		public string LocalPath { get; set; } = "";
	}

	public class InferenceDefaults
	{
		public double Temperature { get; set; } = 0.7;
		public int MaxTokens { get; set; } = 2048;
		public double TopP { get; set; } = 0.9;
		public int TimeoutSeconds { get; set; } = 120;
	}
}