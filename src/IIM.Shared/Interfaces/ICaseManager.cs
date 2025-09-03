
using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IIM.Shared.Models;
using System.Threading;

namespace IIM.Shared.Models;

/// <summary>
/// Interface for managing workspaces
/// </summary>
public interface IWorkspaceManager
{
    /// <summary>
    /// Creates a new case with the specified details
    /// </summary>
    Task<Workspace> CreateWorkspceAsync(string name, string description, CaseType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a case by its unique identifier
    /// </summary>
    Task<Workspace?> GetWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all cases, optionally filtered by user
    /// </summary>
    Task<List<Workspace>> GetUserWorkspacesAsync(string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a case with the provided update action
    /// </summary>
    Task<bool> UpdateWorkspaceAsync(string caseId, Action<Workspace> updateAction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links an investigation session to a case
    /// </summary>
    Task<bool> LinkSessionToWorkspaceAsync(string sessionId, string caseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links evidence to a case
    /// </summary>
    Task<bool> LinkFileToWorkspaceAsync(string fileId, string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent cases ordered by update date
    /// </summary>
    Task<List<Workspace>> GetRecentWorkspacesAsync(int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a case (soft delete)
    /// </summary>
    Task<bool> DeleteWorkspaceAsync(string caseId, CancellationToken cancellationToken = default);

    Task<List<TimelineEvent>> GetCaseTimelineAsync(
            string caseId,
            CancellationToken cancellationToken = default);
}
