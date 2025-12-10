using System;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;

public interface IEnrichmentQueue
{
	Task EnqueueAsync(Guid virtualFileId, CancellationToken ct);
	Task<EnrichmentTask?> DequeueAsync(CancellationToken ct);
	Task AckAsync(string messageId, CancellationToken ct);
	Task MoveToDeadLetterAsync(string messageId, EnrichmentTask task, string error, CancellationToken ct);
}
