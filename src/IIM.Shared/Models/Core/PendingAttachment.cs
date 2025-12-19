using System;
using IIM.Shared.Enums;
using IIM.Shared.Models;

namespace IIM.Shared.Models;

/// <summary>
/// Tracks an attachment uploaded via chat through upload → ingestion → ready states.
/// </summary>
public sealed class PendingAttachment
{
	public string TempId { get; init; } = Guid.NewGuid().ToString();
	public string FileName { get; init; } = "";
	public string? ContentType { get; init; }
	public long Size { get; init; }

	/// <summary>
	/// Populated after upload completes.
	/// </summary>
	public Guid? VirtualFileId { get; set; }

	/// <summary>
	/// Populated after ingestion callback (or immediately if deduplicated).
	/// </summary>
	public string? Blake3Hash { get; set; }

	public FileUploadStatus Status { get; set; } = FileUploadStatus.Uploading;
	public string? Error { get; set; }

	public bool IsReady => Status == FileUploadStatus.Completed && Blake3Hash != null;
	public bool IsPending => Status is FileUploadStatus.Uploading or FileUploadStatus.Pending;
	public bool IsFailed => Status == FileUploadStatus.Failed;

	public FileContextChip ToContextChip() => new()
	{
		FileName = FileName,
		Blake3Hash = Blake3Hash!,
		MimeType = ContentType,
		SizeBytes = Size
	};
}
