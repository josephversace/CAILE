using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IIM.Shared.Dtos;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
	public interface IWorkspaceEvidencePlanner
	{
		/// <summary>
		/// Build a retrieval plan based on intent and context.
		/// </summary>
		Task<WorkspaceEvidencePlan> BuildPlan(
			WorkspaceIntent intent,
			IReadOnlyList<object> context,
			Guid? workspaceId,
			List<string?> fileHashes,
			string? modelId = null);
	}

}
