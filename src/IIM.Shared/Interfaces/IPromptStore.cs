using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models.Configuration;

namespace IIM.Shared.Interfaces
{
	public interface IPromptStore
	{
		Task<IReadOnlyDictionary<string, PromptDefinition>> GetAllAsync(CancellationToken ct = default);
		Task<PromptDefinition?> GetAsync(string promptId, CancellationToken ct = default);
		Task SaveAsync(PromptDefinition prompt, CancellationToken ct = default);
		Task DeleteAsync(string promptId, CancellationToken ct = default);
		Task<bool> ExistsAsync(string promptId, CancellationToken ct = default);
		Task<IReadOnlyList<(string Id, DateTimeOffset UpdatedAt)>> ListAsync(CancellationToken ct = default);
	}


}
