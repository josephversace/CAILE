using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Provides access to model configuration.
    /// Reads from appsettings (via CaileConfig) with optional database overrides for Active models.
    /// </summary>
    public interface IModelConfigurationService
    {
        /// <summary>
        /// Gets the current models configuration.
        /// Infrastructure comes from appsettings (immutable).
        /// Active models may have database overrides.
        /// </summary>
        Task<ModelsConfig> GetConfigurationAsync(CancellationToken ct = default);

        /// <summary>
        /// Updates the active models configuration.
        /// Only Primary and Secondary can be changed at runtime.
        /// </summary>
        Task SaveActiveModelsAsync(ActiveModelsConfig active, CancellationToken ct = default);

        /// <summary>
        /// Resets active models to the defaults from appsettings.
        /// </summary>
        Task ResetActiveModelsAsync(CancellationToken ct = default);
    }
}