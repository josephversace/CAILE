using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IIM.Shared.Dtos;


namespace IIM.Shared.Dtos;


	public interface IModelApiClient
	{
		// ---- Templates ----
		Task<List<ModelTemplateDto>> GetSystemTemplatesAsync();
		Task<ModelTemplateDto?> GetActiveTemplateAsync();
		Task<bool> ApplyTemplateAsync(ModelTemplateDto template);

		// ---- Foundry ----
		Task<List<FoundryModelDto>> GetFoundryAvailableAsync();
		Task<List<FoundryModelDto>> GetFoundryCachedAsync();
		Task<List<FoundryModelDto>> GetFoundryLoadedAsync();
		Task<List<FoundryModelDto>> GetFoundryAllAsync();
		Task<bool> LoadFoundryModelAsync(string modelId, string? ep = null, int? ttl = null);
		Task<bool> UnloadFoundryModelAsync(string modelId, bool force = false);

		// ---- Local Models ----
		Task<List<LocalModelInfoDto>> GetLocalModelsAsync(string slot);
		Task<LocalModelInfoDto?> UploadLocalModelAsync(string slot, string name, Stream zipStream, string contentType);
	}



public class ModelStatusDto
{
	public string ModelId { get; set; } = "";
	public string? FoundryModelId { get; set; }

	public bool IsLoaded { get; set; }
	public string Status { get; set; } = "unknown";

	public string Device { get; set; } = "CPU"; // CPU/GPU/NPU
	public long MemoryBytes { get; set; }

	public DateTimeOffset? LoadedAt { get; set; }

	// Optional metadata
	public Dictionary<string, object>? Metadata { get; set; }
}
