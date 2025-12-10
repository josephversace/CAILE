using System;
using System.Threading;
using System.Threading.Tasks;
using IIM.Ingestion.Interfaces;
using IIM.Shared.Mediator;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Files;

public class IngestFileHandler : IRequestHandler<IngestFileCommand, Unit>
{
	private readonly IIngestionPipeline _ingestion;
	private readonly ILogger<IngestFileHandler> _logger;

	public IngestFileHandler(
		IIngestionPipeline ingestion,
		ILogger<IngestFileHandler> logger)
	{
		_ingestion = ingestion;
		_logger = logger;
	}

	public async Task<Unit> Handle(IngestFileCommand cmd, CancellationToken ct)
	{
		_logger.LogInformation("Starting ingestion for file {FileId}.", cmd.FileId);

		try
		{
			var result = await _ingestion.IngestAsync(cmd.FileId, ct);

			_logger.LogInformation(
				"Ingestion complete for {FileId}: {Chunks} chunks, {Entities} entities, {Vectors} vectors.",
				cmd.FileId,
				result.ChunkCount,
				result.EntityCount,
				result.VectorCount);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ingestion failed for file {FileId}.", cmd.FileId);
			throw;
		}

		return Unit.Value;
	}
}