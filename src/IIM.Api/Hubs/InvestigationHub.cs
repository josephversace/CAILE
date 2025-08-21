using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using IIM.Shared.Models;
using IIM.Core.Models;
using IIM.Shared.Enums;

namespace IIM.Api.Hubs
{
    /// <summary>
    /// SignalR hub for real-time investigation updates
    /// </summary>
    public class InvestigationHub : Hub
    {
        private readonly ILogger<InvestigationHub> _logger;
        
        public InvestigationHub(ILogger<InvestigationHub> logger)
        {
            _logger = logger;
        }
        
        // Session management
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
            _logger.LogInformation("User {ConnectionId} joined session {SessionId}", 
                Context.ConnectionId, sessionId);
        }
        
        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
            _logger.LogInformation("User {ConnectionId} left session {SessionId}", 
                Context.ConnectionId, sessionId);
        }
        
        // Broadcast to session participants
        public async Task SendMessageToSession(string sessionId, string message)
        {
            await Clients.Group($"session-{sessionId}").SendAsync("ReceiveMessage", message);
        }
        
        // Evidence notifications
        public async Task NotifyEvidenceAdded(string caseId, Evidence evidence)
        {
            await Clients.Group($"case-{caseId}").SendAsync("EvidenceAdded", evidence);
        }
        
        public async Task NotifyProcessingComplete(string evidenceId, ProcessingResult result)
        {
            await Clients.All.SendAsync("ProcessingComplete", evidenceId, result);
        }
        
        // Model status updates (for server mode)
        public async Task NotifyModelStatus(string modelId, ModelStatus status)
        {
            await Clients.All.SendAsync("ModelStatusChanged", modelId, status);
        }
        
        // Inference progress
        public async Task NotifyInferenceProgress(string requestId, int progress)
        {
            await Clients.Caller.SendAsync("InferenceProgress", requestId, progress);
        }
        
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
