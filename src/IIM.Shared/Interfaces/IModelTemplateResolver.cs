using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
	/// <summary>
	/// Resolves models for runtime use based on the materialized ModelsConfig.
	/// Resolution is performed by role (Active) or capability (Infrastructure).
	/// </summary>
	public interface IModelResolver
	{
		// ===========================================================
		// ACTIVE (CHAT / REASONING)
		// ===========================================================

		/// <summary>
		/// Gets the primary chat model.
		/// </summary>
		Task<ActiveModelConfig> GetPrimaryModelAsync(
			CancellationToken ct = default);

		/// <summary>
		/// Gets the secondary (reasoning) model, if configured.
		/// </summary>
		Task<ActiveModelConfig?> GetSecondaryModelAsync(
			CancellationToken ct = default);

		// ===========================================================
		// CAPABILITY-BASED RESOLUTION
		// ===========================================================

		Task<InfrastructureModelConfig> GetEmbeddingModelAsync(
			CancellationToken ct = default);

		Task<InfrastructureModelConfig> GetVisionModelAsync(
			CancellationToken ct = default);

		Task<InfrastructureModelConfig> GetFunctionCallingModelAsync(
			CancellationToken ct = default);

		Task<InfrastructureModelConfig> GetIntentModelAsync(
			CancellationToken ct = default);

		Task<InfrastructureModelConfig> GetNerModelAsync(
			CancellationToken ct = default);

		Task<InfrastructureModelConfig> GetAudioModelAsync(
			CancellationToken ct = default);

		Task<ActiveModelConfig> ResolveActiveByModelIdAsync(string modelId, CancellationToken ct = default);


		// ===========================================================
		// PROVIDER / INFERENCE RESOLUTION
		// ===========================================================

		/// <summary>
		/// Resolves the provider configuration for a specific active model,
		/// falling back to global defaults if no override is present.
		/// </summary>
		Task<ProviderConfig> GetProviderAsync(
			ActiveModelConfig model,
			CancellationToken ct = default);

		/// <summary>
		/// Resolves the provider configuration for a specific model,
		/// falling back to global defaults if no override is present.
		/// </summary>
		Task<ProviderConfig> GetProviderAsync(
			InfrastructureModelConfig model,
			CancellationToken ct = default);

		/// <summary>
		/// Resolves inference defaults for a specific model,
		/// falling back to global defaults if no override is present.
		/// </summary>
		Task<InferenceDefaults> GetInferenceDefaultsAsync(
			ActiveModelConfig model,
			CancellationToken ct = default);

		/// <summary>
		/// Resolves inference defaults for a specific model,
		/// falling back to global defaults if no override is present.
		/// </summary>
		Task<InferenceDefaults> GetInferenceDefaultsAsync(
			InfrastructureModelConfig model,
			CancellationToken ct = default);
	}
}
