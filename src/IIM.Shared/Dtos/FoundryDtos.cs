using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Dtos
{
	

	/// <summary>
	/// Canonical CAILE view of a Foundry Local model.
	/// This is what the Blazor admin UI should bind to.
	/// </summary>
	public class FoundryModelDto
	{
		/// <summary>
		/// Short, human-friendly identifier used in the UI (usually the alias).
		/// Example: "phi-4", "phi-3.5-mini", "mistral-7b".
		/// </summary>
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// Full display name with optional device / task decoration.
		/// Example: "Phi-4 (GPU)", "Mistral 7B Instruct (CPU)".
		/// </summary>
		public string DisplayName { get; set; } = string.Empty;

		public string RawName { get; set; } = string.Empty;

		/// <summary>
		/// The actual Foundry model name used in REST calls (/v1/chat, /openai/load).
		/// Example: "Phi-4-generic-gpu:1".
		/// </summary>
		public string FoundryModelId { get; set; } = string.Empty;

		/// <summary>
		/// Alias from Foundry catalog (if present).
		/// Example: "phi-4".
		/// </summary>
		public string? Alias { get; set; }

		/// <summary>
		/// Device type from Foundry runtime (CPU / GPU / NPU).
		/// </summary>
		public string Device { get; set; } = string.Empty;

		/// <summary>
		/// Primary task, e.g. "chat completion", "embedding".
		/// </summary>
		public string Task { get; set; } = string.Empty;

		/// <summary>
		/// File size in bytes, derived from fileSizeMb in Foundry response.
		/// </summary>


		/// <summary>
		/// Size in megabytes, convenient for display.
		/// </summary>
		public double FileSizeMb { get; set; }

		public double FileSizeGB => FileSizeMb / (1024.0 * 1024.0 );

		/// <summary>
		/// License string from Foundry (MIT, apache-2.0, etc.).
		/// </summary>
		public string? License { get; set; }

		/// <summary>
		/// Indicates if the model is currently loaded in memory (from /openai/loadedmodels).
		/// </summary>
		public bool IsLoaded { get; set; }

		// ---- Capability flags for UI filtering / badges ----

		public bool SupportsChat { get; set; }
		public bool SupportsCoding { get; set; }
		public bool SupportsEmbedding { get; set; }
		public bool SupportsVision { get; set; }
		public bool SupportsMultimodal { get; set; }

		/// <summary>
		/// True if Foundry says the model supports tool calling.
		/// </summary>
		public bool SupportsToolCalling { get; set; }

		// Optional extra metadata if you want to expose later
		public string? ProviderType { get; set; }
		public string? Version { get; set; }
	}

}
