using IIM.Shared.Enums;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

/// <summary>
/// Defines the contract for managing the lifecycle and high-level operations of workspaces.
/// </summary>
public interface IWorkspaceManager
{
    /// <summary>
    /// Creates a new workspace with the specified details.
    /// </summary>
    Task<Workspace> CreateWorkspaceAsync(string name, string description, WorkspaceType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a workspace by its unique identifier.
    /// </summary>
    Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all workspaces, optionally filtered by user.
    /// </summary>
    Task<IEnumerable<Workspace>> GetUserWorkspacesAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a workspace with the provided update action.
    /// </summary>
    Task<bool> UpdateWorkspaceAsync(Guid workspaceId, Action<Workspace> updateAction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links an investigation session to a workspace.
    /// </summary>
    Task<bool> LinkSessionToWorkspaceAsync(Guid sessionId, Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a virtual file to a workspace.
    /// </summary>
    Task<bool> LinkFileToWorkspaceAsync(Guid virtualFileId, Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent workspaces ordered by update date.
    /// </summary>
    Task<IEnumerable<Workspace>> GetRecentWorkspacesAsync(int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workspace (soft delete).
    /// </summary>
    Task<bool> DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the timeline of events for a specific workspace.
    /// </summary>
    Task<IEnumerable<TimelineEvent>> GetWorkspaceTimelineAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
