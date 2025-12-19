using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Events;

namespace IIM.Shared.Interfaces;

/// <summary>
/// Broadcasts ingestion events to connected clients.
/// Implemented in API layer with SignalR.
/// </summary>
public interface IIngestionNotifier
{
    Task NotifyFileIngestedAsync(FileIngestedEvent evt, CancellationToken ct = default);
}
