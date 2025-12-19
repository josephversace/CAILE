using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace IIM.Shared.Models;

/// <summary>
/// Base class for context chips attached to messages.
/// </summary>

[NotMapped]
public abstract class ContextChip
{
    public abstract string DisplayName { get; }
    public abstract ContextChipType ChipType { get; }
}



public enum ContextChipType
{
    File,
    Workspace
}

/// <summary>
/// Reference to a specific file by its content hash.
/// </summary>
[NotMapped]
public sealed class FileContextChip : ContextChip
{
    public required string FileName { get; init; }
    public required string Blake3Hash { get; init; }
    public string? MimeType { get; init; }
    public long? SizeBytes { get; init; }

    public override string DisplayName => FileName;
    public override ContextChipType ChipType => ContextChipType.File;
}

/// <summary>
/// Reference to all files in a workspace.
/// Resolved to hashes at query time.
/// </summary>
[NotMapped]
public sealed class WorkspaceContextChip : ContextChip
{
    public required Guid WorkspaceId { get; init; }
    public required string WorkspaceName { get; init; }

    public override string DisplayName => $"All: {WorkspaceName}";
    public override ContextChipType ChipType => ContextChipType.Workspace;
}
