using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// A high-level service for managing investigation workflows, which include workspaces and their sessions.
    /// </summary>
    public interface IInvestigationService
    {
        // Workspace-related methods
        Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Workspace>> GetRecentWorkspacesAsync(int count, CancellationToken cancellationToken = default);
        Task<IEnumerable<InvestigationSession>> GetSessionsByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

        // Session-related methods
        Task<InvestigationSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<InvestigationSession> CreateSessionAsync(Guid workspaceId, string userId, string initialPrompt, CancellationToken cancellationToken = default);
        Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<Message> AddMessageAsync(Guid sessionId, Message message, CancellationToken cancellationToken = default);
    }
}
