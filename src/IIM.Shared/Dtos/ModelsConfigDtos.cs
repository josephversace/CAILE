using System.Collections.Generic;

namespace IIM.Shared.Dtos
{
	/// <summary>
	/// Response for GET /api/models/config
	/// </summary>
	public class ModelsConfigResponse
	{
		public ProviderInfo Provider { get; set; } = new();
		public InfrastructureInfo Infrastructure { get; set; } = new();
		public ActiveModelsInfo Active { get; set; } = new();
		public ToolModelsInfo Tools { get; set; } = new();
		public InferenceDefaultsInfo Defaults { get; set; } = new();
	}

	public class ProviderInfo
	{
		public string Type { get; set; } = "";
		public string Endpoint { get; set; } = "";
		public bool IsConnected { get; set; }
	}

	public class InfrastructureInfo
	{
		public EmbeddingModelInfo Embedding { get; set; } = new();
		public LocalModelInfo? NER { get; set; }
		public LocalModelInfo? Audio { get; set; }
		public ModelInfo? Vision { get; set; }
	}

	public class ActiveModelsInfo
	{
		public ActiveModelInfo Primary { get; set; } = new();
		public ActiveModelInfo? Secondary { get; set; }
	}

	public class ToolModelsInfo
	{
		public ModelInfo? FunctionCalling { get; set; }
	}

	public class ModelInfo
	{
		public string ModelId { get; set; } = "";
		public double? Temperature { get; set; }
		public int? MaxTokens { get; set; }
		public double? TopP { get; set; }
		public bool IsLoaded { get; set; }
	}

	public class ActiveModelInfo : ModelInfo
	{
		public string? SystemPrompt { get; set; }
		public bool SupportsVision { get; set; }
	}

	public class EmbeddingModelInfo
	{
		public string ModelId { get; set; } = "";
		public int Dimensions { get; set; }
		public int MaxInputTokens { get; set; }
		public bool IsLoaded { get; set; }
	}

	public class LocalModelInfo
	{
		public string Backend { get; set; } = "";
		public string LocalPath { get; set; } = "";
		public bool IsAvailable { get; set; }
	}

	public class InferenceDefaultsInfo
	{
		public double Temperature { get; set; }
		public int MaxTokens { get; set; }
		public double TopP { get; set; }
		public int TimeoutSeconds { get; set; }
	}

	/// <summary>
	/// Request to update active models
	/// </summary>
	public class UpdateActiveModelsRequest
	{
		public UpdateActiveModelRequest? Primary { get; set; }
		public UpdateActiveModelRequest? Secondary { get; set; }
	}

	public class UpdateActiveModelRequest
	{
		public string ModelId { get; set; } = "";
		public double? Temperature { get; set; }
		public int? MaxTokens { get; set; }
		public double? TopP { get; set; }
		public string? SystemPrompt { get; set; }
	}

	/// <summary>
	/// Response from model test endpoint
	/// </summary>
	public class ModelTestResponse
	{
		public string Output { get; set; } = "";
		public int? TotalTokens { get; set; }
		public double? DurationMs { get; set; }
		public string? Error { get; set; }
	}

	/// <summary>
	/// Available models from the provider
	/// </summary>
	public class AvailableModelsResponse
	{
		public List<AvailableModelInfo> Models { get; set; } = new();
	}

	public class AvailableModelInfo
	{
		public string ModelId { get; set; } = "";
		public string? Family { get; set; }
		public long? SizeBytes { get; set; }
		public bool SupportsVision { get; set; }
		public bool SupportsTools { get; set; }
		public bool IsLoaded { get; set; }
	}
}