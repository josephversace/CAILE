using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Dtos
{
	public sealed class DerivedArtifactResponse
	{
		public bool Success { get; init; }
		public string? Content { get; init; }        // markdown / json / text
		public string ContentType { get; init; } = ""; // text/markdown, application/json
		public bool IsPreview { get; init; }
		public int? TotalLength { get; init; }        // full size if truncated
		public string? SourceHash { get; init; }      // lineage
	}

}
