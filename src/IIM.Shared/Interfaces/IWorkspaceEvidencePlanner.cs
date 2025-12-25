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
		Task<WorkspaceEvidencePlan> BuildPlan(WorkspaceIntent intent, IReadOnlyList<object> context, Guid? workspaceid, List<string?> filehashes);
	}

}
