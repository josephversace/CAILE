using IIM.Shared.Dtos;
using IIM.Shared.Models;

namespace IIM.Infrastructure.Ollama
{
	public interface IModelService
	{
		string BaseUrl { get; }
		string InferenceEndpoint { get; }

	
		ValueTask DisposeAsync();
		Task EnsureInitializedAsync(CancellationToken ct = default);
		Task<IReadOnlyList<FoundryModelDto>> GetAllWithStatusDtoAsync(CancellationToken ct = default);
		Task<IReadOnlyList<FoundryModelDto>> GetAvailableModelsDtoAsync(CancellationToken ct = default);
		Task<IReadOnlyList<(string Alias, string ModelId)>> GetCachedModelsAsync();
		Task<IReadOnlyList<FoundryModelDto>> GetCachedModelsDtoAsync(CancellationToken ct = default);
		Task<string> GetLoadedModelForAliasAsync(string alias, CancellationToken ct = default);
		Task<IReadOnlyList<FoundryModelDto>> GetLoadedModelsDtoAsync(CancellationToken ct = default);
		Task<IReadOnlyList<FoundryModelDto>> GetPrimaryModelsAsync(CancellationToken ct = default);
		Task<IReadOnlyList<FoundryModelDto>> GetSecondaryModelsAsync(CancellationToken ct = default);
		Task LoadModelAsync(string modelId, CancellationToken ct = default);
		Task LoadModelForSlotAsync(string modelId, string slot, CancellationToken ct = default);
		Task UnloadModelAsync(string modelId, bool force = false, CancellationToken ct = default);
		Task UnloadSlotAsync(string slot, CancellationToken ct = default);
	}
}