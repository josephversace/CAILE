using Microsoft.AspNetCore.SignalR;

namespace IIM.Api.Hubs;

/// <summary>
/// SignalR hub for workspace-scoped real-time events.
/// </summary>
public sealed class WorkspaceHub : Hub
{
    /// <summary>
    /// Join a workspace group to receive events for that workspace.
    /// </summary>
    public async Task JoinWorkspace(Guid workspaceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, WorkspaceGroup(workspaceId));
    }
    
    /// <summary>
    /// Leave a workspace group.
    /// </summary>
    public async Task LeaveWorkspace(Guid workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, WorkspaceGroup(workspaceId));
    }
    
    public static string WorkspaceGroup(Guid workspaceId) => $"workspace-{workspaceId}";
}
