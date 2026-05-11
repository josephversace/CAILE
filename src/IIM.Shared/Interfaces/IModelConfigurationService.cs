using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
	/// <summary>
	/// Provides access to the authoritative, materialized models configuration.
	/// After initial bootstrap, configuration is read from and written to the settings store.
	/// </summary>
	public interface IModelConfigurationService
	{
		/// <summary>
		/// Gets the current models configuration.
		/// The returned configuration is authoritative and fully materialized.
		/// </summary>
		Task<ModelsConfig> GetConfigurationAsync(
			CancellationToken ct = default);

		/// <summary>
		/// Saves the full models configuration.
		/// Callers must provide a complete, valid ModelsConfig.
		/// </summary>
		Task SaveConfigurationAsync(
			ModelsConfig config,
			CancellationToken ct = default);

		/// <summary>
		/// Resets the models configuration back to the installer/appsettings defaults.
		/// </summary>
		Task ResetToDefaultsAsync(
			CancellationToken ct = default);
	}
}
