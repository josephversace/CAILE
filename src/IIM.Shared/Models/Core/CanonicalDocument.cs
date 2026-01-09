using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public sealed class CanonicalDocument
	{
		public string Title { get; init; } = "";
		public string SourceUrl { get; init; } = "";
		public string Markdown { get; init; } = "";

		public IReadOnlyList<CanonicalSection> Sections { get; init; } = [];
	}

	public sealed record CanonicalSection
	{
		public string Heading { get; init; } = "";
		public int Level { get; init; }
		public string Content { get; init; } = "";

		// Provenance
		public string Source { get; init; } = ""; // aria | docling | smartreader
	}


}
