using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using IIM.Shared.Models.Core;

namespace IIM.Shared.Models
{
	public class StoredFile
	{
		// -----------------------------
		// Identity (hash = primary key)
		// -----------------------------
		[Key]
		[MaxLength(64)]
		[Required]
		public string Blake3Hash { get; set; } = string.Empty;

		public string? Md5Hash { get; set; }
		public string? Sha256Hash { get; set; }

		public long FileSize { get; set; }
		public string MimeType { get; set; } = string.Empty;

		// -----------------------------
		// Quarantine + Storage
		// -----------------------------
		public bool IsQuarantined { get; set; } = true;
		public string QuarantineReason { get; set; }
			= "Pending classification / quarantine by default";
		public DateTimeOffset? QuarantinedAt { get; set; }
			= DateTimeOffset.UtcNow;

		public string Bucket { get; set; } = "quarantine";
		public string StoragePath { get; set; } = string.Empty;

		// -----------------------------
		// Ingestion Metadata
		// -----------------------------
		public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;
		public string FirstSeenBy { get; set; } = string.Empty;
		public string OriginalFileName { get; set; } = string.Empty;
		public Guid? FirstWorkspaceId { get; set; }

		// -----------------------------
		// Perceptual hashing / similarity
		// -----------------------------
		public string? PerceptualHash { get; set; }
		public double? PerceptualQuality { get; set; }

		// -----------------------------
		// AI Ingested Metadata
		// -----------------------------
		public string? ContentSummary { get; set; }
		public string? DetectedEntitiesJson { get; set; }

		// -----------------------------
		// Classification Tags
		// -----------------------------
		public ICollection<ClassificationTag> ClassificationTags { get; set; }
			= new List<ClassificationTag>();

		// -----------------------------
		// Relations
		// -----------------------------
		public ICollection<VirtualFile> VirtualFiles { get; set; }
			= new List<VirtualFile>();

		// Derived outputs (OCR, thumbnails, text, etc.)
		public ICollection<ProcessedFile> ProcessedVersions { get; set; }
			= new List<ProcessedFile>();

		// -----------------------------
		// GraphRAG / Chunk Index Metadata
		// -----------------------------
		public bool IsIndexed { get; set; }
		public int ChunkCount { get; set; }
		public int EntityCount { get; set; }
		public DateTimeOffset? IndexedAt { get; set; }
		public string? GraphRagMetadataJson { get; set; }
	}
}
