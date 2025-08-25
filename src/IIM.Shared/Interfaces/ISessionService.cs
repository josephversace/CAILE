using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
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
}
