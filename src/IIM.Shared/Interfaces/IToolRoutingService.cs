using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{

	public interface IToolRoutingService
	{
		Task<ToolDecision> DecideAsync(
			string userInput,
			CancellationToken ct = default);
	

	Task<ToolDecision> DecideAsync(
	string userInput,
	bool allowWebSearch,
	CancellationToken ct = default);

	}
}
