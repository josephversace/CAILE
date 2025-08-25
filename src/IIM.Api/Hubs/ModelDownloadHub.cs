using Microsoft.AspNetCore.SignalR;

namespace IIM.Api.Hubs
{
    public class ModelDownloadHub : Hub
    {
        public string GetConnectionId() => Context.ConnectionId;
    }
}
