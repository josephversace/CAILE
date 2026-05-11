using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{
	// ===========================================================
	// ROOT
	// ===========================================================

	public sealed class ModelsConfig
	{
		/// <summary>
		/// Global provider defaults (used unless overridden per model)
		/// </summary>
		public ProviderConfig Provider { get; set; } = new();

		/// <summary>
		/// All system-owned models (embeddings, vision, tools, NER, etc.)
		/// </summary>
		public InfrastructureModelsConfig Infrastructure { get; set; } = new();

		/// <summary>
		/// Which models are preferred for chat / reasoning
		/// </summary>
		public ActiveModelsConfig Active { get; set; } = new();

		/// <summary>
		/// Default inference values (used when a model does not specify)
		/// </summary>
		public InferenceDefaults Defaults { get; set; } = new();
	}

	// ===========================================================
	// PROVIDERS
	// ===========================================================

	public sealed class ProviderConfig
	{
		/// <summary>
		/// Ollama | ONNX | vLLM | Foundry | OpenAI | AzureOpenAI
		/// </summary>
		public string Type { get; set; } = "Ollama";

		public string? Endpoint { get; set; } = "http://localhost:11434";
		public string? ApiKey { get; set; }
	}

	// ===========================================================
	// INFRASTRUCTURE MODELS (UNIFIED)
	// ===========================================================

	public sealed class InfrastructureModelsConfig
	{
		/// <summary>
		/// Keyed model registry (authoritative)
		/// Example keys: embedding.default, vision.default, tool.default
		/// </summary>
		public Dictionary<string, InfrastructureModelConfig> Models { get; set; }
			= new(StringComparer.OrdinalIgnoreCase);
	}

	public class ModelConfig
	{
		public ProviderConfig? ProviderOverride { get; set; }
		public string ModelId { get; set; } = "";
	
	}


	public sealed class InfrastructureModelConfig
	{
		/// <summary>
		/// Stable identifier (must match dictionary key)
		/// </summary>
		public string Key { get; set; } = "";

		/// <summary>
		/// Provider-specific model identifier
		/// </summary>
		public string ModelId { get; set; } = "";

		/// <summary>
		/// Optional provider override for this model
		/// </summary>
		public ProviderConfig? ProviderOverride { get; set; }

		/// <summary>
		/// Backend hint for local models (ONNX, CUDA, ROCm, CPU)
		/// </summary>
		public string? Backend { get; set; }

		/// <summary>
		/// Local filesystem path (ONNX / GGUF / Whisper)
		/// </summary>
		public string? LocalPath { get; set; }

		/// <summary>
		/// Embedding-specific metadata (optional)
		/// </summary>
		public int? Dimensions { get; set; }
		public int? MaxInputTokens { get; set; }

		/// <summary>
		/// Supported capabilities
		/// </summary>
		public List<ModelCapabilities> Capabilities { get; set; } = new();

		/// <summary>
		/// Optional inference overrides (falls back to Models.Defaults)
		/// </summary>
		public InferenceDefaults? Defaults { get; set; }
	}

	// ===========================================================
	// ACTIVE MODELS (ROLE SELECTION)
	// ===========================================================

	public sealed class ActiveModelsConfig
	{
		public ActiveModelConfig Primary { get; set; } = new();
		public ActiveModelConfig? Secondary { get; set; }
	}

	public sealed class ActiveModelConfig : ModelConfig
	{
		// Literal prompt override (highest priority)
		public string? ExplicitPrompt { get; set; }

		// Key into prompt store (DB override)
		public string? PromptOverrideKey { get; set; }

		// REQUIRED fallback key (defaults)
		public string DefaultPromptKey { get; set; } = "chat.default";

		public List<ModelCapabilities> Capabilities { get; set; } = new();

		public InferenceDefaults? Defaults { get; set; }
	}

	// ===========================================================
	// SHARED TYPES
	// ===========================================================

	public enum ModelCapabilities
	{
		None,
		Text,
		MultiModal,
		Vision,
		Audio,
		Tools,
		StructuredOutput,
		Embeddings,
		NER,
		Intent
	}

	public sealed class InferenceDefaults
	{
		public double Temperature { get; set; } = 0.7;
		public int MaxTokens { get; set; } = 2048;
		public double TopP { get; set; } = 0.9;
		public int TimeoutSeconds { get; set; } = 120;
	}
}
