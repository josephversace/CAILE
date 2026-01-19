using System;
using System.Collections.Generic;
using IIM.Shared.Enums;

namespace IIM.Shared.Models;

public enum ArtifactType
{
	// Existing types (preserve order for compatibility)
	Note,
	Code,
	Plan,
	Research,
	File,
	Entity,
	ExifData,
	TextData,
	RegexData,
	GraphData,
	IndicatorCollection,
	EntityGroup,
	ImageDescription,
	TextAnalysis,

	// New types for pipeline artifacts
	StructureData,      // doc.shape.detect output
	ChunkData,          // chunk.build output
	IndexData,          // embed.index.qdrant status

	// Web capture types
	Screenshot,         // web.capture.screenshot (download only)
	Thumbnail,          // web.capture.thumbnail (displayable)
	WebContent,         // web.extract.markdown
}

public enum FileStatus
{
	Pending,
	Processing,
	Ready,
	Failed,
	Quarantined
}

public enum FileClass
{
	All,
	Evidence,
	Intelligence,
	Reference,
	Reports,
	Derived,
	WebCapture
}

/// <summary>
/// Unified artifact model for Canvas display.
/// Can represent WorkspaceArtifacts, VirtualFiles, or IngestionStepState outputs.
/// </summary>
public sealed class CanvasArtifact
{
	public string Id { get; set; } = "";
	public ArtifactType Type { get; set; }
	public FileClass Classification { get; set; } = FileClass.All;
	public string Title { get; set; } = "";
	public string? Content { get; set; }
	public string? ContentHash { get; set; }
	public bool IsContentLazy { get; set; }

	public string? FileName { get; set; }
	public string? ContentType { get; set; }
	public long? SizeBytes { get; set; }
	public string? Summary { get; set; }

	public string? Bucket { get; set; }

	/// <summary>
	/// Content-addressable hash (BLAKE3). Populated after ingestion.
	/// </summary>
	public string? Blake3Hash { get; set; }

	/// <summary>
	/// MD5 hash for investigator reference/cross-system lookup.
	/// </summary>
	public string? Md5 { get; set; }

	/// <summary>
	/// SHA-256 hash for investigator reference/cross-system lookup.
	/// </summary>
	public string? Sha256 { get; set; }

	/// <summary>
	/// Upload/ingestion status for files.
	/// </summary>
	public FileUploadStatus Status { get; set; } = FileUploadStatus.Completed;

	public DateTime CreatedUtc { get; set; }

	public List<string>? Tags { get; set; }

	/// <summary>
	/// Selection state for multi-select mode.
	/// </summary>
	public bool IsSelected { get; set; }

	// ─────────────────────────────────────────────────────────────
	// New properties for pipeline/ingestion step artifacts
	// ─────────────────────────────────────────────────────────────

	/// <summary>
	/// Canonical step identifier (e.g. "doc.extract.text", "ioc.regex.extract").
	/// </summary>
	public string? StepId { get; set; }

	/// <summary>
	/// Human-readable display name for the step.
	/// </summary>
	public string? StepDisplayName { get; set; }

	/// <summary>
	/// Parsed metadata from MetadataJson (for display).
	/// </summary>
	public Dictionary<string, object>? Metadata { get; set; }

	/// <summary>
	/// Error message (truncated) for failed steps.
	/// </summary>
	public string? ErrorMessage { get; set; }

	/// <summary>
	/// When the step completed (for duration calculation).
	/// </summary>
	public DateTime? CompletedUtc { get; set; }

	/// <summary>
	/// Processing duration.
	/// </summary>
	public TimeSpan? Duration { get; set; }

	// ─────────────────────────────────────────────────────────────

	/// <summary>
	/// True if file is ready to be added to context.
	/// </summary>
	public bool CanAddToContext =>
		Type switch
		{
			ArtifactType.File =>
				Status == FileUploadStatus.Completed &&
				!string.IsNullOrEmpty(Blake3Hash),

			ArtifactType.Entity =>
				!string.IsNullOrWhiteSpace(Id),

			_ => !string.IsNullOrWhiteSpace(Content)
		};

	/// <summary>
	/// True if this artifact has loadable content via ContentHash.
	/// </summary>
	public bool HasContent => !string.IsNullOrEmpty(ContentHash);

	/// <summary>
	/// True if this is a download-only artifact (like large screenshots).
	/// </summary>
	public bool IsDownloadOnly => StepId == "web.capture.screenshot";

	/// <summary>
	/// Convert to context chip for chat.
	/// </summary>
	public FileContextChip ToContextChip() => new()
	{
		FileName = FileName ?? Title,
		Blake3Hash = Blake3Hash!,
		MimeType = ContentType,
		SizeBytes = SizeBytes
	};
}