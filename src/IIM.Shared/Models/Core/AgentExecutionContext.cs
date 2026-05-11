using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models.Core
{
	public sealed record AgentExecutionContext
	{
		public ModelOverrideContext? ModelOverrides { get; init; }
	}

}
