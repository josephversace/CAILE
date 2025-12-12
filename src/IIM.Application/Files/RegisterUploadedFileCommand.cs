using System;
using System.IO;
using IIM.Shared.Mediator;

namespace IIM.Application.Files;

/// <summary>
/// Registers an uploaded file, performs deduplication,
/// writes to storage if required, and enqueues ingestion.
/// </summary>
public sealed record RegisterUploadedFileCommand : IRequest<Guid>
{
	public Guid WorkspaceId { get; init; }
	public string FileName { get; init; } = string.Empty;
	public string MimeType { get; init; } = "application/octet-stream";
	public long FileSize { get; init; }

	/// <summary>
	/// Stream containing the uploaded file.
	/// Must be readable and positioned at 0.
	/// </summary>
	public Stream InputStream { get; init; } = Stream.Null;

	/// <summary>
	/// Forces reprocessing even if content already exists.
	/// </summary>
	public bool Reprocess { get; init; }
}
