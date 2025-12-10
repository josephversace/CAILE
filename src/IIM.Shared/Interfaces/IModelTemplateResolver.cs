using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Dtos;

namespace IIM.Shared.Interfaces
{
	/// <summary>
	/// Resolves the active model template used by the orchestrator.
	/// A thin wrapper around IModelConfigurationTemplateService.
	/// </summary>
	public interface IModelTemplateResolver
	{
	

		/// <summary>
		/// Retrieves the ACTIVE model template:
		/// 1. DB-stored template (full JSON)
		/// 2. appsettings:ModelTemplates.ActiveTemplateId
		/// 3. "micro" fallback
		/// </summary>
		Task<ModelTemplateDto> GetActiveTemplateAsync(CancellationToken ct = default);
	}
}
