using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Ingestion.Models
{
	public sealed record ExtractedDocument(
		  string Text,
		  bool UsedFallback,
		  string Engine,
		  IDictionary<string, object?>? Metadata = null,
		  ExtractionArtifacts? Artifacts = null
	  );

	public sealed record ExtractionArtifacts(
	IReadOnlyList<TableArtifact>? Tables = null,
	IReadOnlyList<ImageArtifact>? Images = null,
	IReadOnlyList<TextChunkArtifact>? Chunks = null
);
	public sealed record TableArtifact(
		string? Caption,
		object Data    // intentionally opaque for now
	);

	public sealed record ImageArtifact(
		string Id,
		string? Caption,
		string? MimeType,
		string? StoragePath,
		IDictionary<string, object?>? Metadata = null
	);

	public sealed record TextChunkArtifact(
		string Text,
		int? Page,
		IDictionary<string, object?>? Metadata
	);

}
