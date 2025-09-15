using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Mediator;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;

namespace IIM.Application.Case
{
    // =================================================================================================
    // Commands
    // =================================================================================================

    public record CreateWorkspaceCommand : IRequest<Workspace>
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public WorkspaceType Type { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
    }

    public record DeleteWorkspaceCommand(Guid WorkspaceId, string? Reason, bool ArchiveOnly) : ICommand;

    // =================================================================================================
    // Queries
    // =================================================================================================

    public record GetWorkspaceQuery(Guid WorkspaceId) : IQuery<Workspace?>;

    public record GetRecentWorkspacesQuery(int Count) : IQuery<IEnumerable<Workspace>>;

    // =================================================================================================
    // Command Handlers
    // =================================================================================================

    public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Workspace>
    {
        private readonly IWorkspaceManager _workspaceManager;

        public CreateWorkspaceCommandHandler(IWorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager;
        }

        public async Task<Workspace> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
        {
            // Delegate the creation to the manager, which handles the database interaction.
            var newWorkspace = await _workspaceManager.CreateWorkspaceAsync(
                request.Name,
                request.Description,
                request.Type,
                cancellationToken
            );

            // The WorkspaceManager is responsible for setting initial state.
            // If we need to add the creator, we would call an update.
            // For now, this fulfills the creation contract.

            return newWorkspace;
        }
    }

    public class DeleteWorkspaceCommandHandler : IRequestHandler<DeleteWorkspaceCommand, Unit>
    {
        private readonly IWorkspaceManager _workspaceManager;

        public DeleteWorkspaceCommandHandler(IWorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager;
        }

        public async Task<Unit> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
        {
            var success = await _workspaceManager.DeleteWorkspaceAsync(request.WorkspaceId, cancellationToken);
            // We can add error handling here if 'success' is false.
            return Unit.Value;
        }
    }

    // =================================================================================================
    // Query Handlers
    // =================================================================================================

    public class GetWorkspaceQueryHandler : IRequestHandler<GetWorkspaceQuery, Workspace?>
    {
        private readonly IWorkspaceManager _workspaceManager;

        public GetWorkspaceQueryHandler(IWorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager;
        }

        public async Task<Workspace?> Handle(GetWorkspaceQuery request, CancellationToken cancellationToken)
        {
            return await _workspaceManager.GetWorkspaceAsync(request.WorkspaceId, cancellationToken);
        }
    }

    public class GetRecentWorkspacesQueryHandler : IRequestHandler<GetRecentWorkspacesQuery, IEnumerable<Workspace>>
    {
        private readonly IWorkspaceManager _workspaceManager;

        public GetRecentWorkspacesQueryHandler(IWorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager;
        }

        public async Task<IEnumerable<Workspace>> Handle(GetRecentWorkspacesQuery request, CancellationToken cancellationToken)
        {
            return await _workspaceManager.GetRecentWorkspacesAsync(request.Count, cancellationToken);
        }
    }
}

