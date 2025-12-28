using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;


namespace IIM.Shared.Interfaces
{
	public interface IWorkspaceIntentEngine
	{
		/// <summary>
		/// Classify the intent of the user's query.
		/// </summary>
		Task<WorkspaceIntent> ClassifyAsync(
			IReadOnlyList<AGUIMessage> messages,
			IReadOnlyList<object> context,
			CancellationToken ct);
	}

}
