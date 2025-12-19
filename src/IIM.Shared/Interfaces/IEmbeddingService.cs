using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

public interface IEmbeddingService
{
	bool IsReady { get; }
	int VectorSize { get; }

	Task InitializeAsync(CancellationToken ct = default);

	Task<IReadOnlyList<float[]>> EmbedAsync(
		IReadOnlyList<EmbeddingWorkItem> texts,
		CancellationToken ct = default);
}
