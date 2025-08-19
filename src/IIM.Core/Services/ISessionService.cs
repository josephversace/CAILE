using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Models;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services
{
    /// <summary>
    /// Service interface for managing investigation sessions
    /// This breaks the circular dependency by providing session management separately
    /// </summary>
    public interface ISessionService
    {
        /// <summary>
        /// Gets a session by ID
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The session if found</returns>
        Task<InvestigationSession> GetSessionAsync(string id, CancellationToken ct = default);

        /// <summary>
        /// Creates a new investigation session
        /// </summary>
        /// <param name="request">Session creation request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The created session</returns>
        Task<InvestigationSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing session
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="updateAction">Action to update the session</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The updated session</returns>
        Task<InvestigationSession> UpdateSessionAsync(string id, Action<InvestigationSession> updateAction, CancellationToken ct = default);

        /// <summary>
        /// Closes a session
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if closed successfully</returns>
        Task<bool> CloseSessionAsync(string id, CancellationToken ct = default);

        /// <summary>
        /// Gets all sessions
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of all sessions</returns>
        Task<List<InvestigationSession>> GetAllSessionsAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets sessions for a specific case
        /// </summary>
        /// <param name="caseId">Case ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of sessions for the case</returns>
        Task<List<InvestigationSession>> GetSessionsByCaseAsync(string caseId, CancellationToken ct = default);

        /// <summary>
        /// Deletes a session
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteSessionAsync(string id, CancellationToken ct = default);

        /// <summary>
        /// Adds a message to a session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="message">Message to add</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The updated session</returns>
        Task<InvestigationSession> AddMessageAsync(string sessionId, InvestigationMessage message, CancellationToken ct = default);
    }

    /// <summary>
    /// Implementation of session management service
    /// Handles all session-related operations without circular dependencies
    /// </summary>
    public class SessionService : ISessionService
    {
        private readonly ILogger<SessionService> _logger;
        private readonly Dictionary<string, InvestigationSession> _sessions = new();
        private readonly SemaphoreSlim _sessionLock = new(1, 1);

        /// <summary>
        /// Initializes the session service
        /// </summary>
        /// <param name="logger">Logger for diagnostics</param>
        public SessionService(ILogger<SessionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets a session by ID
        /// </summary>
        /// <param name="id">Session ID to retrieve</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The session if found</returns>
        /// <exception cref="KeyNotFoundException">If session not found</exception>
        public Task<InvestigationSession> GetSessionAsync(string id, CancellationToken ct = default)
        {
            if (_sessions.TryGetValue(id, out var session))
            {
                _logger.LogDebug("Retrieved session {SessionId}", id);
                return Task.FromResult(session);
            }

            _logger.LogWarning("Session {SessionId} not found", id);
            throw new KeyNotFoundException($"Session {id} not found");
        }

        /// <summary>
        /// Creates a new investigation session
        /// </summary>
        /// <param name="request">Session creation request with case info</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The newly created session</returns>
        public async Task<InvestigationSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct = default)
        {
            await _sessionLock.WaitAsync(ct);
            try
            {
                var session = new InvestigationSession
                {
                    Id = Guid.NewGuid().ToString(),
                    CaseId = request.CaseId,
                    Title = request.Title,
                    Type = Enum.TryParse<InvestigationType>(request.InvestigationType, out var type)
                        ? type
                        : InvestigationType.GeneralInquiry,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Status = InvestigationStatus.Active,
                    CreatedBy = Environment.UserName,
                    Messages = new List<InvestigationMessage>(),
                    EnabledTools = new List<string>(),
                    Models = new Dictionary<string, ModelConfiguration>(),
                    Findings = new List<Finding>()
                };

                _sessions[session.Id] = session;
                _logger.LogInformation("Created session {SessionId} for case {CaseId}",
                    session.Id, request.CaseId);

                return session;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        /// <summary>
        /// Updates an existing session using an update action
        /// </summary>
        /// <param name="id">Session ID to update</param>
        /// <param name="updateAction">Action that modifies the session</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The updated session</returns>
        /// <exception cref="KeyNotFoundException">If session not found</exception>
        public async Task<InvestigationSession> UpdateSessionAsync(
            string id,
            Action<InvestigationSession> updateAction,
            CancellationToken ct = default)
        {
            await _sessionLock.WaitAsync(ct);
            try
            {
                if (!_sessions.TryGetValue(id, out var session))
                {
                    throw new KeyNotFoundException($"Session {id} not found");
                }

                // Apply the update action
                updateAction(session);

                // Update timestamp
                session.UpdatedAt = DateTimeOffset.UtcNow;

                _logger.LogInformation("Updated session {SessionId}", id);
                return session;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        /// <summary>
        /// Closes an investigation session
        /// </summary>
        /// <param name="id">Session ID to close</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if closed successfully</returns>
        public async Task<bool> CloseSessionAsync(string id, CancellationToken ct = default)
        {
            await _sessionLock.WaitAsync(ct);
            try
            {
                if (!_sessions.TryGetValue(id, out var session))
                {
                    _logger.LogWarning("Cannot close session {SessionId} - not found", id);
                    return false;
                }

                session.Status = InvestigationStatus.Completed;
                session.UpdatedAt = DateTimeOffset.UtcNow;

                _logger.LogInformation("Closed session {SessionId}", id);
                return true;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        /// <summary>
        /// Gets all investigation sessions
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of all sessions</returns>
        public Task<List<InvestigationSession>> GetAllSessionsAsync(CancellationToken ct = default)
        {
            var sessions = _sessions.Values
                .OrderByDescending(s => s.UpdatedAt)
                .ToList();

            _logger.LogDebug("Retrieved {Count} sessions", sessions.Count);
            return Task.FromResult(sessions);
        }

        /// <summary>
        /// Gets all sessions for a specific case
        /// </summary>
        /// <param name="caseId">Case ID to filter by</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of sessions for the case</returns>
        public Task<List<InvestigationSession>> GetSessionsByCaseAsync(string caseId, CancellationToken ct = default)
        {
            var sessions = _sessions.Values
                .Where(s => s.CaseId == caseId)
                .OrderByDescending(s => s.UpdatedAt)
                .ToList();

            _logger.LogDebug("Retrieved {Count} sessions for case {CaseId}",
                sessions.Count, caseId);
            return Task.FromResult(sessions);
        }

        /// <summary>
        /// Deletes a session
        /// </summary>
        /// <param name="id">Session ID to delete</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if deleted successfully</returns>
        public async Task<bool> DeleteSessionAsync(string id, CancellationToken ct = default)
        {
            await _sessionLock.WaitAsync(ct);
            try
            {
                var removed = _sessions.Remove(id);
                if (removed)
                {
                    _logger.LogInformation("Deleted session {SessionId}", id);
                }
                else
                {
                    _logger.LogWarning("Cannot delete session {SessionId} - not found", id);
                }
                return removed;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        /// <summary>
        /// Adds a message to a session
        /// </summary>
        /// <param name="sessionId">Session ID to add message to</param>
        /// <param name="message">Message to add</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The updated session</returns>
        /// <exception cref="KeyNotFoundException">If session not found</exception>
        public async Task<InvestigationSession> AddMessageAsync(
            string sessionId,
            InvestigationMessage message,
            CancellationToken ct = default)
        {
            await _sessionLock.WaitAsync(ct);
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    throw new KeyNotFoundException($"Session {sessionId} not found");
                }

                session.Messages.Add(message);
                session.UpdatedAt = DateTimeOffset.UtcNow;

                _logger.LogInformation("Added message to session {SessionId}", sessionId);
                return session;
            }
            finally
            {
                _sessionLock.Release();
            }
        }
    }
}