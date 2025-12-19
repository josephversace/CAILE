using IIM.Ingestion.Models;
using IIM.Ingestion.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;

public sealed class DoclingExtractionService : IDocumentExtractionService
{
	private readonly IDoclingService _docling;

	public DoclingExtractionService(IDoclingService docling)
	{
		_docling = docling;
	}

	public async Task<ExtractedDocument> ExtractAsync(
		byte[] bytes,
		string fileName,
		string mimeType,
		CancellationToken ct)
	{
		// Docling OWNS the stream lifecycle
		using var stream = new MemoryStream(bytes, writable: false);

		var result = await _docling.ParseAsync(stream, fileName, ct);

		return new ExtractedDocument(
		  Text: result.Markdown,
		  UsedFallback: true,
		  Engine: "docling",
		  Metadata: new Dictionary<string, object?>
		  {
			  ["pages"] = result.Pages?.Count,
			  ["title"] = result.Title,
			  ["mime"] = mimeType
		  },
		  Artifacts: ProjectArtifacts(result)
	  );
	}

	private static ExtractionArtifacts? ProjectArtifacts(DoclingDocument? doc)
	{
		if (doc == null)
			return null;

		return new ExtractionArtifacts(
			Tables: ProjectTables(doc),
			Images: ProjectImages(doc)
		);
	}

	private static IReadOnlyList<TableArtifact>? ProjectTables(DoclingDocument doc)
	{
		if (doc.Pages == null || doc.Pages.Count == 0)
			return null;

		var tables = new List<TableArtifact>();

		foreach (var page in doc.Pages)
		{
			foreach (var block in page.Blocks)
			{
				if (block.BlockType == "table")
				{
					tables.Add(new TableArtifact(
						Caption: block.SectionHeading,
						Data: block.Markdown
					));
				}
			}
		}

		return tables.Count > 0 ? tables : null;
	}


	private static IReadOnlyList<ImageArtifact>? ProjectImages(DoclingDocument doc)
	{
		if (doc.Images == null || doc.Images.Count == 0)
			return null;

		var images = new List<ImageArtifact>();

		foreach (var img in doc.Images)
		{
			images.Add(new ImageArtifact(
				Id: img.Id,
				Caption: img.Caption,
				MimeType: InferMimeFromPath(img.Path),
				StoragePath: img.Path, // already points to extracted image
				Metadata: new Dictionary<string, object?>
				{
					["page"] = img.PageNumber,
					["kind"] = img.Kind
				}
			));
		}

		return images.Count > 0 ? images : null;
	}

	private static string? InferMimeFromPath(string path)
	{
		var ext = Path.GetExtension(path)?.ToLowerInvariant();

		return ext switch
		{
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".webp" => "image/webp",
			".tiff" or ".tif" => "image/tiff",
			_ => null
		};
	}



}
