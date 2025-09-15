using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace IIM.Application.Services
{
    /// <summary>
    /// Implements the high-level investigation workflows by orchestrating
    /// the IWorkspaceManager and ISessionService.
    /// </summary>
    public class InvestigationService : IInvestigationService
    {
        private readonly ILogger<InvestigationService> _logger;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly ISessionService _sessionService;

        public InvestigationService(
            ILogger<InvestigationService> logger,
            IWorkspaceManager workspaceManager,
            ISessionService sessionService)
        {
            _logger = logger;
            _workspaceManager = workspaceManager;
            _sessionService = sessionService;
        }

        // ===================================================================
        // Workspace Methods (Delegated to IWorkspaceManager)
        // ===================================================================

        public Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching workspace {WorkspaceId}", workspaceId);
            return _workspaceManager.GetWorkspaceAsync(workspaceId, cancellationToken);
        }

        public Task<IEnumerable<Workspace>> GetRecentWorkspacesAsync(int count, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching {Count} recent workspaces", count);
            return _workspaceManager.GetRecentWorkspacesAsync(count, cancellationToken);
        }

        public Task<IEnumerable<InvestigationSession>> GetSessionsByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching sessions for workspace {WorkspaceId}", workspaceId);
            // This is a session concern, so it delegates to the session service.
            return _sessionService.GetSessionsByWorkspaceAsync(workspaceId, cancellationToken);
        }

        // ===================================================================
        // Session Methods (Delegated to ISessionService)
        // ===================================================================

        public Task<InvestigationSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching session {SessionId}", sessionId);
            return _sessionService.GetSessionAsync(sessionId, cancellationToken);
        }

        public Task<InvestigationSession> CreateSessionAsync(Guid workspaceId, string userId, string initialPrompt, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new session for workspace {WorkspaceId} by user {UserId}", workspaceId, userId);
            return _sessionService.CreateSessionAsync(workspaceId, userId, initialPrompt, cancellationToken);
        }

        public Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting session {SessionId}", sessionId);
            return _sessionService.DeleteSessionAsync(sessionId, cancellationToken);
        }

        public Task<Message> AddMessageAsync(Guid sessionId, Message message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Adding message to session {SessionId}", sessionId);
            return _sessionService.AddMessageAsync(sessionId, message, cancellationToken);
        }
    }
}
