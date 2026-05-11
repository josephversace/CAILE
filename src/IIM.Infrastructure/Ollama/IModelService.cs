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
		Task<IReadOnlyList<ModelCatalogEntryDto>> GetAllWithStatusDtoAsync(CancellationToken ct = default);
		Task<IReadOnlyList<ModelCatalogEntryDto>> GetAvailableModelsDtoAsync(CancellationToken ct = default);
		Task<IReadOnlyList<(string Alias, string ModelId)>> GetCachedModelsAsync();
		Task<IReadOnlyList<ModelCatalogEntryDto>> GetCachedModelsDtoAsync(CancellationToken ct = default);
		Task<string> GetLoadedModelForAliasAsync(string alias, CancellationToken ct = default);
		Task<IReadOnlyList<ModelCatalogEntryDto>> GetLoadedModelsDtoAsync(CancellationToken ct = default);
		Task<IReadOnlyList<ModelCatalogEntryDto>> GetPrimaryModelsAsync(CancellationToken ct = default);
		Task<IReadOnlyList<ModelCatalogEntryDto>> GetSecondaryModelsAsync(CancellationToken ct = default);
		Task LoadModelAsync(string modelId, CancellationToken ct = default);
		Task LoadModelForSlotAsync(string modelId, string slot, CancellationToken ct = default);
		Task UnloadModelAsync(string modelId, bool force = false, CancellationToken ct = default);
		Task UnloadSlotAsync(string slot, CancellationToken ct = default);
		Task<(string? Primary, string? Secondary)> GetActiveSlotsAsync(CancellationToken ct);


	}
}