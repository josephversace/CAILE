using IIM.Ingestion.Models;
using IIM.Ingestion.Services;

public sealed class KreuzbergExtractionService : IDocumentExtractionService
{
	private readonly IKreuzbergClient _client;

	public KreuzbergExtractionService(IKreuzbergClient client)
	{
		_client = client;
	}

	public async Task<ExtractedDocument> ExtractAsync(
		byte[] content,
		string fileName,
		string mimeType,
		CancellationToken ct)
	{
		var result = await _client.ExtractAsync(content, fileName, mimeType, ct);

		return new ExtractedDocument(
			Text: result.Text,
			UsedFallback: false,
			Engine: "kreuzberg",
			Metadata: result.Metadata,
			Artifacts: new ExtractionArtifacts(
				Tables: result.Tables != null
					? new[] { new TableArtifact(null, result.Tables) }
					: null,

				Images: result.Images != null
					? new[] { new ImageArtifact(null, null, null, null) }
					: null
			)
		);

	}
}
