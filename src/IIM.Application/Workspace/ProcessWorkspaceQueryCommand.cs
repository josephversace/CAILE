using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Investigation
{
    // Renamed from ProcessInvestigationCommand to better reflect its purpose
    public class ProcessWorkspaceQueryCommand : ICommand
    {
        public Guid WorkspaceId { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public Stream? AttachmentStream { get; set; }
        public string? AttachmentFileName { get; set; }
    }

    // Renamed from ProcessInvestigationCommandHandler
    public class ProcessWorkspaceQueryCommandHandler : IRequestHandler<ProcessWorkspaceQueryCommand, Unit>
    {
  
   
        private readonly IWorkspaceManager _workspaceManager;
        // The AI Orchestrator would be injected here for actual processing
        // private readonly IIM.Application.AI.SemanticKernel.SemanticKernelOrchestrator _orchestrator;

        public ProcessWorkspaceQueryCommandHandler(
         
 
            IWorkspaceManager workspaceManager)
        {
        
          
            _workspaceManager = workspaceManager;
        }

        public async Task<Unit> Handle(ProcessWorkspaceQueryCommand request, CancellationToken cancellationToken)
        {
        

            var workspace = await _workspaceManager.GetWorkspaceAsync(request.WorkspaceId, cancellationToken);
            if (workspace == null)
            {
                throw new InvalidOperationException("Workspace not found for the session.");
            }

       

            // Here, you would call the AI orchestrator to process the prompt and get a response
            // await _orchestrator.ProcessMessageAsync(session.Id, userMessage, cancellationToken);

            return Unit.Value;
        }
    }
}

