using IIM.Core.Models;

namespace IIM.Core.Services;

public interface ISessionProvider
{
    Task<InvestigationSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}