using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

public interface IModelResolver
{
	Task<ActiveModelConfig> GetPrimaryModelAsync(CancellationToken ct = default);
	Task<ActiveModelConfig?> GetSecondaryModelAsync(CancellationToken ct = default);
	Task<EmbeddingModelConfig> GetEmbeddingModelAsync(CancellationToken ct = default);
	Task<ModelConfig?> GetFunctionCallingModelAsync(CancellationToken ct = default);
	Task<ModelConfig> GetIntentModelAsync(CancellationToken ct = default);  // ADD
	Task<ModelConfig?> GetVisionModelAsync(CancellationToken ct = default);
	Task<LocalModelConfig?> GetNerModelAsync(CancellationToken ct = default);
	Task<LocalModelConfig?> GetAudioModelAsync(CancellationToken ct = default);
	ProviderConfig GetProvider();
	InferenceDefaults GetDefaults();
}