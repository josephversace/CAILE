using System.Collections.Generic;
namespace IIM.Shared.Dtos
{

	public class ModelTemplatesResponse
	{
		public string ActiveTemplateId { get; set; } = "micro";
		public List<ModelTemplateDto> Templates { get; set; } = new();
	}

	public class ModelTemplateDto
	{
		public string Id { get; set; } = "";
		public string Name { get; set; } = "";
		public string? Description { get; set; }
		public MultiModelDto Models { get; set; } = new();
		public List<string> EnabledTools { get; set; } = new();

		public IEnumerable<ModelDefinitionDto> GetAllSlots()
		{
			if (Models.Chat is not null) yield return Models.Chat;
			if (Models.Reasoning is not null) yield return Models.Reasoning;
			if (Models.Coding is not null) yield return Models.Coding;
			if (Models.Embedding is not null) yield return Models.Embedding;
			if (Models.Vision is not null) yield return Models.Vision;
			if (Models.Multimodal is not null) yield return Models.Multimodal;
		}

	}


	public class MultiModelDto
	{
		public ModelDefinitionDto? Chat { get; set; }
		public ModelDefinitionDto? Reasoning { get; set; }
		public ModelDefinitionDto? Coding { get; set; }
		public EmbeddingModelDto? Embedding { get; set; }
		public ModelDefinitionDto? Vision { get; set; }
		public ModelDefinitionDto? Multimodal { get; set; }
	}

	public class ModelDefinitionDto
	{
		public string Id { get; set; } = "";
		public string DisplayName { get; set; } = "";
		public string? Description { get; set; }
		public string? FoundryModelId { get; set; }
		public string? LocalPath { get; set; }
		public double Temperature { get; set; } = 0.7;
		public int MaxTokens { get; set; } = 2048;
		public double TopP { get; set; } = 0.9;
		public string? SystemPrompt { get; set; }
		public string? CustomPromptFormat { get; set; }
	}

	public class EmbeddingModelDto : ModelDefinitionDto
	{
		public int Dimensions { get; set; } = 384;
		public string Pooling { get; set; } = "mean"; // "mean" | "cls"
		public bool Normalize { get; set; } = true;
	}

	public class ModelTestResponse
	{
		public string Output { get; set; } = "";
		public int? TotalTokens { get; set; }
		public double? DurationMs { get; set; }
		public string? Error { get; set; }
	}
}

