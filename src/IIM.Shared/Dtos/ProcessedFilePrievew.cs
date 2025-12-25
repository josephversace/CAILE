using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace IIM.Shared.Dtos
{
	public class ProcessedFilePreview
	{
		public string Summary { get; set; } = string.Empty;

		public Dictionary<string, string> Highlights { get; set; } = new();

		public string PreviewJson
		{
			get => JsonSerializer.Serialize(this);
			set
			{
				var parsed = JsonSerializer.Deserialize<ProcessedFilePreview>(value);
				if (parsed != null)
				{
					Summary = parsed.Summary;
					Highlights = parsed.Highlights;
				}
			}
		}
	}

}
