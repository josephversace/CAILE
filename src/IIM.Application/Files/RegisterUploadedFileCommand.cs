using IIM.Shared.Mediator;

namespace IIM.Application.Files;

/// <summary>
/// Command to register an uploaded file for ingestion.
/// </summary>
public sealed class RegisterUploadedFileCommand : IRequest<RegisterUploadedFileResult>
{
    public required Guid WorkspaceId { get; init; }
    public required string FileName { get; init; }
    public required string MimeType { get; init; }
    public required long FileSize { get; init; }
    public required Stream InputStream { get; init; }
    
    /// <summary>
    /// If true, re-run ingestion even for deduplicated files.
    /// </summary>
    public bool Reprocess { get; init; }
}
