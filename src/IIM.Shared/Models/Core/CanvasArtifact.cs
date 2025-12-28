using System;
using System.Collections.Generic;
using IIM.Shared.Enums;
using IIM.Shared.Models;

namespace IIM.Shared.Models;

public enum ArtifactType
{
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
	EntityGroup

}

public enum FileClass
{
	All,
	Evidence,
	Intelligence,
	Reference,
	Reports,
	Derived
}

/// <summary>
/// Unified artifact model for Canvas display.
/// Can represent WorkspaceArtifacts or VirtualFiles.
/// </summary>
public sealed class CanvasArtifact
{
	public string Id { get; set; } = "";
	public ArtifactType Type { get; set; }
	public FileClass Classification { get; set; } = FileClass.All;

	public string Title { get; set; } = "";
	public string? Content { get; set; }
	public string? FileName { get; set; }
	public string? ContentType { get; set; }
	public long? SizeBytes { get; set; }
	public string? Summary { get; set; }

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

		_ => false
	};


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
