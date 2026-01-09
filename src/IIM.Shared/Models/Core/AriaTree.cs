using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public sealed class AriaTree
	{
		public IReadOnlyList<AriaHeading> Headings { get; init; } = [];
	}

	public sealed class AriaHeading
	{
		public string Text { get; init; } = "";
		public int Level { get; init; }
		public int Order { get; init; } // document order
	}

}
