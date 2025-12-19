namespace IIM.Application.Files;

/// <summary>
/// Result of registering an uploaded file.
/// </summary>
public sealed class RegisterUploadedFileResult
{
    public required Guid VirtualFileId { get; init; }
    public required string Blake3Hash { get; init; }
    public required bool Deduplicated { get; init; }
}
