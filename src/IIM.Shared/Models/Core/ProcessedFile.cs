using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
	public class ProcessedFile
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid VirtualFileId { get; set; }
		public VirtualFile VirtualFile { get; set; }

		// Points to StoredFile.Blake3Hash
		public string StoredFileHash { get; set; } = string.Empty;
		public StoredFile StoredFile { get; set; }

		public string ProcessorName { get; set; } = string.Empty; // e.g. "docling", "clip", "vision-transformer"
		public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;

		// Flexible, JSONB metadata
		public Dictionary<string, string> Metadata { get; set; } = new();
		public string MetadataJson
		{
			get => System.Text.Json.JsonSerializer.Serialize(Metadata);
			set => Metadata =
				string.IsNullOrWhiteSpace(value)
					? new()
					: System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(value)!;
		}
	}
}
