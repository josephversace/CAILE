using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{

	public class ToolCall
	{
		public string Name { get; set; } = "";
		public Dictionary<string, object?> Arguments { get; set; } = new();
	}
}
