using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public sealed class ProcessedFileWriteRequest
	{
		// REQUIRED — input identity
		public required string StoredFileHash { get; init; }

		// REQUIRED — processor identity
		public required string ProcessorName { get; init; }
		public string? ProcessorVersion { get; init; }
		public required string ProcessorKind { get; init; }

		// REQUIRED — output identity (if output exists)
		public byte[]? DerivedContent { get; init; }
		public string? DerivedContentType { get; init; } // "application/json", "text/plain", etc.

		// OPTIONAL — reproducibility
		public string? ParametersHash { get; init; }

		// REQUIRED — lightweight metadata (preview only)
		public Dictionary<string, object> Metadata { get; init; } = new();

		// OPTIONAL — override timestamp (rare)
		public DateTimeOffset? ProcessedAt { get; init; }
	}

}
