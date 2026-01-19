using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IIM.Shared.Models
{
	public class ProcessedFile
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		// INPUT (content-addressed)
		public string StoredFileHash { get; set; } = string.Empty;

		[JsonIgnore]
		public StoredFile StoredFile { get; set; } = null!;

		// OUTPUT (content-addressed)
		public string? DerivedHash { get; set; }

		// PROCESSOR IDENTITY
		public string ProcessorName { get; set; } = string.Empty;
		public string? ProcessorVersion { get; set; }

		// PROCESSOR CLASSIFICATION
		public string ProcessorKind { get; set; } = string.Empty;
		// examples: "extraction", "vision", "graph", "classification", "embedding"

		// PARAMETERS & REPRODUCIBILITY
		public string? ParametersHash { get; set; }

		// TIMING
		public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;

		// LIGHTWEIGHT METADATA (NOT the output)
		public string MetadataJson { get; set; } = "{}";
	}

}
