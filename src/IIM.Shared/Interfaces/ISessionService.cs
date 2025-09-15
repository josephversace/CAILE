using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Manages the lifecycle and operations of investigation sessions.
    /// </summary>
    public interface ISessionService
    {
        Task<InvestigationSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

        Task<IEnumerable<InvestigationSession>> GetSessionsByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

        Task<InvestigationSession> CreateSessionAsync(Guid workspaceId, string userId, string initialPrompt, CancellationToken cancellationToken = default);

        Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

        Task<Message> AddMessageAsync(Guid sessionId, Message message, CancellationToken cancellationToken = default);
    }
}
