using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;

namespace IIM.Shared.Interfaces
{
	public interface IToolRegistry
	{
		void Register(string name, Func<IDictionary<string, object?>, Task<string>> handler);
		Task<string> InvokeAsync(string name, IDictionary<string, object?>? args);
		IList<AITool> GetAIFunctions();

	}

}
