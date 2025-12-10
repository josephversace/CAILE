using IIM.Ingestion.Models;

namespace IIM.Ingestion.Interfaces
{
	public interface IIngestionPipeline
	{
		/// <summary>
		/// Run an ingestion job for the given evidence ID.
		/// </summary>
		Task<IngestionResult> IngestAsync(Guid evidenceId, CancellationToken ct = default);
	}
}
