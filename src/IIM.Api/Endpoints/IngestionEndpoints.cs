using IIM.Ingestion.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

public static class IngestionEndpoints
{
	public static void MapIngestionEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/ingestion");

		group.MapPost("/{evidenceId:guid}", async (
			Guid evidenceId,
			IIngestionPipeline pipeline) =>
		{
			var result = await pipeline.IngestAsync(evidenceId);
			return Results.Ok(result);
		});
	}
}
