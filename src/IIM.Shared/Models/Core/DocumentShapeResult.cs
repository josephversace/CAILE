using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{

	[Flags]
	public enum DocumentShape
	{
		None = 0,
		Sectioned = 1 << 0,
		Versioned = 1 << 1,
		Chronological = 1 << 2,
		ListBased = 1 << 3,
		LogLike = 1 << 4,
		Tabular = 1 << 5,
		Narrative = 1 << 6
	}


	public sealed class DocumentShapeResult
	{
		public DocumentShape Shapes { get; init; }
		public float Confidence { get; init; }

		// Structural signals (persist these)
		public bool HasNumericHeaders { get; init; }
		public string? HeaderPattern { get; init; }
		public bool HasBulletLists { get; init; }
		public bool HasDates { get; init; }
		public bool HasTimestamps { get; init; }

		// Section boundaries (critical for citations)
		public IReadOnlyList<DocumentSection> Sections { get; init; } = Array.Empty<DocumentSection>();

		// Debug / audit
		public IReadOnlyDictionary<string, int> EvidenceCounts { get; init; }
			= new Dictionary<string, int>();
	}

	public sealed class DocumentSection
	{
		public string Id { get; init; } = string.Empty;   // e.g. "1.47"
		public string Header { get; init; } = string.Empty;
		public int StartOffset { get; init; }
		public int EndOffset { get; init; }
	}

}
