using IIM.Api.Hubs;
using IIM.Shared.Events;
using IIM.Shared.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace IIM.Api.Services;

/// <summary>
/// SignalR implementation of ingestion notifications.
/// </summary>
public sealed class SignalRIngestionNotifier : IIngestionNotifier
{
    private readonly IHubContext<WorkspaceHub> _hubContext;

    public SignalRIngestionNotifier(IHubContext<WorkspaceHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyFileIngestedAsync(FileIngestedEvent evt, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group(WorkspaceHub.WorkspaceGroup(evt.WorkspaceId))
            .SendAsync("FileIngested", evt, ct);
    }
}
