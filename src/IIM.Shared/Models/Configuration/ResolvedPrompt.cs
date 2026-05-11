using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models.Configuration
{
	public readonly struct ResolvedPrompt
	{
		public string Content { get; }
		public string Source { get; }
		public string? PromptId { get; }
		public string? Version { get; }

		private ResolvedPrompt(
			string content,
			string source,
			string? promptId,
			string? version)
		{
			Content = content;
			Source = source;
			PromptId = promptId;
			Version = version;
		}

		public static ResolvedPrompt FromLiteral(string content) =>
			new(content, "LiteralOverride", null, null);

		public static ResolvedPrompt FromDefinition(PromptDefinition def) =>
			new(def.Content, "PromptConfig", def.Id, def.Version);
	}

}
