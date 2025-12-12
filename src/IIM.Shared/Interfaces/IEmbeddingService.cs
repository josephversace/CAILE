using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IEmbeddingService
{
	bool IsReady { get; }
	int VectorSize { get; }

	Task InitializeAsync(CancellationToken ct = default);

	Task<IReadOnlyList<float[]>> EmbedAsync(
		IReadOnlyList<string> texts,
		CancellationToken ct = default);
}
