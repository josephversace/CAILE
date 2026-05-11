using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models.Configuration;

namespace IIM.Shared.Models
{
	public sealed class PromptEditorModel
	{
		public string Id { get; set; } = "";
		public string Content { get; set; } = "";
		public string Version { get; set; } = "1.0";
		public string? Notes { get; set; }

		public static PromptEditorModel From(PromptDefinition d) => new()
		{
			Id = d.Id,
			Content = d.Content,
			Version = d.Version,
			Notes = d.Notes
		};

		public PromptDefinition ToDefinition() => new()
		{
			Id = Id,
			Content = Content,
			Version = Version,
			Notes = Notes
		};
	}


}
