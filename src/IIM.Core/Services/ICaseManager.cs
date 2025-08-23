using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Core.Services;

/// <summary>
/// Interface for managing investigation cases
/// </summary>
public interface ICaseManager
{
    /// <summary>
    /// Creates a new case with the specified details
    /// </summary>
    Task<Case> CreateCaseAsync(string name, string description, CaseType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a case by its unique identifier
    /// </summary>
    Task<Case?> GetCaseAsync(string caseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all cases, optionally filtered by user
    /// </summary>
    Task<List<Case>> GetUserCasesAsync(string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a case with the provided update action
    /// </summary>
    Task<bool> UpdateCaseAsync(string caseId, Action<Case> updateAction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links an investigation session to a case
    /// </summary>
    Task<bool> LinkSessionToCaseAsync(string sessionId, string caseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links evidence to a case
    /// </summary>
    Task<bool> LinkEvidenceToCaseAsync(string evidenceId, string caseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent cases ordered by update date
    /// </summary>
    Task<List<Case>> GetRecentCasesAsync(int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a case (soft delete)
    /// </summary>
    Task<bool> DeleteCaseAsync(string caseId, CancellationToken cancellationToken = default);

    Task<List<TimelineEvent>> GetCaseTimelineAsync(
            string caseId,
            CancellationToken cancellationToken = default);
}
