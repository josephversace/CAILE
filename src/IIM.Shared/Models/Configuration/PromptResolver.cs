using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models.Core;

namespace IIM.Shared.Models.Configuration
{
	public sealed class PromptResolver
	{
		public ResolvedPrompt Resolve(
			PromptSnapshot snapshot,
			string? explicitPrompt,
			string? overrideKey,
			string defaultKey)
		{
			if (!string.IsNullOrWhiteSpace(explicitPrompt))
				return ResolvedPrompt.FromLiteral(explicitPrompt);

			if (Try(snapshot, overrideKey, out var p))
				return p;

			if (Try(snapshot, defaultKey, out p))
				return p;

			throw new InvalidOperationException(
				$"Prompt '{defaultKey}' not found.");
		}

		private static bool Try(
			PromptSnapshot snapshot,
			string? key,
			out ResolvedPrompt resolved)
		{
			resolved = default;
			if (string.IsNullOrWhiteSpace(key))
				return false;

			if (!snapshot.Prompts.TryGetValue(key, out var def))
				return false;

			resolved = ResolvedPrompt.FromDefinition(def);
			return true;
		}
	}


}
