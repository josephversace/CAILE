using System;
using System.Collections.Generic;
using IIM.Shared.Models;

namespace IIM.Shared.Dtos;

/// <summary>
/// Canonical UI-facing representation of a model available to CAILE.
/// This is the ONLY model shape the Blazor admin UI should bind to.
/// </summary>
public sealed class ModelCatalogEntryDto
{
	// ===========================================================
	// IDENTITY
	// ===========================================================

	/// <summary>
	/// Canonical CAILE model key.
	/// Example: "phi-4", "mistral-7b", "text-embedding-large".
	/// Used for selection & persistence.
	/// </summary>
	public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Provider-specific model identifier used at runtime.
	/// Example: "Phi-4-generic-gpu:1", "mistral:7b-instruct".
	/// </summary>
	public string ModelId { get; set; } = string.Empty;

	/// <summary>
	/// Optional alias provided by the backend catalog.
	/// </summary>
	public string? Alias { get; set; }

	// ===========================================================
	// DISPLAY (UI ONLY)
	// ===========================================================

	/// <summary>
	/// Human-friendly display name.
	/// Example: "Phi-4 (GPU)", "Mistral 7B Instruct (CPU)".
	/// </summary>
	public string DisplayName { get; set; } = string.Empty;

	/// <summary>
	/// Raw backend name (useful for diagnostics).
	/// </summary>
	public string? RawName { get; set; }

	// ===========================================================
	// PROVIDER / RUNTIME
	// ===========================================================

	/// <summary>
	/// Provider type: Foundry, Ollama, OpenAI, vLLM, etc.
	/// </summary>
	public string ProviderType { get; set; } = string.Empty;

	/// <summary>
	/// Execution backend hint: CPU, GPU, NPU, CUDA, ROCm, ONNX.
	/// </summary>
	public string? Backend { get; set; }

	/// <summary>
	/// Device class if exposed by provider (CPU / GPU / NPU).
	/// </summary>
	public string? Device { get; set; }

	/// <summary>
	/// True if the model is currently loaded in memory.
	/// </summary>
	public bool IsLoaded { get; set; }

	// ===========================================================
	// CAPABILITIES
	// ===========================================================

	/// <summary>
	/// Declared capabilities of the model.
	/// Drives UI filtering and validation.
	/// </summary>
	public IReadOnlyList<ModelCapabilities> Capabilities { get; set; }
		= Array.Empty<ModelCapabilities>();

	// ===========================================================
	// SIZE / METADATA
	// ===========================================================

	/// <summary>
	/// Model size in megabytes.
	/// </summary>
	public double FileSizeMb { get; set; }

	/// <summary>
	/// Model size in gigabytes (derived).
	/// </summary>
	public double FileSizeGb => FileSizeMb / 1024.0;

	/// <summary>
	/// Optional license identifier (MIT, Apache-2.0, etc.).
	/// </summary>
	public string? License { get; set; }

	/// <summary>
	/// Optional model version.
	/// </summary>
	public string? Version { get; set; }
}
