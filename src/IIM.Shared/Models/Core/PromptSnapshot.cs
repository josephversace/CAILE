using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models.Configuration;

namespace IIM.Shared.Models.Core
{
	public sealed class PromptSnapshot
	{
		public IReadOnlyDictionary<string, PromptDefinition> Prompts { get; }

		public PromptSnapshot(
			IReadOnlyDictionary<string, PromptDefinition> prompts)
		{
			Prompts = prompts;
		}
	}

}
